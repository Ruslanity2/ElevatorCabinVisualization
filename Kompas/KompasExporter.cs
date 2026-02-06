using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Kompas6API5;
using Kompas6Constants;
using Kompas6Constants3D;
using KompasAPI7;

namespace ElevatorCabinVisualization
{
    /// <summary>
    /// Класс для экспорта кабины лифта в Kompas 3D
    /// </summary>
    public class KompasExporter
    {
        IApplication application;
        IKompasDocument3D document3D;
        public KompasObject kompas;
        public ksDocument3D ksDocument3D;
        ObjectAssemblyKompas root;
        IDocuments documents;
        private readonly KompasRestartService kompasRestartService;
        Dictionary<string, string> dictionaryofreplacements;
        Dictionary<string, double> dictionaryvariables;
        private string reportFilePath;

        /// <summary>
        /// Объект отчета, загруженный из XML
        /// </summary>
        public Report Report { get; private set; }

        /// <summary>
        /// Путь для выгрузки файлов
        /// </summary>
        public string ExportPath { get; set; }

        /// <summary>
        /// Список деталей, требующих доработки (свойство NeedsRework = true)
        /// </summary>
        public List<ObjectAssemblyKompas> NeedsReworkParts { get; private set; } = new List<ObjectAssemblyKompas>();

        /// <summary>
        /// Список деталей-крепежа (свойство ModifierFastener = true)
        /// </summary>
        public List<ObjectAssemblyKompas> ModifierFastenerParts { get; private set; } = new List<ObjectAssemblyKompas>();

        /// <summary>
        /// Конструктор класса KompasExporter
        /// Загружает самый последний Report.xml из папки Reports
        /// </summary>
        public KompasExporter()
        {
            kompasRestartService = new KompasRestartService();
            // Получаем путь к директории приложения
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string reportsDirectory = Path.Combine(appDirectory, "Reports");

            // Проверяем существование папки Reports
            if (!Directory.Exists(reportsDirectory))
            {
                throw new DirectoryNotFoundException($"Папка Reports не найдена по пути: {reportsDirectory}");
            }

            // Получаем все XML-файлы из папки Reports
            var xmlFiles = Directory.GetFiles(reportsDirectory, "*.xml", SearchOption.AllDirectories);

            if (xmlFiles.Length == 0)
            {
                throw new FileNotFoundException("Не найдено ни одного XML-файла в папке Reports");
            }

            // Находим самый последний файл по дате создания
            reportFilePath = xmlFiles
                .OrderByDescending(f => File.GetCreationTime(f))
                .First();

            // Десериализуем XML-файл в объект Report
            Report = Report.Load(reportFilePath);
        }

        /// <summary>
        /// Ожидает завершения загрузки и перестроения документа
        /// </summary>
        /// <param name="timeoutMs">Максимальное время ожидания в миллисекундах</param>
        /// <returns>true если документ готов, false если превышен таймаут</returns>
        private bool WaitForDocumentReady(int timeoutMs = 60000)
        {
            int elapsed = 0;
            int sleepInterval = 200; // Проверяем каждые 200 мс

            while (elapsed < timeoutMs)
            {
                try
                {
                    // Пытаемся получить TopPart документа - если получилось, документ загружен
                    if (document3D != null)
                    {
                        IPart7 testPart = document3D.TopPart;
                        if (testPart != null)
                        {
                            // Документ успешно загружен и готов к работе
                            return true;
                        }
                    }
                }
                catch
                {
                    // Документ еще не готов, продолжаем ждать
                }

                System.Threading.Thread.Sleep(sleepInterval);
                elapsed += sleepInterval;
            }

            return false;
        }

