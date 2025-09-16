using WinCalc.Models;

namespace WinCalc.Security
{
    public static class AppSession
    {
        public static User? CurrentUser { get; private set; }
        public static bool IsAuthenticated => CurrentUser != null;

        public static void SignIn(User user) => CurrentUser = user;
        public static void SignOut() => CurrentUser = null;

        public static bool IsInRole(string role) =>
            CurrentUser != null && string.Equals(CurrentUser.Role, role, System.StringComparison.OrdinalIgnoreCase);
    }
}

