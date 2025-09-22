using System.IO;
using Microsoft.Data.Sqlite;
using WindowPaswoord.Models;
using WinCalc.Security; 



namespace WinCalc.Storage
{
    public class SqliteUserStore
    {
        private static string ConnString =>
            $"Data Source={Path.Combine(System.AppContext.BaseDirectory, "window_calc.db")};Cache=Shared";

        private static SqliteConnection Create() => new SqliteConnection(ConnString);

        public async Task<User?> GetByUsernameAsync(string username)
        {
            using var con = Create();
            await con.OpenAsync();

            const string sql = @"SELECT Id, Login, Password, Role
                                 FROM Users
                                 WHERE Login = @login
                                 LIMIT 1;";
            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@login", username);

            using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync()) return null;

            return new User
            {
                Id = rd.GetInt32(0),
                Username = rd.GetString(1),
                PasswordHash = rd.GetString(2),
                Role = rd.GetString(3)
            };
        }

        public async Task<User> CreateAsync(User user)
        {
            using var con = Create();
            await con.OpenAsync();

            const string sql = @"INSERT INTO Users (Login, Password, Role)
                                 VALUES (@login, @pass, @role);
                                 SELECT last_insert_rowid();";
            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@login", user.Username);
            cmd.Parameters.AddWithValue("@pass", user.PasswordHash);
            cmd.Parameters.AddWithValue("@role", user.Role);

            var id = (long)await cmd.ExecuteScalarAsync();
            user.Id = (int)id;
            return user;
        }

        public async Task UpdateAsync(User user)
        {
            using var con = Create();
            await con.OpenAsync();

            const string sql = @"UPDATE Users
                                 SET Login=@login, Password=@pass, Role=@role
                                 WHERE Id=@id;";
            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@login", user.Username);
            cmd.Parameters.AddWithValue("@pass", user.PasswordHash);
            cmd.Parameters.AddWithValue("@role", user.Role);
            cmd.Parameters.AddWithValue("@id", user.Id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> AnyAsync()
        {
            using var con = Create();
            await con.OpenAsync();
            using var cmd = new SqliteCommand("SELECT EXISTS(SELECT 1 FROM Users LIMIT 1);", con);
            var res = (long)await cmd.ExecuteScalarAsync();
            return res == 1;
        }


        // Метод: повертає список усіх користувачів
        public async Task<List<User>> GetAllAsync()
        {
            var users = new List<User>();

            using var con = Create();
            await con.OpenAsync();

            // Вибираємо всі записи з таблиці Users
            const string sql = @"SELECT Id, Login, Password, Role FROM Users";
            using var cmd = new SqliteCommand(sql, con);

            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                users.Add(new User
                {
                    Id = rd.GetInt32(0),              // Id
                    Username = rd.GetString(1),       // Login
                    PasswordHash = rd.GetString(2),   // Password (хеш)
                    Role = rd.GetString(3)            // Role
                });
            }

            return users;
        }
        public async Task UpdatePasswordAsync(int userId, string newPlainPassword)
        {
            if (string.IsNullOrWhiteSpace(newPlainPassword))
                throw new ArgumentException("New password is empty.", nameof(newPlainPassword));

            var hash = PasswordHasher.Hash(newPlainPassword);

            using var con = Create();

            await con.OpenAsync();

            const string sql = @"UPDATE Users
                         SET Password = @pass
                         WHERE Id = @id;";
            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@pass", hash);
            cmd.Parameters.AddWithValue("@id", userId);

            await cmd.ExecuteNonQueryAsync();
        }




    }
}
