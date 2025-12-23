using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            #region Получаю коллекции переменных для подстановки в модель
            // Формируем словарь замен из размеров (Dimensions) и меток (Marks)
            Dictionary<string, string> dictionaryofreplacements = new Dictionary<string, string>();

            // Добавляем метки из коллекции Marks
            if (part.Marks != null && part.Marks.Count > 0)
            {
                foreach (var mark in part.Marks)
                {
                    if (!string.IsNullOrEmpty(mark.Mark) && !string.IsNullOrEmpty(mark.Value))
                    {
                        dictionaryofreplacements[mark.Mark] = mark.Value;
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
                        dictionaryofreplacements[dimension.Dimension] = dimension.Value;
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
                                        dictionaryofreplacements[dimension.Name] = dimension.Value;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            #endregion

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
                if (!WaitForDocumentReady())
                {
                    MessageBox.Show("Не удалось дождаться загрузки документа Kompas-3D",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            catch (COMException comEx)
            {
                MessageBox.Show($"Ошибка COM при работе с Kompas: {comEx.Message}\nПопробуйте перезапустить приложение.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            IPart7 part7 = document3D.TopPart;
            IModelObject modelObject = (IModelObject)part7;
            IFeature7 feature7 = modelObject.Owner;

            foreach (var item in dictionaryofreplacements)
            {
                IVariable7 variableDim = feature7.Variable[false, true, item.Key]; //Передал dimension из xml в модель
                if (variableDim != null)
                {
                    variableDim.Value = Convert.ToDouble(item.Value); //Передал в модель значение поля NumericUpDown с формы в модель
                }
            }

            // Перестраиваем документ и ждем завершения
            bool flag = ksDocument3D.RebuildDocument();

            if (root != null)
            {
                root = null;
            }
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
                        RenameTreeNodes(root, dictionaryofreplacements);
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
        /// Рекурсивно переименовывает узлы дерева, заменяя текст в Designation и Name согласно словарю замен
        /// </summary>
        /// <param name="root">Корневой узел дерева</param>
        /// <param name="keyValuePairs">Словарь замен: ключ - текст для поиска, значение - текст для замены</param>
        public void RenameTreeNodes(ObjectAssemblyKompas root, Dictionary<string, string> keyValuePairs)
        {
            if (root == null || keyValuePairs == null || keyValuePairs.Count == 0)
                return;

            // Обрабатываем текущий узел
            ProcessNode(root, keyValuePairs);

            // Рекурсивно обрабатываем всех детей
            if (root.Children != null && root.Children.Count > 0)
            {
                foreach (var child in root.Children)
                {
                    RenameTreeNodes(child, keyValuePairs);
                }
            }
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
                        doc = (IKompasDocument3D)documents.Open(node.FullName, true, false);
                        kompas.ksMessage($"Отлично загружено {node.Designation}");
                        doc.Close(DocumentCloseOptions.kdDoNotSaveChanges);
                    }
                    else
                    {
                        // Логика для сборок (родителей)
                        doc = (IKompasDocument3D)documents.Open(node.FullName, true, false);
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
