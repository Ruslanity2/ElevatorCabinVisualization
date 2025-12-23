using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Kompas6API5;
using KompasAPI7;

namespace ElevatorCabinVisualization
{
    /// <summary>
    /// Сервис для управления жизненным циклом приложения Kompas-3D
    /// </summary>
    public class KompasRestartService
    {
        /// <summary>
        /// Имя процесса Kompas-3D
        /// </summary>
        private const string KompasProcessName = "KOMPAS";

        /// <summary>
        /// Закрывает все запущенные экземпляры Kompas-3D, запускает новый и возвращает готовый к работе объект KompasObject
        /// </summary>
        /// <returns>Готовый к работе объект KompasObject или null в случае ошибки</returns>
        public KompasObject GetKompasInstance()
        {
            // Закрываем все запущенные экземпляры Kompas
            if (!CloseAllKompasInstances())
            {
                return null;
            }

            // Запускаем новый экземпляр Kompas
            if (!StartKompasApplication())
            {
                return null;
            }

            // Ожидаем готовности Kompas к работе
            if (!WaitForKompasReady())
            {
                MessageBox.Show("Не удалось дождаться готовности Kompas-3D к работе",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            // Получаем COM-объект Kompas
            try
            {
                KompasObject kompas = (KompasObject)Marshal.GetActiveObject("KOMPAS.Application.5");
                kompas.Visible = true;
                kompas.ActivateControllerAPI();
                return kompas;
            }
            catch (COMException comEx)
            {
                MessageBox.Show($"Ошибка COM при получении объекта Kompas: {comEx.Message}\nПопробуйте перезапустить приложение.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// Закрывает все запущенные экземпляры Kompas-3D
        /// </summary>
        /// <returns>true если операция прошла успешно, false в случае критической ошибки</returns>
        private bool CloseAllKompasInstances()
        {
            // Получаем все запущенные процессы Kompas
            Process[] kompasProcesses = Process.GetProcessesByName(KompasProcessName);

            // Если есть запущенные экземпляры - закрываем их все
            if (kompasProcesses.Length > 0)
            {
                foreach (Process process in kompasProcesses)
                {
                    try
                    {
                        process.CloseMainWindow();

                        // Ждем 2 секунды на корректное закрытие
                        if (!process.WaitForExit(2000))
                        {
                            // Если не закрылся - принудительно завершаем
                            process.Kill();
                        }

                        process.Dispose();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при закрытии процесса Kompas: {ex.Message}",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Запускает новый экземпляр Kompas-3D
        /// </summary>
        /// <returns>true если запуск успешен, false в случае ошибки</returns>
        private bool StartKompasApplication()
        {
            try
            {
                // Стандартный путь установки Kompas-3D
                string kompasPath = @"C:\Program Files\ASCON\KOMPAS-3D v22\Bin\KOMPAS.exe";

                // Проверяем существование файла
                if (!File.Exists(kompasPath))
                {
                    // Пробуем альтернативные пути
                    kompasPath = @"C:\Program Files (x86)\ASCON\KOMPAS-3D v22\Bin\kompas.exe";

                    if (!File.Exists(kompasPath))
                    {
                        throw new FileNotFoundException("Не удалось найти исполняемый файл Kompas-3D");
                    }
                }

                Process.Start(kompasPath);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске Kompas-3D: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Ожидает готовности Kompas к работе через COM с таймаутом
        /// </summary>
        /// <param name="timeoutMs">Максимальное время ожидания в миллисекундах</param>
        /// <returns>true если Kompas готов, false если превышен таймаут</returns>
        private bool WaitForKompasReady(int timeoutMs = 60000)
        {
            int elapsed = 0;
            int sleepInterval = 500; // Проверяем каждые 500 мс

            while (elapsed < timeoutMs)
            {
                try
                {
                    // Пытаемся подключиться к Kompas через COM
                    KompasObject testKompas = (KompasObject)Marshal.GetActiveObject("KOMPAS.Application.5");
                    if (testKompas != null)
                    {
                        // Проверяем, что приложение действительно готово к работе
                        try
                        {
                            testKompas.ActivateControllerAPI();
                            IApplication application = testKompas.ksGetApplication7();
                            if (application != null)
                            {
                                // Пытаемся получить коллекцию документов
                                IDocuments documents = application.Documents;
                                if (documents != null)
                                {
                                    // Приложение полностью готово к работе
                                    return true;
                                }
                            }
                        }
                        catch
                        {
                            // API еще не готов, продолжаем ждать
                        }
                    }
                }
                catch (COMException)
                {
                    // Kompas еще не готов, продолжаем ждать
                }
                catch (InvalidCastException)
                {
                    // Ошибка приведения типа - продолжаем ждать
                }
                catch (Exception)
                {
                    // Любая другая ошибка - продолжаем ждать
                }

                System.Threading.Thread.Sleep(sleepInterval);
                elapsed += sleepInterval;
            }

            return false;
        }
    }
}
