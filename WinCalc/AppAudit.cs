using AuditLog;

namespace WinCalc;

internal static class AppAudit
{
    private static readonly AuditLogger _log = new(); // пишет в ./logs/audit-YYYYMMDD.jsonl

    public static void LoginOk(string user) =>
        _log.Write(user, "login_ok");

    public static void MaterialsImport(string user, int added, int updated) =>
        _log.Write(user, "materials_import", $"added={added}; updated={updated}", "Material");

    public static void MaterialDelete(string user, int id, string name) =>
        _log.Write(user, "material_delete", $"id={id}; name={name}", "Material");

    public static void RoleChanged(string actor, string targetUser, string fromRole, string toRole) =>
        _log.Write(actor ?? "anonymous", "role_changed",
                   $"target={targetUser}; from={fromRole}; to={toRole}", "User");

    public static void RoleChanged(string actor, string targetUser, string toRole) =>
        _log.Write(actor ?? "anonymous", "role_changed",
                   $"target={targetUser}; to={toRole}", "User");
}

