namespace WinCalc.Security
{
    public static class Roles
    {
        public const string Admin = "admin";
        public const string Manager = "manager";

        public static bool EqualsRole(string? role, string target) =>
            string.Equals(role, target, System.StringComparison.OrdinalIgnoreCase);
    }
}
