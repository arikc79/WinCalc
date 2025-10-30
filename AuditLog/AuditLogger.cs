using System.Text.Json;

namespace AuditLog;

public sealed class AuditLogger
{
    private readonly object _sync = new();
    private readonly string _dir;

    public AuditLogger(string? dir = null)
    {
        _dir = dir ?? Path.Combine(AppContext.BaseDirectory, "logs");

        if (File.Exists(_dir))
        {
            try { File.Move(_dir, _dir + ".bak", true); } catch {  }
        }

        Directory.CreateDirectory(_dir);
    }

    private string FilePath() =>
        Path.Combine(_dir, $"audit-{DateTime.Now:yyyyMMdd}.jsonl");

    public void Write(string actor, string action, string? details = null, string? entity = null)
    {
        var entry = new
        {
            Ts = DateTime.Now,
            Actor = actor,
            Action = action,
            Details = details,
            Entity = entity,
            Machine = Environment.MachineName
        };

        var json = JsonSerializer.Serialize(entry);
        lock (_sync)
        {
            File.AppendAllText(FilePath(), json + Environment.NewLine);
        }
    }
}


