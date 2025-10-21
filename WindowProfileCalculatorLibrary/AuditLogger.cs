using System;
using System.IO;
using System.Text.Json;

namespace WindowProfileCalculatorLibrary
{
    /// <summary>
    /// Проста система аудиту дій користувачів.
    /// Записує події у файл JSONL (по одному JSON-об’єкту на рядок).
    /// </summary>
    public static class AuditLogger
    {
        private static readonly string AuditDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WinCalcAudit");

        /// <summary>
        /// Записує подію у журнал аудиту.
        /// </summary>
        /// <param name="action">Тип дії (Login, Delete, RoleChanged тощо)</param>
        /// <param name="user">Користувач, який виконав дію</param>
        /// <param name="details">Додаткова інформація</param>
        public static void Write(string action, string user, string details)
        {
            try
            {
                Directory.CreateDirectory(AuditDirectory);
                string filePath = Path.Combine(AuditDirectory,
                    $"audit-{DateTime.Now:yyyyMMdd}.jsonl");

                var entry = new
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Action = action,
                    User = user,
                    Details = details
                };

                string json = JsonSerializer.Serialize(entry);
                File.AppendAllText(filePath, json + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // fallback лог, щоб не втратити подію
                File.AppendAllText("audit_fallback.log",
                    $"[{DateTime.Now}] Failed to write audit: {ex.Message}{Environment.NewLine}");
            }
        }
    }
}
