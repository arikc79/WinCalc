
using System;
using WindowProfileCalculatorLibrary;

namespace WinCalc
{
    /// <summary>
    /// Відповідає за реєстрацію основних подій у аудит-журналі.
    /// Обгортка над AuditLogger, щоб зручно викликати з будь-якого місця.
    /// </summary>
    public static class AppAudit
    {
        // Основний метод для запису події
        private static void Log(string action, string user, string details)
        {
            try
            {
                AuditLogger.Write(action, user, details);
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("audit_errors.log",
                    $"[{DateTime.Now}] Failed to write audit: {ex.Message}{Environment.NewLine}");
            }
        }

        // ----------------------------------------------------
        // Конкретні типи подій
        // ----------------------------------------------------
        public static void LoginOk(string username)
            => Log("LoginSuccess", username, "User logged in successfully");

        public static void LoginFail(string username)
            => Log("LoginFail", username, "Invalid credentials");

        public static void MaterialsImport(string username, int count, int total)
            => Log("MaterialsImport", username, $"Imported {count}/{total} materials");

        public static void MaterialDelete(string username, int id, string name)
            => Log("MaterialDelete", username, $"Deleted material ID={id}, Name={name}");

        public static void RoleChanged(string admin, string targetUser, string newRole)
            => Log("RoleChanged", admin, $"User={targetUser}, NewRole={newRole}");
    }
}
