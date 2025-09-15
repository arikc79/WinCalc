using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WinCalc.Models;

namespace WinCalc.Storage
{
    public class FileUserStore
    {
        private readonly string _path;
        private readonly object _lock = new();

        public FileUserStore(string? path = null)
        {
            _path = path ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "users.json");
            if (!File.Exists(_path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                File.WriteAllText(_path, "[]");
            }
        }

        private List<User> ReadAll()
        {
            lock (_lock)
            {
                var json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
            }
        }

        private void WriteAll(List<User> users)
        {
            lock (_lock)
            {
                var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_path, json);
            }
        }

        public Task<User?> GetByUsernameAsync(string username)
        {
            var users = ReadAll();
            return Task.FromResult(users.FirstOrDefault(
                u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<User> CreateAsync(User user)
        {
            var users = ReadAll();
            user.Id = users.Count == 0 ? 1 : users.Max(u => u.Id) + 1;
            users.Add(user);
            WriteAll(users);
            return Task.FromResult(user);
        }

        public Task UpdateAsync(User user)
        {
            var users = ReadAll();
            var idx = users.FindIndex(u => u.Id == user.Id);
            if (idx >= 0)
            {
                users[idx] = user;
                WriteAll(users);
            }
            return Task.CompletedTask;
        }
        // ADD: проверка, есть ли хоть один пользователь
        public Task<bool> AnyAsync()
        {
            var users = ReadAll();
            return Task.FromResult(users.Count > 0);
        }
    }
}

