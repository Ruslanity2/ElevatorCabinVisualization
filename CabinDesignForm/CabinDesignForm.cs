using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ElevatorCabinVisualization;

namespace CabinDesignTool
{
    public partial class CabinDesignForm : Form
    {
        private double currentHeight = 2000;
        private double currentWidth = 600;
        private Options options;
        private Dictionary<string, NumericUpDown> dimensionControls = new Dictionary<string, NumericUpDown>();
        private Dictionary<string, ComboBox> optionControls = new Dictionary<string, ComboBox>();
        private Dictionary<string, TextBox> markControls = new Dictionary<string, TextBox>();
        private string pathXml;
        private string pathImage;
        private string groupName;
        private string pathModel;
        private Dictionary<string, decimal> mainFormValues;
        private Dictionary<string, string> mainFormMarkValues;

        public CabinDesignForm() : this(null, null, null, null, null, null)
        {
        }

        public CabinDesignForm(string pathXml, string pathImage) : this(pathXml, pathImage, null, null, null, null)
        {
        }

        public CabinDesignForm(string pathXml, string pathImage, string groupName, string pathModel) : this(pathXml, pathImage, groupName, pathModel, null, null)
        {
        }

        public CabinDesignForm(string pathXml, string pathImage, string groupName, string pathModel, Dictionary<string, decimal> mainFormValues) : this(pathXml, pathImage, groupName, pathModel, mainFormValues, null)
        {
        }

        public CabinDesignForm(string pathXml, string pathImage, string groupName, string pathModel, Dictionary<string, decimal> mainFormValues, Dictionary<string, string> mainFormMarkValues)
        {
            InitializeComponent();
            this.pathXml = pathXml;
            this.pathImage = pathImage;
            this.groupName = groupName;
            this.pathModel = pathModel;
            this.mainFormValues = mainFormValues;
            this.mainFormMarkValues = mainFormMarkValues;
            LoadOptions();
            CreateDynamicMarkControls();
            CreateDynamicControls();
            CreateDynamicOptionControls();
            InitializeValues();
            LoadImage();
        }

        private void LoadOptions()
        {
            string optionsPath;

            // Если передан путь к XML, используем его
            if (!string.IsNullOrEmpty(pathXml) && System.IO.File.Exists(pathXml))
            {
                optionsPath = pathXml;
            }
            else
            {
                // Иначе используем путь по умолчанию
                optionsPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Application.ExecutablePath),
                    "Options.xml"
                );
            }

