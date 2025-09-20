using WindowPaswoord.Models;
using WinCalc.Security;
using WinCalc.Storage;


namespace WinCalc.Services
{
    public class AuthService
    {
        private readonly SqliteUserStore _store = new();

        public async Task<(bool ok, string? error)> RegisterAsync(string username, string password, string role = "manager")
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return (false, "Введите логин и пароль");

            var existing = await _store.GetByUsernameAsync(username);
            if (existing != null) return (false, "Пользователь уже существует");

            var user = new User
            {
                Username = username.Trim(),
                PasswordHash = PasswordHasher.Hash(password),
                Role = string.IsNullOrWhiteSpace(role) ? "manager" : role
            };

            await _store.CreateAsync(user);
            return (true, null);
        }

        public async Task<(bool ok, User? user, string? error)> LoginAsync(string username, string password)
        {
            var user = await _store.GetByUsernameAsync(username);
            if (user is null) return (false, null, "Неверный логин или пароль");

            var ok = PasswordHasher.Verify(password, user.PasswordHash);
            if (!ok) return (false, null, "Неверный логин или пароль");

            return (true, user, null);
        }
        // ADD: начальный админ, если пользователей ещё нет
        public async Task EnsureAdminSeedAsync()
        {
            if (!await _store.AnyAsync())
            {
                var admin = new User
                {
                    Username = "admin",
                    PasswordHash = PasswordHasher.Hash("admin"), // смените после первого входа
                    Role = Roles.Admin
                };
                await _store.CreateAsync(admin);
            }
        }

    }
}