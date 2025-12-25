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

        /// <summary>
        /// Объект отчета, загруженный из XML
        /// </summary>
        public Report Report { get; private set; }

        /// <summary>
        /// Путь для выгрузки файлов
        /// </summary>
        public string ExportPath { get; set; }

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
            string latestReportFile = xmlFiles
                .OrderByDescending(f => File.GetCreationTime(f))
                .First();

            // Десериализуем XML-файл в объект Report
            Report = Report.Load(latestReportFile);
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
            ksPart ksPar = kompas.TransferInterface(part7, 1, 0);
            ksPar.marking = node.NewDesignation;
            ksPar.Update();
        }

        /// <summary>
        /// Заменяет переменные в документе Kompas 3D согласно словарю замен
        /// </summary>
        /// <param name="doc">Документ Kompas 3D</param>
        /// <param name="keyValuePairs">Словарь замен: ключ - имя переменной, значение - новое значение</param>
        private void ReplaceParts(IKompasDocument3D doc, ObjectAssemblyKompas node)
        {
            if (doc == null || node == null)
                return;

            IPart7 part7 = doc.TopPart;
            foreach (var item in collection)
            {

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

            // TODO: Дополняем новый словарь дополнительными ключами и значениями из документа
            if (node.Designation.Contains("Smm"))
            {
                string Thick = "";
                IPart7 part7 = doc3D.TopPart;
                ISheetMetalContainer sheetMetalContainer = part7 as ISheetMetalContainer;
                ISheetMetalBodies sheetMetalBodies = sheetMetalContainer.SheetMetalBodies;
                int bodies = sheetMetalBodies.Count;
                ISheetMetalBody sheetMetalBody = sheetMetalBodies.SheetMetalBody[0];
                IModelObject modelObject = (IModelObject)part7;
                IFeature7 feature7 = modelObject.Owner;
                IVariable7 variable7 = feature7.Variable[false, true, 0];
                Thick = variable7.Value.ToString(CultureInfo.CreateSpecificCulture("en-GB"));
                extendedDictionary.Add("S", Thick);
            }
            // Например:
            // extendedDictionary["ключ1"] = "значение1";
            // extendedDictionary["ключ2"] = "значение2";

            // Обрабатываем текущий узел с расширенным словарем, здесь и происходит магия замены
            ProcessNode(node, extendedDictionary);
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

                // Обрабатываем PdfFilePath (NewDesignation + .pdf)
                string pdfFileName = baseFileName + ".pdf";

                if (!string.IsNullOrEmpty(ExportPath))
                {
                    node.PdfFilePath = Path.Combine(ExportPath, pdfFileName);
                }
                else
                {
                    node.PdfFilePath = pdfFileName;
                }

                // Обрабатываем PdfFilePath (NewDesignation + .pdf)
                string cdwFileName = baseFileName + ".cdw";

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
                IKompasDocument kompasDocument = document.Open(node.DrawingReferences[Convert.ToInt32(numberDraw)], true, true);
                if (kompasDocument == null)
                {
                    //IKompasDocument kompasDocument = application.ActiveDocument;
                    MessageBox.Show($"У детали {kompasDocument.Name} отсутствует привязанный чертеж", "Уведомление", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                IKompasDocument2D kompasDocument2D = (IKompasDocument2D)kompasDocument;
                IViewsAndLayersManager viewsAndLayersManager = kompasDocument2D.ViewsAndLayersManager;
                IViews views = viewsAndLayersManager.Views;

                IKompasDocument2D1 kompasDocument2D1 = (IKompasDocument2D1)kompasDocument2D;

                List<IView> hideViews = new List<IView>();
                for (int i = 0; i < views.Count; i++)
                {
                    IView view = views.View[i];
                    IAssociationView pAssociationView = view as IAssociationView;
                    if (pAssociationView != null)
                    {
                        pAssociationView.SourceFileName = node.NewFullName;
                        pAssociationView.Update();
                    }
                    if (view != null && view.Visible == false)
                    {
                        view.Visible = true;
                        view.Update();
                        hideViews.Add(view);
                    }
                }
                kompasDocument2D1.RebuildDocument();
                for (int i = 0; i < hideViews.Count; i++)
                {
                    hideViews[i].Visible = false;
                    hideViews[i].Update();
                }
                kompasDocument2D1.RebuildDocument();
                kompasDocument.SaveAs(node.PdfFilePath);
                kompasDocument.SaveAs(node.NewDrawingName);
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
                // Проверяем, что путь к файлу существует
                //if (!File.Exists(node.FullName))
                //{
                //    MessageBox.Show($"Файл не найден: {node.FullName}\nУзел: {node.Name}",
                //        "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}

                // Открываем документ в Kompas
                if (documents != null)
                {
                    IKompasDocument3D doc;
                    if (isLeaf)
                    {
                        // Логика для листочков (деталей)
                        doc = (IKompasDocument3D)documents.Open(node.FullName, true, true);
                        GetDrawingReferences(node, doc);
                        RenameNode(node, dictionaryofreplacements, doc);
                        ReplaceVariables(doc, dictionaryvariables, node);
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
                        RenameNode(node, dictionaryofreplacements, doc);
                        ReplaceVariables(doc, dictionaryvariables, node);
                        ReplaceParts(doc, dictionaryvariables, node);
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
    }
}
