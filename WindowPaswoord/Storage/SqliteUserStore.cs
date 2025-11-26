using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using WindowPaswoord.Models;
using WinCalc.Security;

namespace WinCalc.Storage
{
    public class SqliteUserStore
    {
        // Отримуємо шлях до файлу бази даних
        private static string DbPath
        {
            get
            {
                // Отримуємо шлях: C:\Users\Ім'я\AppData\Local\WinCalc
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string folder = Path.Combine(appData, "WinCalc");

                // ⚠️ Обов'язково створюємо папку, якщо її немає!
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                return Path.Combine(folder, "window_calc.db");
            }
        }
        // Створюємо рядок підключення з використанням спільного кешу
        private static string ConnString => $"Data Source={DbPath};Cache=Shared";
        // Метод для створення нового підключення
        private static SqliteConnection Create() => new SqliteConnection(ConnString);
        // Метод для створення таблиці користувачів, якщо її немає
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
        // Метод для створення таблиці користувачів, якщо її немає
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

        // Метод для оновлення інформації про користувача
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

        // Метод для перевірки, чи є хоч один користувач у базі даних
        public async Task<bool> AnyAsync()
        {
            using var con = Create();
            await con.OpenAsync();
            // Перевіряємо, чи існує таблиця взагалі, перед тим як робити SELECT
            // (хоча CreateTables має запускатися раніше, це додатковий захист)
            const string checkTable = "SELECT name FROM sqlite_master WHERE type='table' AND name='Users';";
            using var checkCmd = new SqliteCommand(checkTable, con);
            var tableName = await checkCmd.ExecuteScalarAsync();

            if (tableName == null) return false;

            using var cmd = new SqliteCommand("SELECT EXISTS(SELECT 1 FROM Users LIMIT 1);", con);
            var res = (long)await cmd.ExecuteScalarAsync();
            return res == 1;
        }

        // Метод для отримання всіх користувачів
        public async Task<List<User>> GetAllAsync()
        {
            var users = new List<User>();
            using var con = Create();
            await con.OpenAsync();

            const string sql = @"SELECT Id, Login, Password, Role FROM Users";
            using var cmd = new SqliteCommand(sql, con);
            using var rd = await cmd.ExecuteReaderAsync();

            while (await rd.ReadAsync())
            {
                users.Add(new User
                {
                    Id = rd.GetInt32(0),
                    Username = rd.GetString(1),
                    PasswordHash = rd.GetString(2),
                    Role = rd.GetString(3)
                });
            }

            return users;
        }

        // Метод для оновлення пароля користувача
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

        // Метод для видалення користувача за ідентифікатором
        public async Task DeleteAsync(int id)
        {
            using var con = Create();
            await con.OpenAsync();

            const string sql = @"DELETE FROM Users WHERE Id=@id;";
            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}