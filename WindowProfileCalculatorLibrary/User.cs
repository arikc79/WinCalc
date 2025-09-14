namespace WindowProfileCalculatorLibrary
{
    public class User
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; } // Зберігатиметься у хешованому вигляді
        public string Role { get; set; } // "admin" або "manager"
    }
}