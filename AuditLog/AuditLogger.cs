using System.Text.Json;

namespace AuditLog;

public sealed class AuditLogger
{
    private readonly object _sync = new();
    private readonly string _dir;

    public AuditLogger(string? dir = null)
    {
        _dir = dir ?? Path.Combine(AppContext.BaseDirectory, "app_log");
        Directory.CreateDirectory(_dir);
    }

    private string FilePath() => Path.Combine(_dir, $"audit-{DateTime.Now:yyyyMMdd}.jsonl");

    public void Write(string actor, string action, string? details = null, string? entity = null)
    {
        var entry = new Entry
        {
            Ts = DateTime.Now,
            Actor = actor,
            Action = action,
            Details = details,
            Entity = entity,
            Machine = Environment.MachineName
        };

        var json = JsonSerializer.Serialize(entry);
        lock (_sync) File.AppendAllText(FilePath(), json + Environment.NewLine);
    }

    private record Entry
    {
        public DateTime Ts { get; init; }
        public string Actor { get; init; } = "";
        public string Action { get; init; } = "";
        public string? Entity { get; init; }
        public string? Details { get; init; }
        public string Machine { get; init; } = "";
    }
}

