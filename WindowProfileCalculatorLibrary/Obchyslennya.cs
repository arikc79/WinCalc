using Microsoft.Data.Sqlite;

namespace WindowProfileCalculatorLibrary
{
    public class Obchyslennya
    {
        private readonly string _dbPath = "window_calc.db";

        /// <summary>
        /// ������� ������� Materials � Users � ��� �����, ���� ���� �� �� �������.
        /// </summary>
        public void CreateTables()
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    Console.WriteLine("Connection opened in CreateTables.");

                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        Console.WriteLine("Failed to open connection in CreateTables.");
                        return;
                    }

                    // ��������� ������� Users
                    string createUsersTable = @"
                        CREATE TABLE IF NOT EXISTS Users (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Login TEXT NOT NULL,
                            Password TEXT NOT NULL,
                            Role TEXT NOT NULL
                        )";
                    try
                    {
                        using (var command = new SqliteCommand(createUsersTable, connection))
                        {
                            int rowsAffected = command.ExecuteNonQuery();
                            Console.WriteLine($"Users table created/checked. Rows affected: {rowsAffected}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating Users table: {ex.Message}");
                    }

                    // ��������� ������� Materials
                    string createMaterialsTable = @"
                        CREATE TABLE IF NOT EXISTS Materials (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Category TEXT NOT NULL,
                            Name TEXT NOT NULL,
                            Color TEXT,
                            Price REAL NOT NULL,
                            Unit TEXT NOT NULL,
                            QuantityType TEXT NOT NULL,
                            Description TEXT
                        )";
                    try
                    {
                        using (var command = new SqliteCommand(createMaterialsTable, connection))
                        {
                            int rowsAffected = command.ExecuteNonQuery();
                            Console.WriteLine($"Materials table created/checked. Rows affected: {rowsAffected}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating Materials table: {ex.Message}");
                    }

                    // �������� �������� �������
                    using (var command = new SqliteCommand("SELECT name FROM sqlite_master WHERE type='table';", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            Console.WriteLine("Tables in database:");
                            while (reader.Read())
                            {
                                Console.WriteLine($"- {reader["name"]}");
                            }
                        }
                    }

                    connection.Close();
                    Console.WriteLine("CreateTables completed successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error in CreateTables: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
        /// <summary>
        /// �������� �������� ������� ������� ��� �������� �������������� ����.
        /// </summary>
        /// <param name="width">������ ���� (� ������)</param>
        /// <param name="height">������ ���� (� ������)</param>
        /// <param name="frameWidth">������ ���� (� ������)</param>
        /// <param name="overlap">��������� ������ (� ������)</param>
        /// <param name="weldingAllowance">������ �� ���������� (� ������ �� �����)</param>
        /// <returns>�������� ������� ������� (� ������)</returns>
        public double CalculateProfileLengthType1(double width, double height, double frameWidth, double overlap, double weldingAllowance)
        {
            if (width <= 0 || height <= 0 || frameWidth <= 0 || overlap <= 0 || weldingAllowance <= 0)
                throw new ArgumentException("�� ��������� ������ ���� ���������.");

            double framePerimeter = 2 * (width + height) + weldingAllowance * 8;
            double sashWidth = width - 2 * frameWidth + 2 * overlap;
            double sashHeight = height - 2 * frameWidth + 2 * overlap;
            double sashPerimeter = 2 * (sashWidth + sashHeight) + weldingAllowance * 8;

            return framePerimeter + sashPerimeter;
        }

        /// <summary>
        /// �������� �������� ������� ������� ��� ����, ������� ����� (2 ������, 1 ���������).
        /// </summary>
        /// <param name="width">������ ���� (� ������)</param>
        /// <param name="height">������ ���� (� ������)</param>
        /// <param name="frameWidth">������ ���� (� ������)</param>
        /// <param name="midFrameWidth">������ ������� (� ������)</param>
        /// <param name="overlap">��������� ������ (� ������)</param>
        /// <param name="weldingAllowance">������ �� ���������� (� ������ �� �����)</param>
        /// <returns>�������� ������� ������� (� ������)</returns>
        public double CalculateProfileLengthType2(double width, double height, double frameWidth, double midFrameWidth, double overlap, double weldingAllowance)
        {
            if (width <= 0 || height <= 0 || frameWidth <= 0 || midFrameWidth <= 0 || overlap <= 0 || weldingAllowance <= 0)
                throw new ArgumentException("�� ��������� ������ ���� ���������.");

            double framePerimeter = 2 * (width + height) + weldingAllowance * 8;
            double mullionLength = (height - 2 * frameWidth + 2 * midFrameWidth) + weldingAllowance * 4;
            double sashWidth = width / 2 - frameWidth - midFrameWidth / 2 + 2 * overlap;
            double sashHeight = height - 2 * frameWidth + 2 * overlap;
            double sashPerimeter = 2 * (sashWidth + sashHeight) + weldingAllowance * 8;

            return framePerimeter + mullionLength + sashPerimeter;
        }

        /// <summary>
        /// �������� �������� ������� ������� ��� ����, ������� �� 3 ������� (3 ������, 1 ���������).
        /// </summary>
        /// <param name="width">������ ���� (� ������)</param>
        /// <param name="height">������ ���� (� ������)</param>
        /// <param name="frameWidth">������ ���� (� ������)</param>
        /// <param name="midFrameWidth">������ ������� (� ������)</param>
        /// <param name="overlap">��������� ������ (� ������)</param>
        /// <param name="weldingAllowance">������ �� ���������� (� ������ �� �����)</param>
        /// <returns>�������� ������� ������� (� ������)</returns>
        public double CalculateProfileLengthType3(double width, double height, double frameWidth, double midFrameWidth, double overlap, double weldingAllowance)
        {
            if (width <= 0 || height <= 0 || frameWidth <= 0 || midFrameWidth <= 0 || overlap <= 0 || weldingAllowance <= 0)
                throw new ArgumentException("�� ��������� ������ ���� ���������.");

            double framePerimeter = 2 * (width + height) + weldingAllowance * 8;
            double mullionLength = 2 * ((height - 2 * frameWidth + 2 * midFrameWidth) + weldingAllowance * 4);
            double sashWidth = width / 3 - frameWidth - midFrameWidth / 2 + 2 * overlap;
            double sashHeight = height - 2 * frameWidth + 2 * overlap;
            double sashPerimeter = 2 * (sashWidth + sashHeight) + weldingAllowance * 8;

            return framePerimeter + mullionLength + sashPerimeter;
        }

        /// <summary>
        /// �������� �������� ������� ������� ��� ���� � 4 �������� (4 ������, 2 ������������).
        /// </summary>
        /// <param name="width">������ ���� (� ������)</param>
        /// <param name="height">������ ���� (� ������)</param>
        /// <param name="frameWidth">������ ���� (� ������)</param>
        /// <param name="midFrameWidth">������ ������� (� ������)</param>
        /// <param name="overlap">��������� ������ (� ������)</param>
        /// <param name="weldingAllowance">������ �� ���������� (� ������ �� �����)</param>
        /// <returns>�������� ������� ������� (� ������)</returns>
        public double CalculateProfileLengthType4(double width, double height, double frameWidth, double midFrameWidth, double overlap, double weldingAllowance)
        {
            if (width <= 0 || height <= 0 || frameWidth <= 0 || midFrameWidth <= 0 || overlap <= 0 || weldingAllowance <= 0)
                throw new ArgumentException("�� ��������� ������ ���� ���������.");

            double framePerimeter = 2 * (width + height) + weldingAllowance * 8;
            double mullionLength = 3 * ((height - 2 * frameWidth + 2 * midFrameWidth) + weldingAllowance * 4);
            double sashWidth = width / 4 - frameWidth - midFrameWidth / 2 + 2 * overlap;
            double sashHeight = height - 2 * frameWidth + 2 * overlap;
            double sashPerimeter = 2 * (sashWidth + sashHeight) + weldingAllowance * 8;
            double totalSashLength = 2 * sashPerimeter; // 2 ����������� ������

            return framePerimeter + mullionLength + totalSashLength;
        }

        /// <summary>
        /// �������� �������� ������� ������� ��� ���� � 5 �������� (5 ������, 2 ������������).
        /// </summary>
        /// <param name="width">������ ���� (� ������)</param>
        /// <param name="height">������ ���� (� ������)</param>
        /// <param name="frameWidth">������ ���� (� ������)</param>
        /// <param name="midFrameWidth">������ ������� (� ������)</param>
        /// <param name="overlap">��������� ������ (� ������)</param>
        /// <param name="weldingAllowance">������ �� ���������� (� ������ �� �����)</param>
        /// <returns>�������� ������� ������� (� ������)</returns>
        public double CalculateProfileLengthType5(double width, double height, double frameWidth, double midFrameWidth, double overlap, double weldingAllowance)
        {
            if (width <= 0 || height <= 0 || frameWidth <= 0 || midFrameWidth <= 0 || overlap <= 0 || weldingAllowance <= 0)
                throw new ArgumentException("�� ��������� ������ ���� ���������.");

            double framePerimeter = 2 * (width + height) + weldingAllowance * 8;
            double mullionLength = 4 * ((height - 2 * frameWidth + 2 * midFrameWidth) + weldingAllowance * 4);
            double sashWidth = width / 5 - frameWidth - midFrameWidth / 2 + 2 * overlap;
            double sashHeight = height - 2 * frameWidth + 2 * overlap;
            double sashPerimeter = 2 * (sashWidth + sashHeight) + weldingAllowance * 8;
            double totalSashLength = 2 * sashPerimeter; // 2 ����������� ������

            return framePerimeter + mullionLength + totalSashLength;
        }
    }
}