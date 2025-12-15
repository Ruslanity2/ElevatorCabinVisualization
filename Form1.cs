using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ElevatorCabinVisualization
{
    public partial class Form1 : Form
    {
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

        // Panel для контролов
        private Panel controlPanel;

        // NumericUpDown для параметров
        private NumericUpDown numHeight;
        private NumericUpDown numWidth;
        private NumericUpDown numDepth;
        private NumericUpDown numFramePosition;
        private NumericUpDown numDoorHeight;
        private NumericUpDown numDoorWidth;

        // Panel для параметров
        private Panel parametersPanel;

        public Form1()
        {
            InitializeComponent();
            this.Text = "Кабина лифта";
            this.Size = new Size(1000, 700);
            this.BackColor = Color.FromArgb(40, 50, 70);
            this.DoubleBuffered = true;

            InitializeCabinPoints();
            InitializeControls();
            InitializeParametersPanel();

            this.Paint += Form1_Paint;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(984, 711);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form1";
            this.ResumeLayout(false);

        }

        private void InitializeControls()
        {
            // Панель для контролов
            controlPanel = new Panel();
            controlPanel.Location = new Point(750, 20);
            controlPanel.Size = new Size(210, 300);
            controlPanel.BackColor = Color.FromArgb(200, 50, 60, 80);
            controlPanel.BorderStyle = BorderStyle.FixedSingle;

            // Заголовок
            Label lblTitle = new Label();
            lblTitle.Text = "Управление";
            lblTitle.Font = new Font("Arial", 10, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(10, 10);
            lblTitle.Size = new Size(200, 20);
            controlPanel.Controls.Add(lblTitle);

            int yPos = 40;
            int spacing = 40;

            // Потолок
            chkCeiling = CreateToggleCheckBox("Потолок", yPos, ceilingColor);
            chkCeiling.Checked = showCeiling;
            chkCeiling.CheckedChanged += (s, e) => { showCeiling = chkCeiling.Checked; this.Invalidate(); };
            controlPanel.Controls.Add(chkCeiling);
            controlPanel.Controls.Add(CreateSettingsButton(yPos));
            yPos += spacing;

            // Пол
            chkFloor = CreateToggleCheckBox("Пол", yPos, floorColor);
            chkFloor.Checked = showFloor;
            chkFloor.CheckedChanged += (s, e) => { showFloor = chkFloor.Checked; this.Invalidate(); };
            controlPanel.Controls.Add(chkFloor);
            controlPanel.Controls.Add(CreateSettingsButton(yPos));
            yPos += spacing;

            // Левая стена
            chkLeftWall = CreateToggleCheckBox("Левая стена", yPos, leftWallColor);
            chkLeftWall.Checked = showLeftWall;
            chkLeftWall.CheckedChanged += (s, e) => { showLeftWall = chkLeftWall.Checked; this.Invalidate(); };
            controlPanel.Controls.Add(chkLeftWall);
            controlPanel.Controls.Add(CreateSettingsButton(yPos));
            yPos += spacing;

            // Правая стена
            chkRightWall = CreateToggleCheckBox("Правая стена", yPos, rightWallColor);
            chkRightWall.Checked = showRightWall;
            chkRightWall.CheckedChanged += (s, e) => { showRightWall = chkRightWall.Checked; this.Invalidate(); };
            controlPanel.Controls.Add(chkRightWall);
            controlPanel.Controls.Add(CreateSettingsButton(yPos));
            yPos += spacing;

            // Передняя стена
            chkFrontWall = CreateToggleCheckBox("Задняя стена", yPos, frontWallColor);
            chkFrontWall.Checked = showFrontWall;
            chkFrontWall.CheckedChanged += (s, e) => { showFrontWall = chkFrontWall.Checked; this.Invalidate(); };
            controlPanel.Controls.Add(chkFrontWall);
            controlPanel.Controls.Add(CreateSettingsButton(yPos));
            yPos += spacing;

            // Задняя стена
            chkBackWall = CreateToggleCheckBox("Передняя стена", yPos, backWallColor);
            chkBackWall.Checked = showBackWall;
            chkBackWall.CheckedChanged += (s, e) => { showBackWall = chkBackWall.Checked; this.Invalidate(); };
            controlPanel.Controls.Add(chkBackWall);
            controlPanel.Controls.Add(CreateSettingsButton(yPos));

            this.Controls.Add(controlPanel);
        }

        private void InitializeParametersPanel()
        {
            // Панель для параметров слева
            parametersPanel = new Panel();
            parametersPanel.Location = new Point(20, 20);
            parametersPanel.Size = new Size(250, 350);
            parametersPanel.BackColor = Color.FromArgb(200, 50, 60, 80);
            parametersPanel.BorderStyle = BorderStyle.FixedSingle;

            // Заголовок
            Label lblTitle = new Label();
            lblTitle.Text = "Параметры кабины";
            lblTitle.Font = new Font("Arial", 10, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(10, 10);
            lblTitle.Size = new Size(230, 20);
            parametersPanel.Controls.Add(lblTitle);

            int yPos = 45;
            int spacing = 50;

            // Высота
            parametersPanel.Controls.Add(CreateLabel("Высота:", yPos));
            numHeight = CreateNumericUpDown(yPos + 20, 250, 100, 500);
            numHeight.ValueChanged += (s, e) => { InitializeCabinPoints(); this.Invalidate(); };
            parametersPanel.Controls.Add(numHeight);
            yPos += spacing;

            // Ширина
            parametersPanel.Controls.Add(CreateLabel("Ширина:", yPos));
            numWidth = CreateNumericUpDown(yPos + 20, 200, 100, 400);
            numWidth.ValueChanged += (s, e) => { InitializeCabinPoints(); this.Invalidate(); };
            parametersPanel.Controls.Add(numWidth);
            yPos += spacing;

            // Глубина
            parametersPanel.Controls.Add(CreateLabel("Глубина:", yPos));
            numDepth = CreateNumericUpDown(yPos + 20, 200, 100, 400);
            numDepth.ValueChanged += (s, e) => { InitializeCabinPoints(); this.Invalidate(); };
            parametersPanel.Controls.Add(numDepth);
            yPos += spacing;

            // Положение каркаса
            parametersPanel.Controls.Add(CreateLabel("Положение каркаса:", yPos));
            numFramePosition = CreateNumericUpDown(yPos + 20, 0, -100, 100);
            numFramePosition.ValueChanged += (s, e) => { InitializeCabinPoints(); this.Invalidate(); };
            parametersPanel.Controls.Add(numFramePosition);
            yPos += spacing;

            // Высота проема
            parametersPanel.Controls.Add(CreateLabel("Высота проема:", yPos));
            numDoorHeight = CreateNumericUpDown(yPos + 20, 200, 50, 300);
            numDoorHeight.ValueChanged += (s, e) => { this.Invalidate(); };
            parametersPanel.Controls.Add(numDoorHeight);
            yPos += spacing;

            // Ширина проема
            parametersPanel.Controls.Add(CreateLabel("Ширина проема:", yPos));
            numDoorWidth = CreateNumericUpDown(yPos + 20, 100, 50, 200);
            numDoorWidth.ValueChanged += (s, e) => { this.Invalidate(); };
            parametersPanel.Controls.Add(numDoorWidth);

            this.Controls.Add(parametersPanel);
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

        private CheckBox CreateToggleCheckBox(string text, int yPos, Color indicatorColor)
        {
            CheckBox chk = new CheckBox();
            chk.Text = text;
            chk.Font = new Font("Arial", 9, FontStyle.Regular);
            chk.ForeColor = Color.White;
            chk.Location = new Point(10, yPos);
            chk.Size = new Size(150, 30);
            chk.Appearance = Appearance.Button;
            chk.FlatStyle = FlatStyle.Flat;
            chk.FlatAppearance.CheckedBackColor = Color.FromArgb(100, 200, 100);
            chk.FlatAppearance.BorderColor = Color.White;
            chk.TextAlign = ContentAlignment.MiddleCenter;

            return chk;
        }

        private Button CreateSettingsButton(int yPos)
        {
            Button btn = new Button();
            btn.Location = new Point(165, yPos);
            btn.Size = new Size(30, 30);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = Color.White;
            btn.BackColor = Color.FromArgb(70, 80, 100);
            btn.ForeColor = Color.White;
            btn.Font = new Font("Arial", 12, FontStyle.Bold);
            btn.Text = "⚙";
            btn.TextAlign = ContentAlignment.MiddleCenter;
            btn.Cursor = Cursors.Hand;

            // Пока просто заглушка, функционал добавим позже
            btn.Click += (s, e) =>
            {
                // TODO: Добавить функционал настроек
            };

            return btn;
        }

        private void InitializeCabinPoints()
        {
            // Размеры кабины (в пикселях для визуализации)
            double width = (double)(numWidth?.Value ?? 200);   // Ширина - слева направо
            double depth = (double)(numDepth?.Value ?? 150);   // Глубина - спереди назад
            double height = (double)(numHeight?.Value ?? 250); // Высота

            // Центр кабины на форме (сдвинут вправо для панели параметров)
            int centerX = 350;
            int centerY = 400;

            // Углы поворота
            double verticalRotation = 10.0 * Math.PI / 180.0;   // 10 градусов вокруг вертикальной оси
            double horizontalTilt = 10.0 * Math.PI / 180.0;     // 10 градусов наклон по горизонтали

            // Изометрические углы с поворотом
            double baseAngle = Math.PI / 6; // 30 градусов (базовая изометрия)

            // Угол для глубины (ось Y идет назад-влево) с наклоном
            double depthAngle = baseAngle + verticalRotation;
            // Угол для ширины (ось X идет вправо-назад) с наклоном
            double widthAngle = -baseAngle + verticalRotation + horizontalTilt;

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
            double doorWidthReal = (double)(numDoorWidth?.Value ?? 100);   // Ширина двери в единицах
            double doorHeight = (double)(numDoorHeight?.Value ?? 200);     // Высота двери
            double doorMargin = 50;      // Отступ от левого края

            // Используем те же углы, что и для основной кабины
            double baseAngle = Math.PI / 6; // 30 градусов
            double verticalRotation = 10.0 * Math.PI / 180.0;
            double horizontalTilt = 10.0 * Math.PI / 180.0;
            double widthAngle = -baseAngle + verticalRotation + horizontalTilt;

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
                doorBottomLeft.X + (int)(doorWidthReal * widthX),
                doorBottomLeft.Y + (int)(doorWidthReal * widthY)
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
