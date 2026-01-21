using System;
using System.Windows.Forms;

namespace ElevatorCabinVisualization
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (LicenseValidator.IsValid(out string errorMessage))
            {
                Application.Run(new MainForm());
            }
            else
            {
                MessageBox.Show(errorMessage, "Ошибка лицензии", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
