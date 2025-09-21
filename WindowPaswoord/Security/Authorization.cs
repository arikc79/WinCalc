using System.Data;
using WindowPaswoord.Models;

namespace WinCalc.Security
{
    public static class Authorization
    {
        public static bool CanViewMaterials(User? u) => In(u, Roles.Admin) || In(u, Roles.Manager);
        public static bool CanCalculate(User? u) => In(u, Roles.Admin) || In(u, Roles.Manager);

        public static bool CanManageMaterials(User? u) => In(u, Roles.Admin);

        private static bool In(User? u, string role) =>
            u != null && string.Equals(u.Role, role, System.StringComparison.OrdinalIgnoreCase);
    }
}