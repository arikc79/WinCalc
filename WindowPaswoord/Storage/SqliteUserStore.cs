using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using WindowPaswoord.Models;
using WinCalc.Security;
using WinCalc.Common;

namespace WinCalc.Storage
{
    public class SqliteUserStore
    {
        // Используем единый ConnectionString из WinCalc.Common.DbConfig
        private static string ConnString => DbConfig.ConnectionString;

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
            const string checkTable = "SELECT name FROM sqlite_master WHERE type='table' AND name='Users';";
            using var checkCmd = new SqliteCommand(checkTable, con);
            var tableName = await checkCmd.ExecuteScalarAsync();

            if (tableName == null) return false;

            using var cmd = new SqliteCommand("SELECT EXISTS(SELECT 1 FROM Users LIMIT 1);", con);
            var res = (long)await cmd.ExecuteScalarAsync();
            return res == 1;
        }

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