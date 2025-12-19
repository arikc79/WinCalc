using System;
using System.IO;

namespace WinCalc.Common
{
    public static class DbConfig
    {
        public static string DbPath
        {
            get
            {
                // Рабочая папка приложения (где расположен exe)
                string appFolder = AppContext.BaseDirectory ?? Environment.CurrentDirectory;

                // Гарантируем, что папка существует
                if (!Directory.Exists(appFolder))
                {
                    Directory.CreateDirectory(appFolder);
                }

                return Path.Combine(appFolder, "window_calc.db");
            }
        }

        public static string ConnectionString => $"Data Source={DbPath}";
    }
}