        /// <summary>
        /// Обрабатывает каждый элемент part из объекта Report
        /// </summary>
        public void ProcessReportParts()
        {
            if (Report == null)
            {
                throw new InvalidOperationException("Объект Report не инициализирован");
            }

            if (Report.Parts == null || Report.Parts.Count == 0)
            {
                MessageBox.Show("В отчете нет элементов для обработки",
                    "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Обрабатываем каждый элемент part
            foreach (ReportPart part in Report.Parts)
            {
                ProcessPart(part);
            }

            // Сохраняем обновлённый Report обратно в XML
            SaveUpdatedReport();
        }

        /// <summary>
        /// Сохраняет обновлённый Report с NewPathModel обратно в XML
        /// </summary>
        private void SaveUpdatedReport()
        {
            Report.Save(reportFilePath);
        }

        /// <summary>
        /// Формирует словарь замен из размеров (Dimensions) и меток (Marks)
        /// </summary>
        /// <param name="part">Элемент для обработки</param>
        /// <returns>Словарь замен: ключ - имя переменной, значение - значение переменной</returns>
        private Dictionary<string, string> BuildReplacementsDictionary(ReportPart part)
        {
            var dictionary = new Dictionary<string, string>();

            if (part == null)
                return dictionary;

            // Добавляем метки из коллекции Marks
            if (part.Marks != null && part.Marks.Count > 0)
            {
                foreach (var mark in part.Marks)
                {
                    if (!string.IsNullOrEmpty(mark.Mark) && !string.IsNullOrEmpty(mark.Value))
                    {
                        dictionary[mark.Mark] = mark.Value;
                    }
                }
            }

            // Добавляем размеры из основной коллекции Dimensions
            if (part.Dimensions != null && part.Dimensions.Count > 0)
            {
                foreach (var dimension in part.Dimensions)
                {
                    if (!string.IsNullOrEmpty(dimension.Dimension) && !string.IsNullOrEmpty(dimension.Value))
                    {
                        dictionary[dimension.Dimension] = dimension.Value;
                    }
                }
            }

            // Добавляем размеры из OptionGroups/Option/Dimensions
            if (part.OptionGroups != null && part.OptionGroups.Count > 0)
            {
                foreach (var optionGroup in part.OptionGroups)
                {
                    if (optionGroup.Options != null && optionGroup.Options.Count > 0)
                    {
                        foreach (var option in optionGroup.Options)
                        {
                            if (option.Dimensions != null && option.Dimensions.Count > 0)
                            {
                                foreach (var dimension in option.Dimensions)
                                {
                                    if (!string.IsNullOrEmpty(dimension.Dimension))
                                    {
                                        // Перезаписываем значение, если ключ уже существует
                                        dictionary[dimension.Name] = dimension.Dimension;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Формирует словарь переменных из элемента ReportPart
        /// </summary>
        /// <param name="part">Элемент для обработки</param>
        /// <returns>Словарь переменных: ключ - имя переменной, значение - значение переменной</returns>
        private Dictionary<string, double> BuildVariablesDictionary(ReportPart part)
        {
            var dictionary = new Dictionary<string, double>();

            if (part == null)
                return dictionary;

            // Добавляем размеры из основной коллекции Dimensions
            if (part.Dimensions != null && part.Dimensions.Count > 0)
            {
                foreach (var dimension in part.Dimensions)
                {
                    if (!string.IsNullOrEmpty(dimension.Dimension) && !string.IsNullOrEmpty(dimension.Value))
                    {
                        dictionary[dimension.Dimension] = Convert.ToDouble(dimension.Value);
                    }
                }
            }

            // Добавляем размеры из OptionGroups/Option/Dimensions
            if (part.OptionGroups != null && part.OptionGroups.Count > 0)
            {
                foreach (var optionGroup in part.OptionGroups)
                {
                    if (optionGroup.Options != null && optionGroup.Options.Count > 0)
                    {
                        foreach (var option in optionGroup.Options)
                        {
                            if (option.Dimensions != null && option.Dimensions.Count > 0)
                            {
                                foreach (var dimension in option.Dimensions)
                                {
                                    if (!string.IsNullOrEmpty(dimension.Dimension))
                                    {
                                        // Перезаписываем значение, если ключ уже существует
                                        dictionary[dimension.Name] = Convert.ToDouble(dimension.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Обрабатывает отдельный элемент part
        /// </summary>
        /// <param name="part">Элемент для обработки</param>
        private void ProcessPart(ReportPart part)
        {
            if (part == null)
                return;

            // Проверяем существование файла модели
            if (!string.IsNullOrEmpty(part.PathModel) && !File.Exists(part.PathModel))
            {
                MessageBox.Show($"Файл модели не найден: {part.PathModel}\nГруппа: {part.Group}",
                    "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Получаю коллекции переменных для подстановки в модель
            dictionaryofreplacements = BuildReplacementsDictionary(part);
            dictionaryvariables = BuildVariablesDictionary(part);

            // Здесь будет логика обработки каждого элемента part
            // Например: загрузка модели в Kompas, применение размеров, и т.д.
            //1.запускаю компас, если есть закрыть и запустить новую сессию
            kompas = kompasRestartService.GetKompasInstance();

            //2.открываем в компас модель по ссылке передаем в неё значения переменных из коллекции и создаем по ней виртуальную структуру.
            try
            {
                application = kompas.ksGetApplication7();
                documents = application.Documents;
                document3D = (IKompasDocument3D)documents.Open(part.PathModel, true, true);
                ksDocument3D = (ksDocument3D)kompas.ActiveDocument3D();

                // Ожидаем полной загрузки документа
                //if (!WaitForDocumentReady())
                //{
                //    MessageBox.Show("Не удалось дождаться загрузки документа Kompas-3D",
                //        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    return;
                //}
            }
            catch (COMException comEx)
            {
                MessageBox.Show($"Ошибка COM при работе с Kompas: {comEx.Message}\nПопробуйте перезапустить приложение.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Заменяем переменные в модели
            ReplaceVariables(document3D, dictionaryvariables);

            // Перестраиваем документ и ждем завершения
            bool flag = ksDocument3D.RebuildDocument();

            if (root != null)
            {
                root = null;
            }
            IPart7 part7 = document3D.TopPart;
            root = PrimaryParse(part7);

            switch (document3D.DocumentType)
            {
                case Kompas6Constants.DocumentTypeEnum.ksDocumentUnknown:
                    break;
                case Kompas6Constants.DocumentTypeEnum.ksDocumentDrawing:
                    break;
                case Kompas6Constants.DocumentTypeEnum.ksDocumentFragment:
                    break;
                case Kompas6Constants.DocumentTypeEnum.ksDocumentSpecification:
                    break;
                case Kompas6Constants.DocumentTypeEnum.ksDocumentPart:
                    break;
                case Kompas6Constants.DocumentTypeEnum.ksDocumentAssembly:
                    {
                        foreach (IPart7 item in part7.Parts)
                        {
                            ksPart ksPart = kompas.TransferInterface(item, 1, 0);
                            if (ksPart.excluded != true)
                            {
                                Recursion(item, root);
                            }
                        }
                        document3D.Close(DocumentCloseOptions.kdDoNotSaveChanges);
                        //3.обрабатываем структуру, с заполнением полей и свойств объектов
                        //RenameTreeNodes(root, dictionaryofreplacements); не нужно больше
                        //4.проходимся по виртуальной структуре начиная со всех деталей
                        TraverseTree(root);
                        //5.сохраняем новый путь к модели в ReportPart
                        part.NewPathModel = root.NewFullName;
                        break;
                    }
                case Kompas6Constants.DocumentTypeEnum.ksDocumentTextual:
                    break;
                case Kompas6Constants.DocumentTypeEnum.ksDocumentTechnologyAssembly:
                    break;
                default:
                    break;
            }


            // Пример вывода информации о детали
            string info = $"Обработка детали:\n" +
                          $"Группа: {part.Group}\n" +
                          $"Путь к модели: {part.PathModel}\n" +
                          $"Количество меток: {part.Marks?.Count ?? 0}\n" +
                          $"Количество размеров: {part.Dimensions?.Count ?? 0}\n" +
                          $"Количество групп опций: {part.OptionGroups?.Count ?? 0}";

            // Временный вывод для отладки
            // MessageBox.Show(info, "Информация о детали", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Заменяет переменные в документе Kompas 3D согласно словарю замен
        /// </summary>
        /// <param name="doc">Документ Kompas 3D</param>
        /// <param name="keyValuePairs">Словарь замен: ключ - имя переменной, значение - новое значение</param>
        private void ReplaceVariables(IKompasDocument3D doc, Dictionary<string, double> keyValuePairs)
        {
            if (doc == null || keyValuePairs == null || keyValuePairs.Count == 0)
                return;

            IPart7 part7 = doc.TopPart;
            IModelObject modelObject = (IModelObject)part7;
            IFeature7 feature7 = modelObject.Owner;

            foreach (var item in keyValuePairs)
            {
                IVariable7 variableDim = feature7.Variable[false, true, item.Key]; //Передал dimension из xml в модель
                if (variableDim != null)
                {
                    variableDim.Value = Convert.ToDouble(item.Value); //Передал в модель значение поля NumericUpDown с формы в модель
                }
            }
            doc.RebuildDocument();
        }

        private void ReplaceVariables(IKompasDocument3D doc, Dictionary<string, double> keyValuePairs, ObjectAssemblyKompas node)
        {
            if (doc == null || keyValuePairs == null || keyValuePairs.Count == 0)
                return;

            IPart7 part7 = doc.TopPart;
            string name = part7.FileName;
            IModelObject modelObject = (IModelObject)part7;
            IFeature7 feature7 = modelObject.Owner;

            foreach (var item in keyValuePairs)
            {
                IVariable7 variableDim = feature7.Variable[false, true, item.Key]; //П

                if (variableDim != null)
                {
                    variableDim.Value = Convert.ToDouble(item.Value); //Передал в модель значение поля NumericUpDown с формы в модель
                }
            }

            doc.RebuildDocument();
            //ksPart ksPar = kompas.TransferInterface(part7, 1, 0);
            //ksPar.marking = node.NewDesignation;
            //ksPar.Update();
        }

        private void ReplaceMaterials(IKompasDocument3D doc, Dictionary<string, string> keyValuePairs)
        {
            if (doc == null || keyValuePairs == null || keyValuePairs.Count == 0)
                return;
            IPart7 part7 = doc.TopPart;
            IPropertyMng propertyMng = (IPropertyMng)application;
            var properties = propertyMng.GetProperties(doc);
            IPropertyKeeper propertyKeeper = (IPropertyKeeper)part7;
            foreach (IProperty item in properties)
            {
                if (item.Name == "isMaterialModified")
                {
                    dynamic propertyValue;
                    bool source;
                    propertyKeeper.GetPropertyValue((_Property)item, out propertyValue, false, out source);

                    string valueStr = Convert.ToString(propertyValue);
                    if (!string.IsNullOrEmpty(valueStr))
                    {
                        IEmbodimentsManager embodimentsManager = (IEmbodimentsManager)doc;
                        IEmbodiment embodiment = embodimentsManager.CurrentEmbodiment;
                        embodiment.SetMaterial(Convert.ToString(keyValuePairs["M"]), Convert.ToDouble(7.85));
                        part7.Update();
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Проверяет, находится ли листовая деталь в развёрнутом состоянии, и сворачивает её
        /// </summary>
        /// <param name="doc">Документ Kompas 3D</param>
        /// <returns>true если деталь была свёрнута, false если не требовалось сворачивание</returns>
        private bool FoldSheetMetalIfUnfolded(IKompasDocument3D doc)
        {
            if (doc == null)
                return false;

            try
            {
                IPart7 part7 = doc.TopPart;
                string name = part7.Name;
                if (part7 == null)
                    return false;

                ISheetMetalContainer sheetMetalContainer = part7 as ISheetMetalContainer;
                if (sheetMetalContainer == null)
                    return false;

                ISheetMetalBendUnfoldParameters unfoldParams = sheetMetalContainer as ISheetMetalBendUnfoldParameters;
                if (unfoldParams == null)
                    return false;

                if (unfoldParams.Unfold)
                {
                    unfoldParams.Unfold = false;
                    doc.RebuildDocument();
                    doc.Save();
                    return true;
                }

                return false;
            }
            catch
            {
                // Деталь не является листовой или произошла ошибка
                return false;
            }
        }

        /// <summary>
        /// Заменяет ссылки на файлы компонентов в сборке на новые пути
        /// </summary>
        /// <param name="doc">Документ Kompas 3D</param>
        /// <param name="node">Узел сборки с дочерними элементами</param>
        private void ReplaceParts(IKompasDocument3D doc, ObjectAssemblyKompas node)
        {
            if (doc == null || node == null)
                return;

            if (node.Children == null || node.Children.Count == 0)
                return;

            IPart7 part7 = doc.TopPart;
            foreach (IPart7 item in part7.Parts)
            {
                string currentFileName = item.FileName;

                // Ищем соответствующий child по совпадению FullName с текущим FileName
                ObjectAssemblyKompas matchingChild = node.Children
                    .FirstOrDefault(child => !string.IsNullOrEmpty(child.FullName) &&
                                            child.FullName.Equals(currentFileName, StringComparison.OrdinalIgnoreCase));

                if (matchingChild != null && !string.IsNullOrEmpty(matchingChild.NewFullName))
                {
                    // Заменяем ссылку на файл на новый путь
                    item.FileName = matchingChild.NewFullName;
                    item.Update();
                }
            }
            doc.RebuildDocument();
        }

        private ObjectAssemblyKompas PrimaryParse(IPart7 part7)
        {
            var ObjectKompas = new ObjectAssemblyKompas();
            ObjectKompas.FullName = part7.FileName;
            ObjectKompas.Designation = part7.Marking;
            ObjectKompas.Name = part7.Name;
            ObjectKompas.IsLocal = part7.IsLocal;
            #region Заполняю IsDetail
            ksPart ksPart = kompas.TransferInterface(part7, 1, 0);
            ObjectKompas.IsDetail = ksPart.IsDetail();
            #endregion

            if (ObjectKompas.IsLocal == true)
            {
                IPropertyMng propertyMng = (IPropertyMng)application;
                var properties = propertyMng.GetProperties(document3D);
                IPropertyKeeper propertyKeeper = (IPropertyKeeper)part7;
                foreach (IProperty item in properties)
                {
                    if (item.Name == "Раздел спецификации")
                    {
                        dynamic info;
                        bool source;
                        propertyKeeper.GetPropertyValue((_Property)item, out info, false, out source);
                        ObjectKompas.SpecificationSection = info;
                    }
                }
            }
            return ObjectKompas;
        }

        private void Recursion(IPart7 Part, ObjectAssemblyKompas parent)
        {
            ObjectAssemblyKompas objectF = parent.FindChild(Part.Marking, Part.Name);

            if (objectF != null)
            {
                return;
            }
            else
            {
                var objectAssemblyKompas = PrimaryParse(Part);
                objectAssemblyKompas.Parent = parent;
                parent.AddChild(objectAssemblyKompas);

                //DisassembleObject(Part, ParentName);
                if (objectAssemblyKompas.Designation != "" || objectAssemblyKompas.Designation != String.Empty)//заглушка не добавлять детей для детелей у которых нет обозначения
                {
                    foreach (IPart7 item in Part.Parts)
                    {
                        ksPart ksPart2 = kompas.TransferInterface(item, 1, 0);
                        if (ksPart2.excluded != true)
                        {
                            if (item.Detail) objectAssemblyKompas.AddChild(PrimaryParse(item));
                            else Recursion(item, objectAssemblyKompas);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Переименовывает узел дерева с дополнительными параметрами из документа Kompas
        /// </summary>
        /// <param name="node">Узел для обработки</param>
        /// <param name="keyValuePairs">Словарь замен: ключ - текст для поиска, значение - текст для замены</param>
        /// <param name="doc">Документ Kompas 3D для получения дополнительных данных</param>
        public void RenameNode(ObjectAssemblyKompas node, Dictionary<string, string> keyValuePairs, IKompasDocument3D doc3D)
        {
            if (node == null || keyValuePairs == null || doc3D == null)
                return;

            // Создаем новый словарь и копируем в него переданный
            Dictionary<string, string> extendedDictionary = new Dictionary<string, string>(keyValuePairs);

            IPart7 part7 = doc3D.TopPart;
            IModelObject modelObject = (IModelObject)part7;
            IFeature7 feature7 = modelObject.Owner;

            // TODO: Дополняем новый словарь дополнительными ключами и значениями из документа
            if (node.Designation.Contains("Smm"))
            {
                string Thick = "";
                ISheetMetalContainer sheetMetalContainer = part7 as ISheetMetalContainer;
                ISheetMetalBodies sheetMetalBodies = sheetMetalContainer.SheetMetalBodies;
                int bodies = sheetMetalBodies.Count;
                ISheetMetalBody sheetMetalBody = sheetMetalBodies.SheetMetalBody[0];
                IVariable7 variable7 = feature7.Variable[false, true, 0];
                Thick = variable7.Value.ToString(CultureInfo.CreateSpecificCulture("en-GB"));
                extendedDictionary.Add("S", Thick);
            }
            if (node.Designation.Contains("Ld"))
            {
                IVariable7 variable7 = feature7.Variable[false, true, "Ld"];
                double value = variable7.Value;
                extendedDictionary.Add("Ld", value.ToString());
            }
            if (node.Designation.Contains("Lw"))
            {
                IVariable7 variable7 = feature7.Variable[false, true, "Lw"];
                double value = variable7.Value;
                extendedDictionary.Add("Lw", value.ToString());
            }
            if (node.Designation.Contains("Lh"))
            {
                IVariable7 variable7 = feature7.Variable[false, true, "Lh"];
                double value = variable7.Value;
                extendedDictionary.Add("Lh", value.ToString());
            }
            // Например:
            // extendedDictionary["ключ1"] = "значение1";
            // extendedDictionary["ключ2"] = "значение2";

            // Обрабатываем текущий узел с расширенным словарем, здесь и происходит магия замены
            ProcessNode(node, extendedDictionary);

            ksPart ksPar = kompas.TransferInterface(part7, 1, 0);
            ksPar.marking = node.NewDesignation;
            ksPar.Update();
        }

        /// <summary>
        /// Обрабатывает один узел: заменяет текст в Designation и Name согласно словарю
        /// </summary>
        /// <param name="node">Узел для обработки</param>
        /// <param name="keyValuePairs">Словарь замен</param>
        private void ProcessNode(ObjectAssemblyKompas node, Dictionary<string, string> keyValuePairs)
        {
            // Обрабатываем Designation -> NewDesignation
            if (!string.IsNullOrEmpty(node.Designation))
            {
                string newDesignation = node.Designation;
                foreach (var kvp in keyValuePairs)
                {
                    if (!string.IsNullOrEmpty(kvp.Key))
                    {
                        newDesignation = newDesignation.Replace(kvp.Key, kvp.Value);
                    }
                }
                node.NewDesignation = newDesignation;
            }
            else
            {
                node.NewDesignation = string.Empty;
            }

            // Обрабатываем Name -> NewName
            if (!string.IsNullOrEmpty(node.Name))
            {
                string newName = node.Name;
                foreach (var kvp in keyValuePairs)
                {
                    if (!string.IsNullOrEmpty(kvp.Key))
                    {
                        newName = newName.Replace(kvp.Key, kvp.Value);
                    }
                }
                node.NewName = newName;
            }
            else
            {
                node.NewName = string.Empty;
            }

            // Обрабатываем FullName -> NewFullName
            if (!string.IsNullOrEmpty(node.FullName))
            {
                // Получаем только имя файла с расширением
                string fileName = Path.GetFileName(node.FullName);

                // Обрабатываем имя файла так же, как Designation и Name
                string newFileName = fileName;
                foreach (var kvp in keyValuePairs)
                {
                    if (!string.IsNullOrEmpty(kvp.Key))
                    {
                        newFileName = newFileName.Replace(kvp.Key, kvp.Value);
                    }
                }

                // Объединяем новую директорию из ExportPath и обработанное имя файла
                if (!string.IsNullOrEmpty(ExportPath))
                {
                    node.NewFullName = Path.Combine(ExportPath, newFileName);
                }
                else
                {
                    node.NewFullName = newFileName;
                }

                // Формируем имя файла из NewDesignation
                string baseFileName = !string.IsNullOrEmpty(node.NewDesignation) ? node.NewDesignation : Path.GetFileNameWithoutExtension(newFileName);

                // Обрабатываем DxfFilePath (NewDesignation + .dxf)
                string dxfFileName = baseFileName + ".dxf";

                if (!string.IsNullOrEmpty(ExportPath))
                {
                    node.DxfFilePath = Path.Combine(ExportPath, dxfFileName);
                }
                else
                {
                    node.DxfFilePath = dxfFileName;
                }

                // Определяем базовое имя для PDF и CDW в зависимости от наличия children
                // Если есть children (сборка) - используем формат "Обозначение СБ - Наименование"
                // Если нет children (деталь) - используем только NewDesignation
                bool hasChildren = node.Children != null && node.Children.Count > 0;
                string drawingBaseFileName;

                if (hasChildren && !string.IsNullOrEmpty(node.NewDesignation) && !string.IsNullOrEmpty(node.NewName))
                {
                    drawingBaseFileName = $"{node.NewDesignation} СБ - {node.NewName}";
                }
                else
                {
                    drawingBaseFileName = baseFileName;
                }

                // Обрабатываем PdfFilePath
                string pdfFileName = drawingBaseFileName + ".pdf";

                if (!string.IsNullOrEmpty(ExportPath))
                {
                    node.PdfFilePath = Path.Combine(ExportPath, pdfFileName);
                }
                else
                {
                    node.PdfFilePath = pdfFileName;
                }

                // Обрабатываем NewDrawingName (cdwFileName)
                string cdwFileName = drawingBaseFileName + ".cdw";

                if (!string.IsNullOrEmpty(ExportPath))
                {
                    node.NewDrawingName = Path.Combine(ExportPath, cdwFileName);
                }
                else
                {
                    node.NewDrawingName = cdwFileName;
                }
            }
            else
            {
                node.NewFullName = string.Empty;
                node.DxfFilePath = string.Empty;
                node.PdfFilePath = string.Empty;
            }
        }

        /// <summary>
        /// Обходит дерево в обратном порядке (от листьев к корню) - сначала обрабатывает детали без детей, потом их родителей и т.д.
        /// </summary>
        /// <param name="root">Корневой узел дерева</param>
        public void TraverseTree(ObjectAssemblyKompas root)
        {
            if (root == null)
                return;

            // Рекурсивно обрабатываем сначала всех детей
            if (root.Children != null && root.Children.Count > 0)
            {
                foreach (var child in root.Children)
                {
                    TraverseTree(child);
                }
            }

            // Затем обрабатываем текущий узел (после обработки всех детей)
            // Благодаря рекурсии порядок обработки: листочки → их родители → родители родителей → root
            ProcessTreeNode(root);
        }

        /// <summary>
        /// Экспортирует документ Kompas в формат DXF
        /// </summary>
        /// <param name="doc">Документ Kompas 3D для экспорта</param>
        /// <param name="node">Узел с информацией о детали</param>
        private void ExportDxf(IKompasDocument3D doc, ObjectAssemblyKompas node)
        {
            if (doc == null || node == null)
                return;
            if (!node.Designation.Contains("Smm"))
            {
                return;
            }
            application.HideMessage = ksHideMessageEnum.ksHideMessageYes; //Скрываем все сообщения системы -Да
            try
            {
                ksDocument3D = kompas.ActiveDocument3D();
                string projection = String.Empty;
                ksViewProjectionCollection ksViewProjectionCollection = ksDocument3D.GetViewProjectionCollection();
                for (int i = 0; i < ksViewProjectionCollection.GetCount(); i++)
                {
                    ksViewProjection ksViewProjection = ksViewProjectionCollection.GetByIndex(i);
                    if (ksViewProjection.name == "R" && dictionaryofreplacements["Side"] == "R")
                    {
                        projection = "R";
                    }
                    if (ksViewProjection.name == "L" && dictionaryofreplacements["Side"] == "L")
                    {
                        projection = "L";
                    }
                    if (ksViewProjection.name == "Развертка" && i > 0)
                    {
                        projection = "#Развертка";
                    }
                }
                ksDocumentParam documentParam = (ksDocumentParam)kompas.GetParamStruct(35);
                documentParam.type = 1;
                documentParam.Init();
                ksDocument2D document2D = (ksDocument2D)kompas.Document2D();
                document2D.ksCreateDocument(documentParam);
                IKompasDocument2D kompasDocument2D = (IKompasDocument2D)application.ActiveDocument;
                IViewsAndLayersManager viewsAndLayersManager = kompasDocument2D.ViewsAndLayersManager;
                IViews views = viewsAndLayersManager.Views;
                IView pView = views.Add(Kompas6Constants.LtViewType.vt_Arbitrary);
                IAssociationView pAssociationView = pView as IAssociationView;
                IPart7 part7 = doc.TopPart;
                pAssociationView.SourceFileName = part7.FileName;

                //скрываю оси при создании dxf
                IAssociationViewElements associationViewElements = (IAssociationViewElements)pAssociationView;
                associationViewElements.CreateCircularCentres = false;
                associationViewElements.CreateLinearCentres = false;
                associationViewElements.CreateAxis = false;
                associationViewElements.CreateCentresMarkers = false;
                associationViewElements.ProjectAxis = false;
                associationViewElements.ProjectDesTexts = false;

                IEmbodimentsManager embodimentsManager = (IEmbodimentsManager)doc;
                int indexPart = embodimentsManager.CurrentEmbodimentIndex;
                IEmbodimentsManager emb = (IEmbodimentsManager)pAssociationView;
                emb.SetCurrentEmbodiment(indexPart);
                pAssociationView.Angle = 0;
                pAssociationView.X = 0;
                pAssociationView.Y = 0;
                pAssociationView.BendLinesVisible = false;
                pAssociationView.BreakLinesVisible = false;
                pAssociationView.HiddenLinesVisible = false;
                pAssociationView.VisibleLinesStyle = (int)ksCurveStyleEnum.ksCSNormal;
                pAssociationView.Scale = 1;
                pAssociationView.Name = "User view";
                pAssociationView.ProjectionName = projection;
                pAssociationView.Unfold = true; //развернутый вид
                pAssociationView.BendLinesVisible = false;
                pAssociationView.CenterLinesVisible = false;
                pAssociationView.SourceFileName = part7.FileName;
                pAssociationView.Update();
                pView.Update();
                IViewDesignation pViewDesignation = pView as IViewDesignation;
                pViewDesignation.ShowUnfold = false;
                pViewDesignation.ShowScale = false;
                pView.Update();
                document2D.ksRebuildDocument();
                document2D.ksSaveToDXF(node.DxfFilePath);
                document2D.ksCloseDocument();
            }

            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в DXF для '{node.Name}': {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        /// <summary>
        /// Экспортирует документ Kompas в формат PDF
        /// </summary>
        /// <param name="doc">Документ Kompas 3D для экспорта</param>
        /// <param name="node">Узел с информацией о детали</param>
        private void ExportPdf(IKompasDocument3D doc, ObjectAssemblyKompas node)
        {
            if (doc == null || node == null || node.DrawingReferences.Count < 1)
                return;

            try
            {
                IModelObject modelObject = (IModelObject)doc.TopPart;
                IFeature7 feature7 = modelObject.Owner;
                IVariable7 variable7 = feature7.Variable[false, true, "Draw"];
                double numberDraw = 0;
                if (variable7 != null)
                {
                    numberDraw = variable7.Value;
                }

                IDocuments document = application.Documents;
                IKompasDocument kompasDocument = document.Open(
                    node.DrawingReferences[Convert.ToInt32(numberDraw)], true, true);

                if (kompasDocument == null)
                {
                    MessageBox.Show($"У детали {node.Name} отсутствует привязанный чертеж",
                        "Уведомление", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                IKompasDocument2D kompasDocument2D = (IKompasDocument2D)kompasDocument;
                IViewsAndLayersManager viewsAndLayersManager = kompasDocument2D.ViewsAndLayersManager;
                IViews views = viewsAndLayersManager.Views;
                IKompasDocument2D1 kompasDocument2D1 = (IKompasDocument2D1)kompasDocument2D;

                List<IView> hideViews = new List<IView>();

                for (int i = 0; i < views.Count; i++)
                {
                    IView view = views.View[i];
                    if (view == null)
                        continue;

                    // Сначала запоминаем скрытые виды и делаем их видимыми
                    bool wasHidden = (view.Visible == false);
                    if (wasHidden)
                    {
                        view.Visible = true;
                        view.Update();
                        hideViews.Add(view);
                    }

                    // Теперь устанавливаем источник
                    IAssociationView pAssociationView = view as IAssociationView;
                    if (pAssociationView != null)
                    {
                        //pAssociationView.BendLinesStyle = (int)ksCurveStyleEnum.ksCSDashed;                        
                        pAssociationView.SourceFileName = node.NewFullName;
                        pAssociationView.Update();
                        //kompasDocument2D1.RebuildDocument();
                        //pAssociationView.UseOcclusion = false;
                        //pAssociationView.Update();
                        //pAssociationView.UseOcclusion = true;
                        //pAssociationView.Update();
                    }
                }

                kompasDocument2D1.RebuildDocument();

                // Скрываем обратно виды, которые были скрыты
                foreach (var hideView in hideViews)
                {
                    hideView.Visible = false;
                    hideView.Update();
                }

                kompasDocument2D1.RebuildDocument();

                kompasDocument.SaveAs(node.PdfFilePath);
                kompasDocument.SaveAs(node.NewDrawingName);
                FoldSheetMetalIfUnfolded(doc);

                kompasDocument.Close(DocumentCloseOptions.kdDoNotSaveChanges);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в PDF для '{node.Name}': {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Получает массив чертежей из модели и заносит ссылки в узел
        /// </summary>
        /// <param name="node">Узел для сохранения ссылок на чертежи</param>
        /// <param name="doc">Документ Kompas 3D</param>
        private void GetDrawingReferences(ObjectAssemblyKompas node, IKompasDocument3D doc)
        {
            if (node == null || doc == null)
                return;

            // Инициализируем список для хранения ссылок на чертежи
            node.DrawingReferences = new List<string>();

            try
            {
                // Получаем чертежи из модели
                IProductDataManager productDataManager = (IProductDataManager)doc;
                string[] arrayDrawings = productDataManager.ObjectAttachedDocuments[(IPropertyKeeper)doc];

                if (arrayDrawings != null && arrayDrawings.Length > 0)
                {
                    // Фильтруем только файлы чертежей (.cdw) и добавляем в список
                    var filteredDrawings = arrayDrawings
                        .Where(s => !string.IsNullOrEmpty(s) && s.EndsWith(".cdw", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    // Используем AddRange вместо обращения по индексу
                    node.DrawingReferences.AddRange(filteredDrawings);
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не прерываем выполнение
                // MessageBox можно раскомментировать для отладки
                // MessageBox.Show($"Ошибка при получении чертежей для '{node.Name}': {ex.Message}",
                //     "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Обрабатывает узел дерева - открывает документ
        /// </summary>
        /// <param name="node">Узел для обработки</param>
        private void ProcessTreeNode(ObjectAssemblyKompas node)
        {
            if (node == null || string.IsNullOrEmpty(node.FullName))
                return;

            // Проверяем, является ли узел листочком (нет детей)
            bool isLeaf = node.Children == null || node.Children.Count == 0;

            try
            {
                // Открываем документ в Kompas
                if (documents != null)
                {
                    IKompasDocument3D doc;
                    if (isLeaf)
                    {
                        // Логика для листочков (деталей)
                        doc = (IKompasDocument3D)documents.Open(node.FullName, true, true);
                        GetDrawingReferences(node, doc);
                        ReplaceVariables(doc, dictionaryvariables, node);
                        RenameNode(node, dictionaryofreplacements, doc);
                        ReplaceMaterials(doc, dictionaryofreplacements);

                        doc.SaveAs(node.NewFullName);
                        ExportDxf(doc, node);
                        ExportPdf(doc, node);
                        //kompas.ksMessage($"Отлично загружено {node.Designation}");
                        doc.Close(DocumentCloseOptions.kdDoNotSaveChanges);
                    }
                    else
                    {
                        // Логика для сборок (родителей)
                        doc = (IKompasDocument3D)documents.Open(node.FullName, true, false);
                        GetDrawingReferences(node, doc);
                        ReplaceVariables(doc, dictionaryvariables, node);
                        RenameNode(node, dictionaryofreplacements, doc);

                        ReplaceParts(doc, node);
                        doc.SaveAs(node.NewFullName);
                        ExportPdf(doc, node);
                        doc.Close(DocumentCloseOptions.kdDoNotSaveChanges);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обработке узла '{node.Name}': {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Создаёт новую пустую сборку в KOMPAS-3D с заданным обозначением и наименованием
        /// </summary>
        /// <returns>Путь к созданному файлу сборки или null в случае ошибки</returns>
        public string CreateEmptyAssembly()
        {
            // 1. Получить значение Mark.Value (номер заказа) из Report
            string markValue = GetOrderNumber();
            if (string.IsNullOrEmpty(markValue))
            {
                MessageBox.Show("Не удалось получить номер заказа из Report",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            // 2. Сформировать имя файла по маске
            string designation = $"500.00.00_{markValue}";
            string name = "Купе";
            string fileName = $"{designation} - {name}.a3d";
            string filePath = Path.Combine(ExportPath, fileName);

            // 3. Подключиться к KOMPAS (если ещё не подключены)
            if (kompas == null)
            {
                kompas = kompasRestartService.GetKompasInstance();
                if (kompas == null)
                {
                    MessageBox.Show("Не удалось подключиться к KOMPAS-3D",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
                application = kompas.ksGetApplication7();
                documents = application.Documents;
            }

            try
            {
                // 4. Создать новый документ-сборку
                IKompasDocument3D newAssembly = (IKompasDocument3D)documents.Add(
                    Kompas6Constants.DocumentTypeEnum.ksDocumentAssembly, true);

                if (newAssembly == null)
                {
                    MessageBox.Show("Не удалось создать новую сборку",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }

                // 5. Задать обозначение и наименование через ksPart
                IPart7 part7 = newAssembly.TopPart;
                ksPart topPart = kompas.TransferInterface(part7, 1, 0);
                topPart.marking = designation;  // Обозначение
                topPart.name = name;            // Наименование
                topPart.Update();

                // 6. Добавить компоненты из Report.Parts
                IParts7 parts = part7.Parts;
                foreach (var reportPart in Report.Parts)
                {
                    if (!string.IsNullOrEmpty(reportPart.NewPathModel) && File.Exists(reportPart.NewPathModel))
                    {
                        // Добавляем компонент из файла
                        IPart7 insertedPart = parts.AddFromFile(reportPart.NewPathModel, true, false);

                        if (insertedPart != null)
                        {
                            // Устанавливаем положение компонента (0, 0, 0)
                            Placement3D placement3D = insertedPart.Placement;
                            placement3D.SetOrigin(0, 0, 0);
                            insertedPart.UpdatePlacement(true);
                            insertedPart.Fixed = true;
                            IModelObject modelObject = (IModelObject)insertedPart;
                            modelObject.Update();
                        }
                    }
                }

                // 7. Перестраиваем и сохраняем сборку
                newAssembly.RebuildDocument();
                newAssembly.SaveAs(filePath);

                return filePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании сборки: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// Получает номер заказа (Mark.Value с mark="Number") из первого part в Report
        /// </summary>
        private string GetOrderNumber()
        {
            if (Report?.Parts == null || Report.Parts.Count == 0)
                return null;

            // Ищем Mark с mark="Number" в любом part
            foreach (var part in Report.Parts)
            {
                if (part.Marks != null)
                {
                    var orderMark = part.Marks.Find(m => m.Mark == "Number");
                    if (orderMark != null && !string.IsNullOrEmpty(orderMark.Value))
                    {
                        return orderMark.Value;
                    }
                }
            }
            return null;
        }

        // Цвета-маркеры для определения свойств деталей (BGR формат)
        private const int ColorNeedsRework = 0x0000FF;       // Красный
        private const int ColorModifierFastener = 0x00FF00;  // Зелёный

        /// <summary>
        /// Сканирует открытую сборку и заполняет списки деталей с особыми свойствами.
        /// Определяет свойства по цвету детали через IColorParam7 (без открытия документов).
        /// </summary>
        public void ScanAssemblyForSpecialParts()
        {
            NeedsReworkParts.Clear();
            ModifierFastenerParts.Clear();

            if (kompas == null)
            {
                kompas = kompasRestartService.GetKompasInstance();
                if (kompas == null)
                {
                    MessageBox.Show("Не удалось подключиться к KOMPAS-3D",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                application = kompas.ksGetApplication7();
                documents = application.Documents;
            }

            IKompasDocument3D activeDoc = (IKompasDocument3D)application.ActiveDocument;
            if (activeDoc == null || activeDoc.DocumentType != Kompas6Constants.DocumentTypeEnum.ksDocumentAssembly)
            {
                MessageBox.Show("Активный документ не является сборкой",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            IPart7 topPart = activeDoc.TopPart;
            ScanPartsRecursively(topPart);
        }

        /// <summary>
        /// Рекурсивно сканирует компоненты сборки, определяя свойства деталей по их цвету.
        /// </summary>
        private void ScanPartsRecursively(IPart7 part)
        {
            foreach (IPart7 item in part.Parts)
            {
                ksPart ksPart = kompas.TransferInterface(item, 1, 0);
                if (ksPart.excluded)
                    continue;

                if (item.Detail)
                {
                    var obj = CreateObjectFromPart(item);

                    if (obj.NeedsRework)
                        NeedsReworkParts.Add(obj);

                    if (obj.ModifierFastener)
                        ModifierFastenerParts.Add(obj);
                }
                else
                {
                    ScanPartsRecursively(item);
                }
            }
        }

        /// <summary>
        /// Создаёт ObjectAssemblyKompas из детали, определяя свойства по цвету через IColorParam7.
        /// Получает расположение и ориентацию детали в сборке.
        /// </summary>
        private ObjectAssemblyKompas CreateObjectFromPart(IPart7 item)
        {
            var obj = new ObjectAssemblyKompas
            {
                FullName = item.FileName,
                Designation = item.Marking,
                Name = item.Name,
                IsDetail = true
            };

            // Получаем матрицу трансформации (содержит и расположение, и ориентацию)
            try
            {
                IPlacement3D placement3D = (IPlacement3D)item.Placement;

                // Получаем матрицу трансформации 4x4 через GetMatrix3D
                object matrixObj = placement3D.GetMatrix3D();
                if (matrixObj != null)
                {
                    double[] matrix = (double[])matrixObj;
                    obj.TransformMatrix = matrix;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении матрицы трансформации: {ex.Message}",
                        "Ошибка",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
            }

            // Определяем свойства по цвету детали
            try
            {
                IColorParam7 colorParam = (IColorParam7)item;
                int color = colorParam.Color;

                obj.NeedsRework = (color == ColorNeedsRework);
                obj.ModifierFastener = (color == ColorModifierFastener);
            }
            catch (Exception)
            {
                // Если не удалось получить цвет, оставляем false
            }

            return obj;
        }

        /// <summary>
        /// Вставляет геометрию крепежа в детали, требующие доработки
        /// </summary>
        public void InsertFastenersIntoReworkParts()
        {
            if (NeedsReworkParts.Count == 0 || ModifierFastenerParts.Count == 0)
                return;

            // Кэш обработанных деталей по пути
            HashSet<string> processedParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var reworkPart in NeedsReworkParts)
            {
                if (string.IsNullOrEmpty(reworkPart.FullName))
                    continue;

                if (processedParts.Contains(reworkPart.FullName))
                    continue;
                processedParts.Add(reworkPart.FullName);

                // Открываем документ детали через API7
                IKompasDocument3D partDoc = (IKompasDocument3D)documents.Open(
                    reworkPart.FullName, true, false);
                if (partDoc == null)
                    continue;

                IPart7 topPart = partDoc.TopPart;
                if (topPart == null)
                    continue;

                IParts7 parts = topPart.Parts;
                if (parts == null)
                    continue;

                // Список для хранения всех вставленных деталей-инструментов
                List<IPart7> insertedTools = new List<IPart7>();

                // Сначала добавляем все детали из ModifierFastenerParts
                foreach (var fastener in ModifierFastenerParts)
                {
                    if (string.IsNullOrEmpty(fastener.FullName))
                        continue;

                    // Вставляем крепёж
                    IPart7 insertedPart = parts.AddFromFile(fastener.FullName, true, false);
                    if (insertedPart == null)
                        continue;

                    // Устанавливаем положение через матрицу трансформации
                    if (fastener.TransformMatrix != null)
                    {
                        IPlacement3D placement = (IPlacement3D)insertedPart.Placement;
                        if (placement != null)
                        {
                            placement.InitByMatrix3D(fastener.TransformMatrix);
                            insertedPart.UpdatePlacement(true);
                        }
                    }

                    IModelObject modelObj = (IModelObject)insertedPart;
                    modelObj.Update();

                    // Добавляем в список для последующей булевой операции
                    insertedTools.Add(insertedPart);
                }

                // Выполняем одну булеву операцию вычитания со всеми инструментами
                if (insertedTools.Count > 0)
                {
                    PerformBooleanSubtract(topPart, insertedTools);
                }

                topPart.RebuildModel(true);

                // Экспорт DXF с проекции "#Развертка"
                ExportDxfFromUnfold(partDoc, reworkPart);

                IKompasDocument kompasDoc = (IKompasDocument)partDoc;
                kompasDoc.Save();
                kompasDoc.Close(DocumentCloseOptions.kdDoNotSaveChanges);
            }
        }

        /// <summary>
        /// Экспортирует DXF с проекции "#Развертка" из открытой модели
        /// </summary>
        /// <param name="doc">Открытый документ Kompas 3D</param>
        /// <param name="node">Узел с информацией о детали</param>
        private void ExportDxfFromUnfold(IKompasDocument3D doc, ObjectAssemblyKompas node)
        {
            if (doc == null || node == null)
                return;

            application.HideMessage = ksHideMessageEnum.ksHideMessageYes;

            try
            {
                ksDocument3D ksDoc3D = kompas.ActiveDocument3D();
                IPart7 part7 = doc.TopPart;

                // Формируем путь для DXF: та же папка, что и модель, имя файла = обозначение детали
                string modelDirectory = Path.GetDirectoryName(node.FullName);
                string designation = !string.IsNullOrEmpty(part7.Marking) ? part7.Marking : Path.GetFileNameWithoutExtension(node.FullName);
                string dxfFilePath = Path.Combine(modelDirectory, designation + ".dxf");

                // Создаём временный 2D-документ (фрагмент)
                ksDocumentParam documentParam = (ksDocumentParam)kompas.GetParamStruct(35);
                documentParam.type = 1;
                documentParam.Init();
                ksDocument2D document2D = (ksDocument2D)kompas.Document2D();
                document2D.ksCreateDocument(documentParam);

                IKompasDocument2D kompasDocument2D = (IKompasDocument2D)application.ActiveDocument;
                IViewsAndLayersManager viewsAndLayersManager = kompasDocument2D.ViewsAndLayersManager;
                IViews views = viewsAndLayersManager.Views;
                IView pView = views.Add(Kompas6Constants.LtViewType.vt_Arbitrary);
                IAssociationView pAssociationView = pView as IAssociationView;

                pAssociationView.SourceFileName = part7.FileName;

                IEmbodimentsManager embodimentsManager = (IEmbodimentsManager)doc;
                int indexPart = embodimentsManager.CurrentEmbodimentIndex;
                IEmbodimentsManager emb = (IEmbodimentsManager)pAssociationView;
                emb.SetCurrentEmbodiment(indexPart);

                pAssociationView.Angle = 0;
                pAssociationView.X = 0;
                pAssociationView.Y = 0;
                pAssociationView.BendLinesVisible = false;
                pAssociationView.BreakLinesVisible = false;
                pAssociationView.HiddenLinesVisible = false;
                pAssociationView.VisibleLinesStyle = (int)ksCurveStyleEnum.ksCSNormal;
                pAssociationView.Scale = 1;
                pAssociationView.Name = "Unfold view";

                // Определяем имя проекции на основе маркировки детали
                string projectionName = "#Развертка";
                string marking = part7.Marking ?? string.Empty;
                if (marking.Contains("R"))
                {
                    projectionName = "R";
                }
                else if (marking.Contains("L"))
                {
                    projectionName = "L";
                }
                pAssociationView.ProjectionName = projectionName;

                pAssociationView.Unfold = true;
                pAssociationView.BendLinesVisible = false;
                pAssociationView.CenterLinesVisible = false;
                pAssociationView.SourceFileName = part7.FileName;
                pAssociationView.Update();
                pView.Update();

                IViewDesignation pViewDesignation = pView as IViewDesignation;
                pViewDesignation.ShowUnfold = false;
                pViewDesignation.ShowScale = false;
                pView.Update();

                document2D.ksRebuildDocument();
                document2D.ksSaveToDXF(dxfFilePath);
                document2D.ksCloseDocument();
                FoldSheetMetalIfUnfolded(doc);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в DXF для '{node.Name}': {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Выполняет булеву операцию вычитания (Difference) тел крепежа из детали
        /// </summary>
        /// <param name="targetPart">Целевая деталь (из которой вычитаем)</param>
        /// <param name="toolParts">Список тел-инструментов (которые вычитаем)</param>
        private void PerformBooleanSubtract(IPart7 targetPart, List<IPart7> toolParts)
        {
            if (targetPart == null || toolParts == null || toolParts.Count == 0)
                return;

            try
            {
                // Получаем IModelContainer от IPart7
                IModelContainer modelContainer = (IModelContainer)targetPart;
                if (modelContainer == null)
                    return;

                // Получаем коллекцию булевых операций
                IBooleans booleans = modelContainer.Booleans;
                if (booleans == null)
                    return;

                // Добавляем новую булеву операцию
                IBoolean booleanOp = booleans.Add();
                if (booleanOp == null)
                    return;

                // Устанавливаем тип операции - вычитание (Difference)
                booleanOp.BooleanType = ksBooleanType.ksDifference;

                // Получаем тело базовой детали через IFeature7.ResultBodies
                IModelObject targetModelObj = (IModelObject)targetPart;
                IFeature7 targetFeature = (IFeature7)targetModelObj.Owner;

                var targetBodies = targetFeature.ResultBodies;
                IBody7 targetBody = GetFirstBody(targetBodies);
                if (targetBody != null)
                {
                    booleanOp.BaseObject = targetBody;
                }

                // Собираем все тела из списка инструментов
                List<IBody7> allToolBodies = new List<IBody7>();
                foreach (var toolPart in toolParts)
                {
                    IModelObject toolModelObj = (IModelObject)toolPart;
                    IFeature7 toolFeature = (IFeature7)toolModelObj.Owner;
                    var toolBodies = toolFeature.ResultBodies;

                    // Добавляем все тела из каждого инструмента
                    AddAllBodies(toolBodies, allToolBodies);
                }

                // Устанавливаем все модифицирующие объекты (тела для вычитания)
                if (allToolBodies.Count > 0)
                {
                    booleanOp.ModifyObjects = allToolBodies.ToArray();
                }

                // Обновляем операцию
                IModelObject modelObject = (IModelObject)booleanOp;
                modelObject.Update();

                // Проверяем, не появилась ли ошибка в дереве построения
                IFeature7 booleanFeature = modelObject.Owner;
                if (booleanFeature != null)
                {
                    // Проверяем код ошибки (0 = нет ошибки)
                    long errorCode = booleanFeature.ObjectError;
                    bool isValid = booleanFeature.Valid;

                    if (errorCode != 0 || !isValid)
                    {
                        // Погашаем элемент с ошибкой (исключаем из расчёта)
                        booleanFeature.Excluded = true;

                        // Гасим модифицирующие объекты (детали-инструменты) в дереве
                        foreach (var toolPart in toolParts)
                        {
                            try
                            {
                                IModelObject toolModelObj = (IModelObject)toolPart;
                                IFeature7 toolFeature = toolModelObj.Owner;
                                if (toolFeature != null)
                                {
                                    toolFeature.Excluded = true;
                                }
                            }
                            catch
                            {
                                // Не удалось погасить инструмент
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выполнении булевой операции: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Извлекает первое тело из VARIANT (может быть один объект или массив)
        /// </summary>
        /// <param name="bodies">VARIANT с телом/телами</param>
        /// <returns>Первое тело IBody7 или null</returns>
        private IBody7 GetFirstBody(object bodies)
        {
            if (bodies == null)
                return null;

            try
            {
                // Если это массив объектов
                if (bodies is object[] bodyArray)
                {
                    if (bodyArray.Length > 0)
                        return (IBody7)bodyArray[0];
                }
                // Если это Array (SAFEARRAY)
                else if (bodies is Array array)
                {
                    if (array.Length > 0)
                        return (IBody7)array.GetValue(0);
                }
                // Если это один объект (VT_DISPATCH)
                else
                {
                    return (IBody7)bodies;
                }
            }
            catch
            {
                // Не удалось привести к IBody7
            }

            return null;
        }

        /// <summary>
        /// Добавляет все тела из VARIANT в список
        /// </summary>
        /// <param name="bodies">VARIANT с телом/телами</param>
        /// <param name="targetList">Список для добавления тел</param>
        private void AddAllBodies(object bodies, List<IBody7> targetList)
        {
            if (bodies == null || targetList == null)
                return;

            try
            {
                // Если это массив объектов
                if (bodies is object[] bodyArray)
                {
                    foreach (var item in bodyArray)
                    {
                        if (item is IBody7 body)
                            targetList.Add(body);
                    }
                }
                // Если это Array (SAFEARRAY)
                else if (bodies is Array array)
                {
                    foreach (var item in array)
                    {
                        if (item is IBody7 body)
                            targetList.Add(body);
                    }
                }
                // Если это один объект (VT_DISPATCH)
                else if (bodies is IBody7 singleBody)
                {
                    targetList.Add(singleBody);
                }
            }
            catch
            {
                // Не удалось добавить тела
            }
        }

    }
}