            if (System.IO.File.Exists(optionsPath))
            {
                options = Options.Load(optionsPath);
            }
            else
            {
                options = new Options();
            }
        }

        private decimal? GetValueFromMainForm(Dimension dimension)
        {
            if (mainFormValues == null)
                return null;

            // Пытаемся найти значение по имени dimension
            if (mainFormValues.ContainsKey(dimension.Name))
            {
                return mainFormValues[dimension.Name];
            }

            // Пытаемся найти значение по ID dimension
            if (!string.IsNullOrEmpty(dimension.Id) && mainFormValues.ContainsKey(dimension.Id))
            {
                return mainFormValues[dimension.Id];
            }

            // Не найдено
            return null;
        }

        private string GetMarkValueFromMainForm(Mark mark)
        {
            if (mainFormMarkValues == null)
                return null;

            // Пытаемся найти значение по имени mark
            if (mainFormMarkValues.ContainsKey(mark.Name))
            {
                return mainFormMarkValues[mark.Name];
            }

            // Пытаемся найти значение по ID mark
            if (!string.IsNullOrEmpty(mark.Id) && mainFormMarkValues.ContainsKey(mark.Id))
            {
                return mainFormMarkValues[mark.Id];
            }

            // Не найдено
            return null;
        }

        private void CreateDynamicMarkControls()
        {
            // Очищаем существующие контролы в GroupBox "Маски"
            groupBoxMasks.Controls.Clear();

            if (options == null || options.Marks == null || options.Marks.Count == 0)
            {
                return;
            }

            int yPosition = 25;
            int labelX = 10;
            int textBoxX = 140;
            int rowHeight = 30;

            foreach (var mark in options.Marks)
            {
                // Создаем Label
                Label label = new Label
                {
                    Text = mark.Name,
                    Location = new Point(labelX, yPosition),
                    AutoSize = true
                };

                // Создаем TextBox
                TextBox textBox = new TextBox
                {
                    Name = "textBox" + mark.Id,
                    Location = new Point(textBoxX, yPosition - 2),
                    Size = new Size(100, 20)
                };

                // Проверяем, есть ли значение из MainForm
                string valueFromMainForm = GetMarkValueFromMainForm(mark);

                if (!string.IsNullOrEmpty(valueFromMainForm))
                {
                    // Устанавливаем значение из MainForm
                    textBox.Text = valueFromMainForm;
                    // Блокируем контрол
                    textBox.Enabled = false;
                    textBox.BackColor = Color.LightGray;
                }

                // Добавляем контролы в GroupBox
                groupBoxMasks.Controls.Add(label);
                groupBoxMasks.Controls.Add(textBox);

                // Сохраняем ссылку на TextBox
                markControls[mark.Id] = textBox;

                yPosition += rowHeight;
            }
        }

        private void CreateDynamicControls()
        {
            // Очищаем существующие контролы в GroupBox "Размеры"
            groupBoxSizes.Controls.Clear();

            if (options == null || options.Dimensions == null || options.Dimensions.Count == 0)
            {
                return;
            }

            int yPosition = 25;
            int labelX = 10;
            int numericX = 140;
            int rowHeight = 30;

            foreach (var dimension in options.Dimensions)
            {
                // Создаем Label
                Label label = new Label
                {
                    Text = dimension.Name,
                    Location = new Point(labelX, yPosition),
                    AutoSize = true
                };


                // Создаем NumericUpDown
                NumericUpDown numeric = new NumericUpDown
                {
                    Name = "numeric" + dimension.Id,
                    Location = new Point(numericX, yPosition - 2),
                    Size = new Size(100, 20),
                    Minimum = (decimal)dimension.Min,
                    Maximum = (decimal)dimension.Max,
                    Increment = (decimal)dimension.Inc,
                    Value = (decimal)dimension.Default
                };

                // Проверяем, есть ли значение из MainForm
                decimal? valueFromMainForm = GetValueFromMainForm(dimension);

                if (valueFromMainForm.HasValue)
                {
                    // Устанавливаем значение из MainForm
                    numeric.Value = valueFromMainForm.Value;
                    // Блокируем контрол
                    numeric.Enabled = false;
                    numeric.BackColor = Color.LightGray;
                }
                else
                {
                    // Подписываемся на событие изменения значения только для незаблокированных контролов
                    numeric.ValueChanged += (sender, e) => OnDimensionValueChanged(dimension.Id, (double)numeric.Value);
                }

                // Добавляем контролы в GroupBox
                groupBoxSizes.Controls.Add(label);
                groupBoxSizes.Controls.Add(numeric);

                // Сохраняем ссылку на NumericUpDown
                dimensionControls[dimension.Id] = numeric;

                yPosition += rowHeight;
            }
        }

        private void CreateDynamicOptionControls()
        {
            // Очищаем существующие контролы в GroupBox "Опции"
            groupBoxOptions.Controls.Clear();

            if (options == null || options.OptionGroups == null || options.OptionGroups.Count == 0)
            {
                return;
            }

            int yPosition = 20;
            int labelX = 10;
            int comboX = 140;
            int rowHeight = 30;

            foreach (var optionGroup in options.OptionGroups)
            {
                // Создаем Label
                Label label = new Label
                {
                    Text = optionGroup.Name,
                    Location = new Point(labelX, yPosition),
                    AutoSize = true
                };

                // Создаем ComboBox
                ComboBox combo = new ComboBox
                {
                    Name = "combo" + optionGroup.Id,
                    Location = new Point(comboX, yPosition - 2),
                    Size = new Size(100, 21),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                // Заполняем ComboBox опциями
                int defaultIndex = -1;
                for (int i = 0; i < optionGroup.Options.Count; i++)
                {
                    combo.Items.Add(optionGroup.Options[i].Name);
                    if (optionGroup.Options[i].Default)
                    {
                        defaultIndex = i;
                    }
                }

                // Устанавливаем значение по умолчанию
                if (defaultIndex >= 0)
                {
                    combo.SelectedIndex = defaultIndex;
                }
                else if (combo.Items.Count > 0)
                {
                    combo.SelectedIndex = 0;
                }

                // Подписываемся на событие изменения значения
                combo.SelectedIndexChanged += (sender, e) => OnOptionValueChanged(optionGroup.Id, combo.SelectedIndex);

                // Добавляем контролы в GroupBox
                groupBoxOptions.Controls.Add(label);
                groupBoxOptions.Controls.Add(combo);

                // Сохраняем ссылку на ComboBox
                optionControls[optionGroup.Id] = combo;

                yPosition += rowHeight;
            }
        }

        private void OnOptionValueChanged(string optionGroupId, int selectedIndex)
        {
            UpdateVisualization();
        }

        private void OnDimensionValueChanged(string dimensionId, double value)
        {
            // Обновляем соответствующее значение
            if (dimensionId == "Высота")
            {
                currentHeight = value;
            }
            else if (dimensionId == "Ширина")
            {
                currentWidth = value;
            }

            UpdateVisualization();
        }

        private void InitializeValues()
        {
            // Инициализация выполняется в CreateDynamicOptionControls
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            try
            {
                // Создаем папку Reports, если её нет
                string reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                if (!Directory.Exists(reportsFolder))
                {
                    Directory.CreateDirectory(reportsFolder);
                }

                // Формируем имя файла из значений масок
                string fileName = "Report";
                if (markControls.Count > 0)
                {
                    List<string> maskValues = new List<string>();
                    foreach (var kvp in markControls)
                    {
                        string value = kvp.Value.Text?.Trim();
                        if (!string.IsNullOrEmpty(value))
                        {
                            // Заменяем недопустимые символы для имени файла
                            value = string.Join("_", value.Split(Path.GetInvalidFileNameChars()));
                            maskValues.Add(value);
                        }
                    }

                    if (maskValues.Count > 0)
                    {
                        fileName = string.Join("_", maskValues);
                    }
                }

                // Формируем путь к файлу Report.xml
                string reportPath = Path.Combine(reportsFolder, fileName + ".xml");

                // Загружаем существующий отчет или создаем новый
                Report report;
                if (File.Exists(reportPath))
                {
                    report = Report.Load(reportPath);
                }
                else
                {
                    report = new Report();
                }

                // Создаем новую часть отчета
                ReportPart part = new ReportPart
                {
                    Group = groupName ?? "Unknown",
                    PathModel = pathModel ?? ""
                };

                // Добавляем все маркеры из формы
                foreach (var kvp in markControls)
                {
                    // Находим соответствующий Mark в Options
                    string markValue = kvp.Key;
                    if (options != null && options.Marks != null)
                    {
                        var optionMark = options.Marks.Find(m => m.Id == kvp.Key);
                        if (optionMark != null && !string.IsNullOrEmpty(optionMark.MarkType))
                        {
                            markValue = optionMark.MarkType;
                        }
                    }

                    ReportMark mark = new ReportMark
                    {
                        Name = kvp.Key,
                        Mark = markValue,
                        Value = kvp.Value.Text ?? ""
                    };
                    part.Marks.Add(mark);
                }

                // Добавляем все размеры из формы
                foreach (var kvp in dimensionControls)
                {
                    // Находим соответствующий Dimension в Options
                    string dimensionValue = kvp.Key;
                    if (options != null && options.Dimensions != null)
                    {
                        var optionDimension = options.Dimensions.Find(d => d.Id == kvp.Key);
                        if (optionDimension != null && !string.IsNullOrEmpty(optionDimension.DimensionCode))
                        {
                            dimensionValue = optionDimension.DimensionCode;
                        }
                    }

                    ReportDimension dimension = new ReportDimension
                    {
                        Name = kvp.Key,
                        Dimension = dimensionValue,
                        Value = kvp.Value.Value.ToString()
                    };
                    part.Dimensions.Add(dimension);
                }

                // Добавляем все группы опций из формы
                foreach (var kvp in optionControls)
                {
                    if (kvp.Value.SelectedIndex >= 0)
                    {
                        // Находим соответствующую OptionGroup в Options
                        OptionGroup optionGroup = null;
                        if (options != null && options.OptionGroups != null)
                        {
                            optionGroup = options.OptionGroups.Find(og => og.Id == kvp.Key);
                        }

                        if (optionGroup != null)
                        {
                            // Создаем ReportOptionGroup
                            ReportOptionGroup reportOptionGroup = new ReportOptionGroup
                            {
                                Name = optionGroup.Name,
                                Id = optionGroup.Id
                            };

                            // Находим выбранную опцию
                            string selectedOptionName = kvp.Value.SelectedItem?.ToString();
                            Option selectedOption = optionGroup.Options.Find(o => o.Name == selectedOptionName);

                            if (selectedOption != null)
                            {
                                // Создаем ReportOption с размерами
                                ReportOption reportOption = new ReportOption
                                {
                                    Name = selectedOption.Name,
                                    Id = selectedOption.Id,
                                    Dimensions = new List<ReportDimension>()
                                };

                                // Конвертируем Dimension из CabinDesignTool в ReportDimension для Report
                                foreach (var sourceDimension in selectedOption.Dimensions)
                                {
                                    reportOption.Dimensions.Add(new ReportDimension
                                    {
                                        Name = sourceDimension.Name,
                                        Dimension = sourceDimension.DimensionCode,
                                        Value = sourceDimension.Value.ToString()
                                    });
                                }

                                reportOptionGroup.Options.Add(reportOption);
                            }

                            part.OptionGroups.Add(reportOptionGroup);
                        }
                    }
                }

                // Удаляем старую часть с той же группой, если есть
                ReportPart existingPart = report.GetPartByGroup(part.Group);
                if (existingPart != null)
                {
                    report.Parts.Remove(existingPart);
                }

                // Добавляем новую часть
                report.Parts.Add(part);

                // Сохраняем отчет
                report.Save(reportPath);

                //MessageBox.Show($"Отчет успешно сохранен:\n{reportPath}", "Передать", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Закрываем форму после успешного сохранения
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения отчета: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateVisualization()
        {
            // Создаем изображение с параметрами кабины
            Bitmap bitmap = new Bitmap(pictureBoxVisualization.Width, pictureBoxVisualization.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Фон
                g.Clear(Color.FromArgb(240, 240, 240));

                // Вычисляем размеры для отрисовки с учетом пропорций
                int padding = 20;
                int availableWidth = pictureBoxVisualization.Width - 2 * padding;
                int availableHeight = pictureBoxVisualization.Height - 2 * padding;

                double aspectRatio = currentWidth / currentHeight;
                int drawHeight = availableHeight;
                int drawWidth = (int)(drawHeight * aspectRatio);

                if (drawWidth > availableWidth)
                {
                    drawWidth = availableWidth;
                    drawHeight = (int)(drawWidth / aspectRatio);
                }

                int startX = (pictureBoxVisualization.Width - drawWidth) / 2;
                int startY = (pictureBoxVisualization.Height - drawHeight) / 2;

                // Тень
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                {
                    g.FillRectangle(shadowBrush, startX + 5, startY + 5, drawWidth, drawHeight);
                }

                // Основной корпус кабины
                Color cabinetColor = GetMaterialColor();
                using (SolidBrush bodyBrush = new SolidBrush(cabinetColor))
                using (Pen borderPen = new Pen(Color.FromArgb(100, 100, 100), 2))
                {
                    g.FillRectangle(bodyBrush, startX, startY, drawWidth, drawHeight);
                    g.DrawRectangle(borderPen, startX, startY, drawWidth, drawHeight);
                }

                // Верхние крепления
                int mountingSize = Math.Max(15, drawWidth / 10);
                int mountingHeight = Math.Max(20, drawHeight / 20);
                using (SolidBrush mountingBrush = new SolidBrush(Color.FromArgb(60, 60, 60)))
                using (Pen mountingPen = new Pen(Color.Black, 1))
                {
                    // Левое крепление
                    Rectangle leftMount = new Rectangle(startX + mountingSize, startY - mountingHeight, mountingSize, mountingHeight + 10);
                    g.FillRectangle(mountingBrush, leftMount);
                    g.DrawRectangle(mountingPen, leftMount);

                    // Правое крепление
                    Rectangle rightMount = new Rectangle(startX + drawWidth - 2 * mountingSize, startY - mountingHeight, mountingSize, mountingHeight + 10);
                    g.FillRectangle(mountingBrush, rightMount);
                    g.DrawRectangle(mountingPen, rightMount);
                }

                // Нижние элементы
                int bottomElementHeight = Math.Max(15, drawHeight / 15);
                int bottomElementWidth = drawWidth / 4;
                using (SolidBrush bottomBrush = new SolidBrush(Color.FromArgb(80, 80, 80)))
                using (Pen bottomPen = new Pen(Color.Black, 1))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int x = startX + i * (drawWidth / 3) + (drawWidth / 6) - (bottomElementWidth / 2);
                        Rectangle bottomElement = new Rectangle(x, startY + drawHeight - 5, bottomElementWidth, bottomElementHeight);
                        g.FillRectangle(bottomBrush, bottomElement);
                        g.DrawRectangle(bottomPen, bottomElement);
                    }
                }

                // Секции на двери
                using (Pen sectionPen = new Pen(Color.FromArgb(150, 150, 150), 1))
                {
                    int sectionHeight = drawHeight / 4;
                    for (int i = 1; i < 4; i++)
                    {
                        g.DrawLine(sectionPen, startX + 10, startY + i * sectionHeight, startX + drawWidth - 10, startY + i * sectionHeight);
                    }
                }

                // Индикатор стороны открывания
                using (Pen openingSidePen = new Pen(Color.Red, 2))
                {
                    int arrowY = startY + drawHeight / 2;

                    // Получаем ComboBox для стороны открывания
                    ComboBox openingSideCombo = null;
                    if (optionControls.ContainsKey("Сторона открывания"))
                    {
                        openingSideCombo = optionControls["Сторона открывания"];
                    }

                    if (openingSideCombo != null && openingSideCombo.SelectedIndex >= 0)
                    {
                        string selectedOption = openingSideCombo.SelectedItem?.ToString();

                        if (selectedOption == "Левое")
                        {
                            g.DrawLine(openingSidePen, startX + 10, arrowY, startX + 30, arrowY);
                            g.DrawLine(openingSidePen, startX + 10, arrowY, startX + 20, arrowY - 5);
                            g.DrawLine(openingSidePen, startX + 10, arrowY, startX + 20, arrowY + 5);
                        }
                        else if (selectedOption == "Правое")
                        {
                            g.DrawLine(openingSidePen, startX + drawWidth - 10, arrowY, startX + drawWidth - 30, arrowY);
                            g.DrawLine(openingSidePen, startX + drawWidth - 10, arrowY, startX + drawWidth - 20, arrowY - 5);
                            g.DrawLine(openingSidePen, startX + drawWidth - 10, arrowY, startX + drawWidth - 20, arrowY + 5);
                        }
                    }
                }
            }

            // Устанавливаем изображение в PictureBox
            if (pictureBoxVisualization.Image != null)
            {
                pictureBoxVisualization.Image.Dispose();
            }
            pictureBoxVisualization.Image = bitmap;
        }

        private Color GetMaterialColor()
        {
            // Получаем ComboBox для материала
            ComboBox materialCombo = null;
            if (optionControls.ContainsKey("Материал"))
            {
                materialCombo = optionControls["Материал"];
            }

            if (materialCombo != null && materialCombo.SelectedIndex >= 0)
            {
                string selectedMaterial = materialCombo.SelectedItem?.ToString();

                switch (selectedMaterial)
                {
                    case "Aisi":
                        return Color.FromArgb(200, 180, 140);
                    case "Aisi Bronze":
                        return Color.FromArgb(200, 180, 140);
                    case "Цинк":
                        return Color.FromArgb(220, 230, 240);
                    default:
                        return Color.Gray;
                }
            }

            return Color.Gray;
        }

        private void LoadImage()
        {
            // Если передан путь к изображению, загружаем его
            if (!string.IsNullOrEmpty(pathImage) && System.IO.File.Exists(pathImage))
            {
                try
                {
                    // Загружаем изображение из файла
                    Image image = Image.FromFile(pathImage);

                    // Освобождаем предыдущее изображение, если есть
                    if (pictureBoxVisualization.Image != null)
                    {
                        pictureBoxVisualization.Image.Dispose();
                    }

                    // Устанавливаем новое изображение
                    pictureBoxVisualization.Image = image;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
