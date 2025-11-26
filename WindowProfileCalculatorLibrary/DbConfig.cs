using System;
using System.IO;

namespace WindowProfileCalculatorLibrary // Або WindowProfileCalculatorLibrary, залежно від того, де ви його створите
{
    public static class DbConfig
    {
        public static string DbPath
        {
            get
            {
                // Шлях: C:\Users\User\AppData\Local\WinCalc\window_calc.db
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinCalc");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                return Path.Combine(folder, "window_calc.db");
            }
        }

        public static string ConnectionString => $"Data Source={DbPath}";
    }
}