using WindowPaswoord.Models;
using WinCalc.Security;
using WinCalc.Storage;

namespace WinCalc.Services
{
    public class AuthService
    {
        private readonly SqliteUserStore _store = new();

        public async Task<(bool ok, string? error)> RegisterAsync(string username, string password, string role = Roles.Manager)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return (false, "Введіть логін і пароль");

            var existing = await _store.GetByUsernameAsync(username);
            if (existing != null)
                return (false, "Користувач вже існує");

            var user = new User
            {
                Username = username.Trim(),
                PasswordHash = PasswordHasher.Hash(password),
                Role = role
            };

            await _store.CreateAsync(user);
            return (true, null);
        }

        public async Task<(bool ok, User? user, string? error)> LoginAsync(string username, string password)
        {
            var user = await _store.GetByUsernameAsync(username);
            if (user == null)
                return (false, null, "Невірний логін або пароль");

            bool valid = PasswordHasher.Verify(password, user.PasswordHash);
            if (!valid)
                return (false, null, "Невірний логін або пароль");

            return (true, user, null);
        }

        public async Task EnsureAdminSeedAsync()
        {
            if (!await _store.AnyAsync())
            {
                var admin = new User
                {
                    Username = "admin",
                    PasswordHash = PasswordHasher.Hash("admin"),
                    Role = Roles.Admin
                };
                await _store.CreateAsync(admin);
            }
        }
    }
}
