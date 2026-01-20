using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace ElevatorCabinVisualization
{
    public partial class MainForm : Form
    {
        // Данные отделки из XML
        private Finishing finishingData;

        // Данные параметров из XML
        private Params paramsData;

        // Флаги видимости частей кабины
        private bool showCeiling = true;
        private bool showFloor = true;
        private bool showLeftWall = true;
        private bool showRightWall = true;
        private bool showFrontWall = true;
        private bool showBackWall = true;

        // Точки для изометрической проекции
        private Point[] ceilingPoints;
        private Point[] floorPoints;
        private Point[] leftWallPoints;
        private Point[] rightWallPoints;
        private Point[] frontWallPoints;
        private Point[] backWallPoints;

        // Цвета для частей
        private Color ceilingColor = Color.FromArgb(180, 200, 220, 240);
        private Color floorColor = Color.FromArgb(180, 150, 150, 150);
        private Color leftWallColor = Color.FromArgb(180, 180, 200, 220);
        private Color rightWallColor = Color.FromArgb(180, 160, 180, 200);
        private Color frontWallColor = Color.FromArgb(180, 220, 230, 240);
        private Color backWallColor = Color.FromArgb(180, 140, 160, 180);

        // CheckBox для управления видимостью
        private CheckBox chkCeiling;
        private CheckBox chkFloor;
        private CheckBox chkLeftWall;
        private CheckBox chkRightWall;
        private CheckBox chkFrontWall;
        private CheckBox chkBackWall;

        // ComboBox для каждого элемента
        private ComboBox cmbCeiling;
        private ComboBox cmbFloor;
        private ComboBox cmbLeftWall;
        private ComboBox cmbRightWall;
        private ComboBox cmbFrontWall;
        private ComboBox cmbBackWall;

        // GroupBox для контролов
        private GroupBox controlPanel;
        private Button btnToggleControl;
        private bool isControlPanelCollapsed = false;
        private int controlPanelExpandedHeight = 300;

        // NumericUpDown для параметров (заполняются динамически из Params.xml)
        // Оставлены для обратной совместимости с существующим кодом
        private NumericUpDown numHeight;
        private NumericUpDown numWidth;
        private NumericUpDown numDepth;
        private NumericUpDown numFramePosition;
        private NumericUpDown numDoorHeight;
        private NumericUpDown numDoorWidth;
        private NumericUpDown numDoorMargin;

        // Словарь для связи имен параметров с NumericUpDown контролами
        private Dictionary<string, NumericUpDown> parameterControls = new Dictionary<string, NumericUpDown>();

        // Словарь для связи имен маркеров с TextBox контролами
        private Dictionary<string, TextBox> markControls = new Dictionary<string, TextBox>();

        // GroupBox для маркеров
        private GroupBox marksPanel;
        private Button btnToggleMarks;
        private bool isMarksPanelCollapsed = false;
        private int marksPanelExpandedHeight = 100;

        // GroupBox для параметров
        private GroupBox parametersPanel;
        private Button btnToggleParameters;
        private bool isParametersPanelCollapsed = false;
        private int parametersPanelExpandedHeight = 450;

        // Кнопка закрытия
        private Button btnClose;

        // Кнопка выгрузки
        private Button btnExport;

        // StatusStrip и его элементы
        private StatusStrip statusStrip;
        private ToolStripLabel lblExportPath;
        private ToolStripTextBox txtExportPath;
        private ToolStripButton btnBrowseFolder;
        private ToolStripLabel lblStatus;

        public MainForm()
        {
            InitializeComponent();
            this.Text = "Кабина лифта";
            this.Size = new Size(730, 730);
            this.BackColor = Color.FromArgb(40, 50, 70);
            this.DoubleBuffered = true;

            LoadFinishingData();
            LoadParamsData();
            InitializeStatusStrip();
            InitializeMarksPanel();
            InitializeParametersPanel();
            InitializeControls();
            InitializeCabinPoints(); // Должен быть после InitializeParametersPanel, чтобы numHeight/numWidth/numDepth были инициализированы
            InitializeExportButton();
            InitializeCloseButton();

            this.Paint += Form1_Paint;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(800, 711);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ResumeLayout(false);

        }

        private void LoadFinishingData()
        {
            string xmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "Finishing.xml");
            try
            {
                finishingData = Finishing.Load(xmlPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки Finishing.xml: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                finishingData = new Finishing();
            }
        }

        private void LoadParamsData()
        {
            string xmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "Params.xml");
            try
            {
                paramsData = Params.Load(xmlPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки Params.xml: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                paramsData = new Params();
            }
        }

        private void InitializeControls()
        {
            // GroupBox для контролов (размещается под панелью параметров)
            controlPanel = new GroupBox();
            controlPanel.Text = "Управление";
            int controlPanelY = parametersPanel.Location.Y + parametersPanel.Height + 10;
            controlPanel.Location = new Point(20, controlPanelY);
            controlPanel.Width = 250;
            controlPanel.BackColor = Color.FromArgb(200, 50, 60, 80);
            controlPanel.ForeColor = Color.White;
            controlPanel.Font = new Font("Arial", 10, FontStyle.Bold);

            // Кнопка сворачивания/разворачивания
            btnToggleControl = new Button();
            btnToggleControl.Location = new Point(controlPanel.Width - 30, 10);
            btnToggleControl.Size = new Size(20, 20);
            btnToggleControl.FlatStyle = FlatStyle.Flat;
            btnToggleControl.FlatAppearance.BorderSize = 0;
            btnToggleControl.BackColor = Color.Transparent;
            btnToggleControl.ForeColor = Color.White;
            btnToggleControl.Font = new Font("Arial", 10, FontStyle.Bold);
            btnToggleControl.Text = "▲";
            btnToggleControl.Cursor = Cursors.Hand;
            btnToggleControl.Click += ToggleControlPanel;
            controlPanel.Controls.Add(btnToggleControl);

            int yPos = 35;
            int spacing = 32;

            // Потолок
            chkCeiling = CreateToggleCheckBox("", yPos, ceilingColor);
            chkCeiling.Checked = showCeiling;
            chkCeiling.CheckedChanged += (s, e) => { showCeiling = chkCeiling.Checked; UpdateEyeIcon(chkCeiling); this.Invalidate(); };
            controlPanel.Controls.Add(chkCeiling);
            UpdateEyeIcon(chkCeiling);
            cmbCeiling = CreateComboBox(yPos, "Потолок");
            controlPanel.Controls.Add(cmbCeiling);
            controlPanel.Controls.Add(CreateSettingsButton(yPos, "Потолок", cmbCeiling));
            yPos += spacing;

            // Пол
            chkFloor = CreateToggleCheckBox("", yPos, floorColor);
            chkFloor.Checked = showFloor;
            chkFloor.CheckedChanged += (s, e) => { showFloor = chkFloor.Checked; UpdateEyeIcon(chkFloor); this.Invalidate(); };
            controlPanel.Controls.Add(chkFloor);
            UpdateEyeIcon(chkFloor);
            cmbFloor = CreateComboBox(yPos, "Пол");
            controlPanel.Controls.Add(cmbFloor);
            controlPanel.Controls.Add(CreateSettingsButton(yPos, "Пол", cmbFloor));
            yPos += spacing;

            // Левая стена
            chkLeftWall = CreateToggleCheckBox("", yPos, leftWallColor);
            chkLeftWall.Checked = showLeftWall;
            chkLeftWall.CheckedChanged += (s, e) => { showLeftWall = chkLeftWall.Checked; UpdateEyeIcon(chkLeftWall); this.Invalidate(); };
            controlPanel.Controls.Add(chkLeftWall);
            UpdateEyeIcon(chkLeftWall);
            cmbLeftWall = CreateComboBox(yPos, "Левая стенка");
            controlPanel.Controls.Add(cmbLeftWall);
            controlPanel.Controls.Add(CreateSettingsButton(yPos, "Левая стенка", cmbLeftWall));
            yPos += spacing;

            // Правая стена
            chkRightWall = CreateToggleCheckBox("", yPos, rightWallColor);
            chkRightWall.Checked = showRightWall;
            chkRightWall.CheckedChanged += (s, e) => { showRightWall = chkRightWall.Checked; UpdateEyeIcon(chkRightWall); this.Invalidate(); };
            controlPanel.Controls.Add(chkRightWall);
            UpdateEyeIcon(chkRightWall);
            cmbRightWall = CreateComboBox(yPos, "Правая стенка");
            controlPanel.Controls.Add(cmbRightWall);
            controlPanel.Controls.Add(CreateSettingsButton(yPos, "Правая стенка", cmbRightWall));
            yPos += spacing;

            // Передняя стена (визуально ближняя к зрителю, в коде называется frontWall)
            chkFrontWall = CreateToggleCheckBox("", yPos, frontWallColor);
            chkFrontWall.Checked = showFrontWall;
            chkFrontWall.CheckedChanged += (s, e) => { showFrontWall = chkFrontWall.Checked; UpdateEyeIcon(chkFrontWall); this.Invalidate(); };
            controlPanel.Controls.Add(chkFrontWall);
            UpdateEyeIcon(chkFrontWall);
            cmbFrontWall = CreateComboBox(yPos, "Задняя стенка");
            controlPanel.Controls.Add(cmbFrontWall);
            controlPanel.Controls.Add(CreateSettingsButton(yPos, "Задняя стенка", cmbFrontWall));
            yPos += spacing;

            // Задняя стена (визуально дальняя с дверью, в коде backWall)
            chkBackWall = CreateToggleCheckBox("", yPos, backWallColor);
            chkBackWall.Checked = showBackWall;
            chkBackWall.CheckedChanged += (s, e) => { showBackWall = chkBackWall.Checked; UpdateEyeIcon(chkBackWall); this.Invalidate(); };
            controlPanel.Controls.Add(chkBackWall);
            UpdateEyeIcon(chkBackWall);
            cmbBackWall = CreateComboBox(yPos, "Передняя стенка");
            controlPanel.Controls.Add(cmbBackWall);
            controlPanel.Controls.Add(CreateSettingsButton(yPos, "Передняя стенка", cmbBackWall));
            yPos += spacing;

            // Навесное оборудование (без управления видимостью на изометрии)
            CheckBox chkEquipment = CreateToggleCheckBox("", yPos, Color.FromArgb(180, 200, 180, 160));
            chkEquipment.Checked = false;
            chkEquipment.Enabled = false; // Заблокирован, так как пока нечего скрывать/показывать
            controlPanel.Controls.Add(chkEquipment);
            UpdateEyeIcon(chkEquipment);
            ComboBox cmbEquipment = CreateComboBox(yPos, "Навесное оборудование");
            controlPanel.Controls.Add(cmbEquipment);
            controlPanel.Controls.Add(CreateSettingsButton(yPos, "Навесное оборудование", cmbEquipment));
            yPos += spacing;

            // Подгоняем высоту GroupBox под количество контролов
            // yPos уже указывает на следующую позицию после последнего контрола
            // Добавляем отступ снизу
            controlPanelExpandedHeight = yPos + 10;
            controlPanel.Height = controlPanelExpandedHeight;

            this.Controls.Add(controlPanel);
        }

        private void InitializeMarksPanel()
        {
            // GroupBox для маркеров слева (самый верхний)
            marksPanel = new GroupBox();
            marksPanel.Text = "Маски";
            marksPanel.Location = new Point(20, 20);
            marksPanel.Width = 250;
            marksPanel.BackColor = Color.FromArgb(200, 50, 60, 80);
            marksPanel.ForeColor = Color.White;
            marksPanel.Font = new Font("Arial", 10, FontStyle.Bold);

            // Кнопка сворачивания/разворачивания
            btnToggleMarks = new Button();
            btnToggleMarks.Location = new Point(marksPanel.Width - 30, 10);
            btnToggleMarks.Size = new Size(20, 20);
            btnToggleMarks.FlatStyle = FlatStyle.Flat;
            btnToggleMarks.FlatAppearance.BorderSize = 0;
            btnToggleMarks.BackColor = Color.Transparent;
            btnToggleMarks.ForeColor = Color.White;
            btnToggleMarks.Font = new Font("Arial", 10, FontStyle.Bold);
            btnToggleMarks.Text = "▲";
            btnToggleMarks.Cursor = Cursors.Hand;
            btnToggleMarks.Click += ToggleMarksPanel;
            marksPanel.Controls.Add(btnToggleMarks);

            // Очищаем словарь контролов
            markControls.Clear();

            // Создаем контролы динамически из paramsData.Marks
            if (paramsData != null && paramsData.Marks != null && paramsData.Marks.Count > 0)
            {
                int yPos = 35;
                int spacing = 28;
                int labelX = 10;
                int textBoxX = 140;

                foreach (var mark in paramsData.Marks)
                {
                    // Создаем Label для маркера
                    Label label = CreateInlineLabel(mark.Name, labelX, yPos);
                    marksPanel.Controls.Add(label);

                    // Создаем TextBox для маркера
                    TextBox textBox = new TextBox();
                    textBox.Location = new Point(textBoxX, yPos);
                    textBox.Width = 100;
                    textBox.Font = new Font("Arial", 10, FontStyle.Regular);
                    textBox.BackColor = Color.FromArgb(70, 80, 100);
                    textBox.ForeColor = Color.White;
                    textBox.BorderStyle = BorderStyle.FixedSingle;

                    marksPanel.Controls.Add(textBox);

                    // Сохраняем ссылку на контрол в словаре
                    markControls[mark.Name] = textBox;

                    yPos += spacing;
                }

                // Подгоняем высоту GroupBox под количество маркеров
                marksPanelExpandedHeight = yPos + 10;
                marksPanel.Height = marksPanelExpandedHeight;
            }
            else
            {
                // Если маркеров нет, устанавливаем минимальную высоту
                marksPanelExpandedHeight = 60;
                marksPanel.Height = marksPanelExpandedHeight;
            }

            this.Controls.Add(marksPanel);
        }

        private void ToggleMarksPanel(object sender, EventArgs e)
        {
            isMarksPanelCollapsed = !isMarksPanelCollapsed;

            if (isMarksPanelCollapsed)
            {
                // Сворачиваем: скрываем все контролы кроме кнопки сворачивания
                foreach (Control control in marksPanel.Controls)
                {
                    if (control != btnToggleMarks)
                    {
                        control.Visible = false;
                    }
                }
                marksPanel.Height = 35; // Только заголовок
                btnToggleMarks.Text = "▼";
            }
            else
            {
                // Разворачиваем: показываем все контролы
                foreach (Control control in marksPanel.Controls)
                {
                    control.Visible = true;
                }
                marksPanel.Height = marksPanelExpandedHeight;
                btnToggleMarks.Text = "▲";
            }

            // Пересчитываем положение панели параметров
            int parametersPanelY = marksPanel.Location.Y + marksPanel.Height + 10;
            parametersPanel.Location = new Point(parametersPanel.Location.X, parametersPanelY);

            // Пересчитываем положение контрольной панели
            int controlPanelY = parametersPanel.Location.Y + parametersPanel.Height + 10;
            controlPanel.Location = new Point(controlPanel.Location.X, controlPanelY);

            // Перерисовываем форму
            this.Invalidate();
        }

        private void InitializeParametersPanel()
        {
            // GroupBox для параметров слева (под панелью маркеров)
            parametersPanel = new GroupBox();
            parametersPanel.Text = "Параметры кабины";
            int parametersPanelY = marksPanel.Location.Y + marksPanel.Height + 10;
            parametersPanel.Location = new Point(20, parametersPanelY);
            parametersPanel.Width = 250;
            // Высота будет установлена динамически после создания контролов
            parametersPanel.BackColor = Color.FromArgb(200, 50, 60, 80);
            parametersPanel.ForeColor = Color.White;
            parametersPanel.Font = new Font("Arial", 10, FontStyle.Bold);

            // Кнопка сворачивания/разворачивания
            btnToggleParameters = new Button();
            btnToggleParameters.Location = new Point(parametersPanel.Width - 30, 10);
            btnToggleParameters.Size = new Size(20, 20);
            btnToggleParameters.FlatStyle = FlatStyle.Flat;
            btnToggleParameters.FlatAppearance.BorderSize = 0;
            btnToggleParameters.BackColor = Color.Transparent;
            btnToggleParameters.ForeColor = Color.White;
            btnToggleParameters.Font = new Font("Arial", 10, FontStyle.Bold);
            btnToggleParameters.Text = "▲";
            btnToggleParameters.Cursor = Cursors.Hand;
            btnToggleParameters.Click += ToggleParametersPanel;
            parametersPanel.Controls.Add(btnToggleParameters);

            // Очищаем словарь контролов
            parameterControls.Clear();

            // Создаем контролы динамически из paramsData
            if (paramsData != null && paramsData.Parameters != null && paramsData.Parameters.Count > 0)
            {
                int yPos = 35;
                int spacing = 28;
                int labelX = 10;
                int numericX = 140;

                foreach (var param in paramsData.Parameters)
                {
                    // Создаем Label для параметра (с AutoSize)
                    Label label = CreateInlineLabel(param.Name, labelX, yPos);
                    parametersPanel.Controls.Add(label);

                    // Создаем NumericUpDown для параметра (ширина 100)
                    NumericUpDown numeric = CreateInlineNumericUpDown(
                        numericX,
                        yPos,
                        (decimal)param.Default,
                        (decimal)param.Min,
                        (decimal)param.Max
                    );
                    numeric.Increment = (decimal)param.Inc;

                    // Определяем, нужно ли перерисовывать кабину при изменении этого параметра
                    // Параметры, влияющие на геометрию кабины
                    if (param.Name == "Высота" || param.Name == "Ширина" || param.Name == "Глубина" || param.Name == "Положение каркаса")
                    {
                        numeric.ValueChanged += (s, e) => { InitializeCabinPoints(); this.Invalidate(); };
                    }
                    else
                    {
                        // Остальные параметры только перерисовывают
                        numeric.ValueChanged += (s, e) => { this.Invalidate(); };
                    }

                    parametersPanel.Controls.Add(numeric);

                    // Сохраняем ссылку на контрол в словаре
                    parameterControls[param.Name] = numeric;

                    // Также сохраняем ссылки в старые переменные для обратной совместимости
                    AssignLegacyControl(param.Name, numeric);

                    yPos += spacing;
                }

                // Подгоняем высоту GroupBox под количество параметров
                // yPos уже указывает на следующую позицию после последнего контрола
                // Добавляем отступ снизу
                parametersPanelExpandedHeight = yPos + 10;
                parametersPanel.Height = parametersPanelExpandedHeight;
            }
            else
            {
                // Если параметров нет, устанавливаем минимальную высоту
                parametersPanelExpandedHeight = 60;
                parametersPanel.Height = parametersPanelExpandedHeight;
            }

            this.Controls.Add(parametersPanel);
        }

        private void ToggleParametersPanel(object sender, EventArgs e)
        {
            isParametersPanelCollapsed = !isParametersPanelCollapsed;

            if (isParametersPanelCollapsed)
            {
                // Сворачиваем: скрываем все контролы кроме кнопки сворачивания
                foreach (Control control in parametersPanel.Controls)
                {
                    if (control != btnToggleParameters)
                    {
                        control.Visible = false;
                    }
                }
                parametersPanel.Height = 35; // Только заголовок
                btnToggleParameters.Text = "▼";
            }
            else
            {
                // Разворачиваем: показываем все контролы
                foreach (Control control in parametersPanel.Controls)
                {
                    control.Visible = true;
                }
                parametersPanel.Height = parametersPanelExpandedHeight;
                btnToggleParameters.Text = "▲";
            }

            // Пересчитываем положение контрольной панели
            int controlPanelY = parametersPanel.Location.Y + parametersPanel.Height + 10;
            controlPanel.Location = new Point(controlPanel.Location.X, controlPanelY);

            // Перерисовываем форму
            this.Invalidate();
        }

        private void ToggleControlPanel(object sender, EventArgs e)
        {
            isControlPanelCollapsed = !isControlPanelCollapsed;

            if (isControlPanelCollapsed)
            {
                // Сворачиваем: скрываем все контролы кроме кнопки сворачивания
                foreach (Control control in controlPanel.Controls)
                {
                    if (control != btnToggleControl)
                    {
                        control.Visible = false;
                    }
                }
                controlPanel.Height = 35; // Только заголовок
                btnToggleControl.Text = "▼";
            }
            else
            {
                // Разворачиваем: показываем все контролы
                foreach (Control control in controlPanel.Controls)
                {
                    control.Visible = true;
                }
                controlPanel.Height = controlPanelExpandedHeight;
                btnToggleControl.Text = "▲";
            }

            // Перерисовываем форму
            this.Invalidate();
        }

        private void AssignLegacyControl(string paramName, NumericUpDown control)
        {
            // Присваиваем контролы к старым полям для обратной совместимости
            switch (paramName)
            {
                case "Высота":
                    numHeight = control;
                    break;
                case "Ширина":
                    numWidth = control;
                    break;
                case "Глубина":
                    numDepth = control;
                    break;
                case "Положение каркаса":
                    numFramePosition = control;
                    break;
                case "Высота проема":
                    numDoorHeight = control;
                    break;
                case "Ширина проема":
                    numDoorWidth = control;
                    break;
                case "Заплечик":
                    numDoorMargin = control;
                    break;
            }
        }

        private void InitializeStatusStrip()
        {
            statusStrip = new StatusStrip();
            statusStrip.BackColor = Color.FromArgb(50, 60, 80);
            statusStrip.ForeColor = Color.White;

            // Label "Путь выгрузки"
            lblExportPath = new ToolStripLabel();
            lblExportPath.Text = "Путь выгрузки: ";
            lblExportPath.ForeColor = Color.White;

            // TextBox для пути
            txtExportPath = new ToolStripTextBox();
            txtExportPath.Size = new Size(150, 23);
            txtExportPath.BackColor = Color.FromArgb(70, 80, 100);
            txtExportPath.ForeColor = Color.White;
            txtExportPath.BorderStyle = BorderStyle.FixedSingle;

            // Кнопка выбора папки (используем текстовый символ папки)
            btnBrowseFolder = new ToolStripButton();
            btnBrowseFolder.Font = new Font("Arial", 12, FontStyle.Regular);
            btnBrowseFolder.Text = "📁";
            btnBrowseFolder.ForeColor = Color.White;
            btnBrowseFolder.Click += BtnBrowseFolder_Click;

            // Label "Строка статуса"
            lblStatus = new ToolStripLabel();
            lblStatus.Text = "Строка статуса";
            lblStatus.ForeColor = Color.White;
            lblStatus.AutoSize = true; // Заполняет оставшееся пространство
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;

            // Добавляем элементы в StatusStrip
            statusStrip.Items.Add(lblExportPath);
            statusStrip.Items.Add(txtExportPath);
            statusStrip.Items.Add(btnBrowseFolder);
            statusStrip.Items.Add(lblStatus);

            this.Controls.Add(statusStrip);
        }

        private void BtnBrowseFolder_Click(object sender, EventArgs e)
        {
            // Если в txtExportPath указана существующая папка, открываем её в проводнике
            if (!string.IsNullOrEmpty(txtExportPath.Text) && Directory.Exists(txtExportPath.Text))
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", txtExportPath.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии папки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Если папки нет, открываем диалог выбора папки
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Выберите папку для выгрузки";

                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        txtExportPath.Text = folderDialog.SelectedPath;
                    }
                }
            }
        }

        private void InitializeExportButton()
        {
            btnExport = new Button();
            int exportButtonY = controlPanel.Location.Y + controlPanel.Height + 10;
            btnExport.Location = new Point(20, exportButtonY);
            btnExport.Width = 250;
            btnExport.Height = 35;
            btnExport.Text = "Выполнить выгрузку";
            btnExport.Font = new Font("Arial", 10, FontStyle.Bold);
            btnExport.BackColor = Color.FromArgb(200, 80, 120, 180);
            btnExport.ForeColor = Color.White;
            btnExport.FlatStyle = FlatStyle.Flat;
            btnExport.FlatAppearance.BorderColor = Color.White;
            btnExport.FlatAppearance.BorderSize = 1;
            btnExport.Cursor = Cursors.Hand;
            btnExport.Click += BtnExport_Click;

            this.Controls.Add(btnExport);
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            // Проверяем, что путь указан и существует
            if (string.IsNullOrWhiteSpace(txtExportPath.Text))
            {
                MessageBox.Show("Укажите путь для выгрузки файлов",
                    "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                lblStatus.Text = "Ошибка: не указан путь выгрузки";
                return;
            }

            if (!Directory.Exists(txtExportPath.Text))
            {
                MessageBox.Show($"Указанный путь не существует: {txtExportPath.Text}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Ошибка: путь не существует";
                return;
            }

            lblStatus.Text = "Выполняется выгрузка...";
            Application.DoEvents(); // Обновляем UI

            KompasExporter kompasExporter = new KompasExporter();
            kompasExporter.ExportPath = txtExportPath.Text;
            kompasExporter.ProcessReportParts();

            lblStatus.Text = $"Выгрузка выполнена успешно в: {txtExportPath.Text}";
        }

        private void InitializeCloseButton()
        {
            btnClose = new Button();
            btnClose.Location = new Point(this.ClientSize.Width - 40, 10);
            btnClose.Size = new Size(30, 30);
            btnClose.FlatStyle = FlatStyle.Popup;
            //btnClose.FlatAppearance.BorderColor = Color.White;
            //btnClose.FlatAppearance.BorderSize = 2;
            btnClose.BackColor = Color.FromArgb(180, 200, 50, 50);
            btnClose.ForeColor = Color.White;
            btnClose.Font = new Font("Arial", 14, FontStyle.Bold);
            btnClose.Text = "Х";
            btnClose.TextAlign = ContentAlignment.BottomCenter;
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) => this.Close();

            this.Controls.Add(btnClose);
        }

        private Label CreateLabel(string text, int yPos)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = new Font("Arial", 9, FontStyle.Regular);
            lbl.ForeColor = Color.White;
            lbl.Location = new Point(10, yPos);
            lbl.Size = new Size(230, 20);
            return lbl;
        }

        private Label CreateCompactLabel(string text, int yPos)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = new Font("Arial", 8, FontStyle.Regular);
            lbl.ForeColor = Color.White;
            lbl.Location = new Point(10, yPos);
            lbl.Size = new Size(230, 14);
            lbl.AutoSize = false;
            return lbl;
        }

        private Label CreateInlineLabel(string text, int xPos, int yPos)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = new Font("Arial", 9, FontStyle.Regular);
            lbl.ForeColor = Color.White;
            lbl.Location = new Point(xPos, yPos + 3);
            lbl.AutoSize = true;
            return lbl;
        }

        private NumericUpDown CreateNumericUpDown(int yPos, decimal value, decimal min, decimal max)
        {
            NumericUpDown num = new NumericUpDown();
            num.Location = new Point(10, yPos);
            num.Size = new Size(230, 25);
            num.Font = new Font("Arial", 10, FontStyle.Regular);
            num.Minimum = min;
            num.Maximum = max;
            num.Value = value;
            num.DecimalPlaces = 0;
            num.BackColor = Color.FromArgb(70, 80, 100);
            num.ForeColor = Color.White;
            return num;
        }

        private NumericUpDown CreateCompactNumericUpDown(int yPos, decimal value, decimal min, decimal max)
        {
            NumericUpDown num = new NumericUpDown();
            num.Location = new Point(10, yPos);
            num.Size = new Size(230, 20);
            num.Font = new Font("Arial", 8, FontStyle.Regular);
            num.Minimum = min;
            num.Maximum = max;
            num.Value = value;
            num.DecimalPlaces = 0;
            num.BackColor = Color.FromArgb(70, 80, 100);
            num.ForeColor = Color.White;
            return num;
        }

        private NumericUpDown CreateInlineNumericUpDown(int xPos, int yPos, decimal value, decimal min, decimal max)
        {
            NumericUpDown num = new NumericUpDown();
            num.Location = new Point(xPos, yPos);
            num.Size = new Size(100, 23);
            num.Font = new Font("Arial", 10, FontStyle.Regular);
            num.Minimum = min;
            num.Maximum = max;
            num.Value = value;
            num.DecimalPlaces = 0;
            num.BackColor = Color.FromArgb(70, 80, 100);
            num.ForeColor = Color.White;
            return num;
        }

        private CheckBox CreateToggleCheckBox(string text, int yPos, Color indicatorColor)
        {
            CheckBox chk = new CheckBox();
            chk.Text = text;
            chk.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            chk.ForeColor = Color.White;
            chk.Location = new Point(10, yPos);
            chk.Size = new Size(24, 24);
            chk.Appearance = Appearance.Button;
            chk.FlatStyle = FlatStyle.Flat;
            chk.FlatAppearance.CheckedBackColor = Color.FromArgb(100, 200, 100);
            chk.FlatAppearance.BorderColor = Color.White;
            chk.FlatAppearance.BorderSize = 1;
            chk.TextAlign = ContentAlignment.MiddleCenter;

            return chk;
        }

        private void UpdateEyeIcon(CheckBox chk)
        {
            // Символы: 👁 - открытый глаз, 👁‍🗨 - зачеркнутый (используем простой Unicode)
            // Альтернатива: 👁 и ⃠ или просто разные символы            
            chk.Text = chk.Checked ? "👁" : " ";
        }

        private ComboBox CreateComboBox(int yPos, string groupName)
        {
            ComboBox cmb = new ComboBox();
            cmb.Location = new Point(39, yPos);
            cmb.Size = new Size(170, 35);
            cmb.Font = new Font("Arial", 10, FontStyle.Regular);
            cmb.BackColor = Color.FromArgb(70, 80, 100);
            cmb.ForeColor = Color.White;
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.DropDownStyle = ComboBoxStyle.DropDownList;

            // Ищем группу по имени в данных из XML
            FinishingGroup group = finishingData?.GetGroupByName(groupName);

            if (group != null)
            {
                // Первый элемент - название группы
                cmb.Items.Add(group.Name);

                // Если есть варианты отделки, добавляем их
                if (group.Rows.Count > 0)
                {
                    foreach (var row in group.Rows)
                    {
                        cmb.Items.Add(row.Name);
                    }
                }

                cmb.SelectedIndex = 0;
            }
            else
            {
                // Если группа не найдена, добавляем название группы как заглушку
                cmb.Items.Add(groupName);
                cmb.SelectedIndex = 0;
            }

            return cmb;
        }

        private Button CreateSettingsButton(int yPos, string groupName, ComboBox combo)
        {
            Button btn = new Button();
            btn.Location = new Point(214, yPos);
            btn.Size = new Size(24, 24);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = Color.White;
            btn.BackColor = Color.FromArgb(70, 80, 100);
            btn.ForeColor = Color.White;
            btn.Font = new Font("Arial", 14, FontStyle.Regular);
            btn.Text = "🔧";
            btn.TextAlign = ContentAlignment.BottomCenter;
            btn.Cursor = Cursors.Hand;

            btn.Click += (s, e) =>
            {
                string pathXml = null;
                string pathImage = null;
                string pathModel = null;

                // Получаем выбранный элемент из ComboBox
                if (combo.SelectedIndex > 0) // Первый элемент - это название группы
                {
                    // Получаем группу из данных отделки
                    FinishingGroup group = finishingData?.GetGroupByName(groupName);

                    if (group != null && combo.SelectedIndex - 1 < group.Rows.Count)
                    {
                        // Индекс в списке Rows на 1 меньше, чем SelectedIndex (т.к. первый элемент - название группы)
                        FinishingRow row = group.Rows[combo.SelectedIndex - 1];

                        pathXml = row.PathXml;
                        pathImage = row.PathImage;
                        pathModel = row.PathModel;
                    }
                }

                // Проверяем, указан ли PathXml
                if (string.IsNullOrEmpty(pathXml))
                {
                    MessageBox.Show("Для выбранного элемента не указан путь к файлу конфигурации (PathXml).",
                        "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Собираем значения параметров динамически из Params.xml
                Dictionary<string, decimal> mainFormValues = new Dictionary<string, decimal>();

                if (paramsData != null && paramsData.Parameters != null)
                {
                    foreach (var param in paramsData.Parameters)
                    {
                        // Используем текущее значение из NumericUpDown контролов
                        decimal currentValue = GetParameterValue(param.Name);
                        mainFormValues[param.Name] = currentValue;
                    }
                }

                // Собираем значения маркеров динамически из Params.xml
                Dictionary<string, string> mainFormMarkValues = new Dictionary<string, string>();

                if (paramsData != null && paramsData.Marks != null)
                {
                    foreach (var mark in paramsData.Marks)
                    {
                        // Используем текущее значение из TextBox контролов
                        string currentValue = GetMarkValue(mark.Name);
                        mainFormMarkValues[mark.Name] = currentValue;
                    }
                }

                // Открываем форму с переданными параметрами и маркерами
                CabinDesignTool.CabinDesignForm designForm = new CabinDesignTool.CabinDesignForm(pathXml, pathImage, groupName, pathModel, mainFormValues, mainFormMarkValues);
                designForm.ShowDialog(this);
            };

            return btn;
        }

        private decimal GetParameterValue(string parameterName)
        {
            // Пытаемся найти контрол в словаре по имени параметра
            if (parameterControls.ContainsKey(parameterName))
            {
                NumericUpDown control = parameterControls[parameterName];
                return control?.Value ?? 0;
            }

            // Если контрол не найден в словаре, ищем параметр в paramsData
            Parameter param = paramsData?.GetParameterByName(parameterName);
            if (param != null)
            {
                return (decimal)param.Default;
            }

            // Если ничего не найдено, возвращаем 0
            return 0;
        }

        private string GetMarkValue(string markName)
        {
            // Пытаемся найти контрол в словаре по имени маркера
            if (markControls.ContainsKey(markName))
            {
                TextBox control = markControls[markName];
                return control?.Text ?? string.Empty;
            }

            // Если ничего не найдено, возвращаем пустую строку
            return string.Empty;
        }

        private void InitializeCabinPoints()
        {
            // Реальные размеры кабины в мм
            double widthReal = (double)(numWidth?.Value ?? 1100);   // Ширина - слева направо
            double depthReal = (double)(numDepth?.Value ?? 800);    // Глубина - спереди назад
            double heightReal = (double)(numHeight?.Value ?? 2100); // Высота

            // Коэффициент масштабирования для отображения на форме
            // Доступная область: от панели параметров (270px) до панели управления (750px) = 480px ширина
            // Высота формы около 700px, оставляем запас
            double availableWidth = 480;
            double availableHeight = 500;

            // Вычисляем максимальные проекции кабины в изометрии
            double baseAngle = Math.PI / 6;
            double verticalRotation = 10.0 * Math.PI / 180.0;
            double horizontalTilt = 10.0 * Math.PI / 180.0;
            double depthAngle = baseAngle + verticalRotation;
            double widthAngle = -baseAngle + verticalRotation + horizontalTilt;

            // Проекция габаритов на плоскость экрана
            double projectedWidth = Math.Abs(depthReal * Math.Cos(depthAngle)) + Math.Abs(widthReal * Math.Cos(widthAngle));
            double projectedDepth = Math.Abs(depthReal * Math.Sin(depthAngle)) + Math.Abs(widthReal * Math.Sin(widthAngle));
            double totalProjectedHeight = heightReal + projectedDepth;

            // Коэффициент масштабирования (берем минимальный, чтобы всё влезло)
            double scaleX = availableWidth / projectedWidth;
            double scaleY = availableHeight / totalProjectedHeight;
            double scale = Math.Min(scaleX, scaleY) * 0.9; // 0.9 для небольшого отступа

            // Применяем масштаб к размерам
            double width = widthReal * scale;
            double depth = depthReal * scale;
            double height = heightReal * scale;

            // Центр кабины на форме (сдвинут влево ближе к панели параметров)
            int centerX = 320;
            int centerY = 100 + (int)(availableHeight / 2) + (int)(depth * Math.Sin(depthAngle));

            // Изометрические коэффициенты для осей
            double depthX = Math.Cos(depthAngle);
            double depthY = Math.Sin(depthAngle);
            double widthX = Math.Cos(widthAngle);
            double widthY = Math.Sin(widthAngle);

            // Вычисляем 8 вершин куба (чистая изометрия без перспективы)
            // Нижние точки (пол) - z = 0
            Point p0 = new Point(centerX, centerY); // Передний левый нижний (0,0,0)

            Point p1 = new Point(
                centerX + (int)(depth * depthX),
                centerY + (int)(depth * depthY)
            ); // Задний левый нижний (0,depth,0)

            Point p2 = new Point(
                centerX + (int)(depth * depthX + width * widthX),
                centerY + (int)(depth * depthY + width * widthY)
            ); // Задний правый нижний (width,depth,0)

            Point p3 = new Point(
                centerX + (int)(width * widthX),
                centerY + (int)(width * widthY)
            ); // Передний правый нижний (width,0,0)

            // Верхние точки (потолок) - z = height
            // В изометрии вертикальные линии остаются строго вертикальными
            Point p4 = new Point(p0.X, p0.Y - (int)height); // Передний левый верхний
            Point p5 = new Point(p1.X, p1.Y - (int)height); // Задний левый верхний
            Point p6 = new Point(p2.X, p2.Y - (int)height); // Задний правый верхний
            Point p7 = new Point(p3.X, p3.Y - (int)height); // Передний правый верхний

            // Собираем грани (обход против часовой стрелки, если смотреть снаружи)

            // Пол (нижняя грань, смотрим сверху вниз)
            floorPoints = new Point[] { p0, p3, p2, p1 };

            // Потолок (верхняя грань, смотрим снизу вверх)
            ceilingPoints = new Point[] { p4, p5, p6, p7 };

            // Передняя стена (ближайшая к нам)
            frontWallPoints = new Point[] { p0, p3, p7, p4 };

            // Задняя стена (с дверью, дальняя от нас)
            backWallPoints = new Point[] { p1, p2, p6, p5 };

            // Левая стена
            leftWallPoints = new Point[] { p0, p1, p5, p4 };

            // Правая стена
            rightWallPoints = new Point[] { p3, p2, p6, p7 };
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Рисуем части в правильном порядке (от дальних к ближним)

            // Задняя стена (с дверью) - самая дальняя
            if (showBackWall)
                DrawBackWallWithDoor(g);

            // Потолок
            if (showCeiling)
                DrawPart(g, ceilingPoints, ceilingColor);

            // Левая стена (задняя левая)
            if (showLeftWall)
                DrawPart(g, leftWallPoints, leftWallColor);

            // Правая стена (передняя правая)
            if (showRightWall)
                DrawPart(g, rightWallPoints, rightWallColor);

            // Пол
            if (showFloor)
                DrawPart(g, floorPoints, floorColor);

            // Передняя стена - рисуем последней
            if (showFrontWall)
                DrawPart(g, frontWallPoints, frontWallColor);
        }

        private void DrawPart(Graphics g, Point[] points, Color color)
        {
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillPolygon(brush, points);
            }

            using (Pen pen = new Pen(Color.White, 2))
            {
                g.DrawPolygon(pen, points);
            }
        }

        private void DrawBackWallWithDoor(Graphics g)
        {
            // Рисуем стену
            using (SolidBrush brush = new SolidBrush(backWallColor))
            {
                g.FillPolygon(brush, backWallPoints);
            }

            // Рисуем дверь поверх стены в изометрии
            double doorWidthReal = (double)(numDoorWidth?.Value ?? 800);   // Ширина двери в мм
            double doorHeightReal = (double)(numDoorHeight?.Value ?? 2000); // Высота двери в мм
            double doorMarginReal = (double)(numDoorMargin?.Value ?? 150);  // Отступ от левого края в мм (Заплечик)

            // Вычисляем коэффициент масштабирования (тот же, что для кабины)
            double widthReal = (double)(numWidth?.Value ?? 1100);
            double depthReal = (double)(numDepth?.Value ?? 800);
            double heightReal = (double)(numHeight?.Value ?? 2100);

            double availableWidth = 480;
            double availableHeight = 500;

            double baseAngle = Math.PI / 6;
            double verticalRotation = 10.0 * Math.PI / 180.0;
            double horizontalTilt = 10.0 * Math.PI / 180.0;
            double depthAngle = baseAngle + verticalRotation;
            double widthAngle = -baseAngle + verticalRotation + horizontalTilt;

            double projectedWidth = Math.Abs(depthReal * Math.Cos(depthAngle)) + Math.Abs(widthReal * Math.Cos(widthAngle));
            double projectedDepth = Math.Abs(depthReal * Math.Sin(depthAngle)) + Math.Abs(widthReal * Math.Sin(widthAngle));
            double totalProjectedHeight = heightReal + projectedDepth;

            double scaleX = availableWidth / projectedWidth;
            double scaleY = availableHeight / totalProjectedHeight;
            double scale = Math.Min(scaleX, scaleY) * 0.9;

            // Применяем масштаб к размерам двери
            double doorWidth = doorWidthReal * scale;
            double doorHeight = doorHeightReal * scale;
            double doorMargin = doorMarginReal * scale;

            // Изометрические коэффициенты для ширины двери
            double widthX = Math.Cos(widthAngle);
            double widthY = Math.Sin(widthAngle);

            // Базовая точка двери (левый нижний угол задней стены)
            Point basePoint = backWallPoints[0]; // p1 - Задний левый нижний

            // Рассчитываем точки двери в изометрии
            // Нижняя левая точка двери (смещение от левого края стены)
            Point doorBottomLeft = new Point(
                basePoint.X + (int)(doorMargin * widthX),
                basePoint.Y + (int)(doorMargin * widthY)
            );

            // Нижняя правая точка двери
            Point doorBottomRight = new Point(
                doorBottomLeft.X + (int)(doorWidth * widthX),
                doorBottomLeft.Y + (int)(doorWidth * widthY)
            );

            // Верхние точки двери (строго вертикально вверх)
            Point doorTopRight = new Point(
                doorBottomRight.X,
                doorBottomRight.Y - (int)doorHeight
            );

            Point doorTopLeft = new Point(
                doorBottomLeft.X,
                doorBottomLeft.Y - (int)doorHeight
            );

            Point[] doorPoints = new Point[] { doorBottomLeft, doorBottomRight, doorTopRight, doorTopLeft };

            // Дверь рисуется темнее стены
            using (SolidBrush doorBrush = new SolidBrush(Color.FromArgb(200, 80, 80, 80)))
            {
                g.FillPolygon(doorBrush, doorPoints);
            }

            using (Pen doorPen = new Pen(Color.Silver, 2))
            {
                g.DrawPolygon(doorPen, doorPoints);

                // Вертикальное разделение двери (посередине, строго вертикально)
                int midX = (doorBottomLeft.X + doorBottomRight.X) / 2;
                int midY = (doorBottomLeft.Y + doorBottomRight.Y) / 2;
                int midTopY = midY - (int)doorHeight;

                g.DrawLine(doorPen,
                    midX, midY,
                    midX, midTopY);
            }

            // Обводка стены
            using (Pen pen = new Pen(Color.White, 2))
            {
                g.DrawPolygon(pen, backWallPoints);
            }
        }
    }
}
