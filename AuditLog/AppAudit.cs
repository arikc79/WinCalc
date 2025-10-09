using System;
using System.IO;
using System.Text;

namespace AuditLog
{
    
    // Усі події записуються у файл Logs/audit.log із часовою позначкою.
 
    public static class AppAudit
    {
        private static readonly string LogDir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        private static readonly string LogFile =
            Path.Combine(LogDir, "audit.log");

        static AppAudit()
        {
            try
            {
                if (!Directory.Exists(LogDir))
                    Directory.CreateDirectory(LogDir);

                if (!File.Exists(LogFile))
                    File.AppendAllText(LogFile, $"=== Створено журнал {DateTime.Now} ==={Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка створення папки логів: {ex.Message}");
            }
        }

        // ====== Основний метод запису ======
        private static void Write(string action, string username, string details)
        {
            try
            {
                var entry = new StringBuilder()
                    .Append($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | ")
                    .Append($"{username,-15} | ")
                    .Append($"{action,-20} | ")
                    .AppendLine(details);

                File.AppendAllText(LogFile, entry.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка запису в журнал: {ex.Message}");
            }
        }

        // ====== Події користувачів ======
        public static void LoginOk(string username)
        {
            Write("Login OK", username, "Успішний вхід у систему.");
        }

        public static void LoginFail(string username)
        {
            Write("Login FAIL", username, "Невдала спроба входу.");
        }

        // ====== Події матеріалів ======
        public static void MaterialsImport(string username, int count, int errors)
        {
            Write("Import CSV", username, $"Імпортовано {count} матеріалів, помилок: {errors}");
        }

        public static void MaterialDelete(string username, int id, string name)
        {
            Write("Delete Material", username, $"Видалено матеріал ID={id}, Назва='{name}'");
        }

        public static void MaterialEdit(string username, int id, string field, string oldValue, string newValue)
        {
            Write("Edit Material", username, $"ID={id}, поле '{field}' змінено: '{oldValue}' → '{newValue}'");
        }

        // ====== Службові ======
        public static void SystemError(string username, string message)
        {
            Write("System Error", username, message);
        }
    }
}
