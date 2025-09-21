namespace WindowPaswoord.Models
{
    /// <summary>
    /// Модель користувача з підтримкою хешованого пароля.
    /// Використовується для авторизації та управління ролями.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Унікальний ідентифікатор користувача (Primary Key в БД).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Логін користувача (унікальний).
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Хешований пароль (генерується через PasswordHasher).
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Роль користувача (admin / manager).
        /// </summary>
        public string Role { get; set; } = "manager";
    }
}
