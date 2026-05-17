
using WinCalc.Storage;
using WindowPaswoord.Models;

namespace WinCalc.Security
{
    public static class AppSession
    {
        public static User? CurrentUser { get; private set; }

        public static void SetCurrentUser(User? user)
        {
            CurrentUser = user;
        }

        public static void Clear()
        {
            CurrentUser = null;
        }

        public static bool IsInRole(string role)
            => CurrentUser != null &&
               string.Equals(CurrentUser.Role, role, StringComparison.OrdinalIgnoreCase);
    }
}
