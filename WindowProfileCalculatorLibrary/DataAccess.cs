using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace WindowProfileCalculatorLibrary
{
    public class DataAccess
    {
        private readonly string _dbPath = "window_calc.db";

        public void CreateUser(string login, string password, string role)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    string query = "INSERT INTO Users (Login, Password, Role) VALUES (@login, @password, @role)";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@password", password);
                        command.Parameters.AddWithValue("@role", role);
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating user: {ex.Message}");
            }
        }

        public List<(string Login, string Password, string Role)> ReadUsers()
        {
            var users = new List<(string Login, string Password, string Role)>();
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    string query = "SELECT Login, Password, Role FROM Users";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                users.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading users: {ex.Message}");
            }
            return users;
        }

        public void UpdateUser(string login, string newPassword, string newRole)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    string query = "UPDATE Users SET Password = @password, Role = @role WHERE Login = @login";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@password", newPassword);
                        command.Parameters.AddWithValue("@role", newRole);
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user: {ex.Message}");
            }
        }

        public void DeleteUser(string login)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    string query = "DELETE FROM Users WHERE Login = @login";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user: {ex.Message}");
            }
        }

        // Аналогічні методи для Materials
        public void CreateMaterial(string category, string name, string color, double price, string unit, string quantityType, string description)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    string query = "INSERT INTO Materials (Category, Name, Color, Price, Unit, QuantityType, Description) VALUES (@category, @name, @color, @price, @unit, @quantityType, @description)";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@category", category);
                        command.Parameters.AddWithValue("@name", name);
                        command.Parameters.AddWithValue("@color", (object)color ?? DBNull.Value); // Обробка NULL
                        command.Parameters.AddWithValue("@price", price);
                        command.Parameters.AddWithValue("@unit", unit);
                        command.Parameters.AddWithValue("@quantityType", quantityType);
                        command.Parameters.AddWithValue("@description", description);
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating material: {ex.Message}");
            }
        }

        public List<(string Category, string Name, string Color, double Price, string Unit, string QuantityType, string Description)> ReadMaterials()
        {
            var materials = new List<(string, string, string, double, string, string, string)>();
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    string query = "SELECT Category, Name, Color, Price, Unit, QuantityType, Description FROM Materials";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                materials.Add((
                                    reader.GetString(0),
                                    reader.GetString(1),
                                    reader.IsDBNull(2) ? null : reader.GetString(2),
                                    reader.GetDouble(3),
                                    reader.GetString(4),
                                    reader.GetString(5),
                                    reader.GetString(6)
                                ));
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading materials: {ex.Message}");
            }
            return materials;
        }

        public void UpdateMaterial(int id, string category, string name, string color, double price, string unit, string quantityType, string description)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    string query = "UPDATE Materials SET Category = @category, Name = @name, Color = @color, Price = @price, Unit = @unit, QuantityType = @quantityType, Description = @description WHERE Id = @id";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.Parameters.AddWithValue("@category", category);
                        command.Parameters.AddWithValue("@name", name);
                        command.Parameters.AddWithValue("@color", (object)color ?? DBNull.Value);
                        command.Parameters.AddWithValue("@price", price);
                        command.Parameters.AddWithValue("@unit", unit);
                        command.Parameters.AddWithValue("@quantityType", quantityType);
                        command.Parameters.AddWithValue("@description", description);
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating material: {ex.Message}");
            }
        }

        public void DeleteMaterial(int id)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    string query = "DELETE FROM Materials WHERE Id = @id";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting material: {ex.Message}");
            }
        }
    }
}
