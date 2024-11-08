using Npgsql;
using System;
using System.Linq;

namespace TelegramEncryptionBot.Database
{
    public static class DatabaseManager
    {
        private static readonly string ConnectionString = "Host=localhost;Username=postgres;Password=qwe123123;Database=ChatInfo";

        public static bool UserExists(string tgTag)
        {
            try
            {
                using var conn = new NpgsqlConnection(ConnectionString);
                conn.Open();

                using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM chats WHERE tg_tag = @tgTag", conn);
                cmd.Parameters.AddWithValue("tgTag", tgTag);
                return (long)cmd.ExecuteScalar() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке существования пользователя: {ex.Message}");
                return false;
            }
        }

        public static void AddChatData(string tgTag, string defaultKey, string lastMessage)
        {
            try
            {
                using var conn = new NpgsqlConnection(ConnectionString);
                conn.Open();

                string lastKey = GenerateRandomHexKey();

                using var cmd = new NpgsqlCommand("INSERT INTO chats (tg_tag, default_key, last_key, last_message) VALUES (@tgTag, @defaultKey, @lastKey, @lastMessage)", conn);
                cmd.Parameters.AddWithValue("tgTag", tgTag);
                cmd.Parameters.AddWithValue("defaultKey", defaultKey);
                cmd.Parameters.AddWithValue("lastKey", lastKey);
                cmd.Parameters.AddWithValue("lastMessage", lastMessage);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении данных чата: {ex.Message}");
            }
        }

        public static void SetUserStage(string tgTag, string stage)
        {
            try
            {
                using var conn = new NpgsqlConnection(ConnectionString);
                conn.Open();

                using var cmd = new NpgsqlCommand("UPDATE chats SET stage = @stage WHERE tg_tag = @tgTag", conn);
                cmd.Parameters.AddWithValue("tgTag", tgTag);
                cmd.Parameters.AddWithValue("stage", stage);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при установке стадии пользователя: {ex.Message}");
            }
        }

        public static string GetUserStage(string tgTag)
        {
            try
            {
                using var conn = new NpgsqlConnection(ConnectionString);
                conn.Open();

                using var cmd = new NpgsqlCommand("SELECT stage FROM chats WHERE tg_tag = @tgTag", conn);
                cmd.Parameters.AddWithValue("tgTag", tgTag);
                return cmd.ExecuteScalar() as string ?? "unregistered";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении стадии пользователя: {ex.Message}");
                return "unregistered";
            }
        }

        public static string GenerateRandomHexKey()
        {
            var random = new Random();
            return string.Concat(Enumerable.Range(0, 8).Select(_ => random.Next(16).ToString("X")));
        }

        public static void SetDefaultKey(string tgTag, string key)
        {
            try
            {
                using var conn = new NpgsqlConnection(ConnectionString);
                conn.Open();

                using var cmd = new NpgsqlCommand("UPDATE chats SET default_key = @key WHERE tg_tag = @tgTag", conn);
                cmd.Parameters.AddWithValue("tgTag", tgTag);
                cmd.Parameters.AddWithValue("key", key);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при установке ключа по умолчанию: {ex.Message}");
            }
        }
        public static string GetDefaultKey(string tgTag)
        {
            try
            {
                using var conn = new NpgsqlConnection(ConnectionString);
                conn.Open();

                using var cmd = new NpgsqlCommand("SELECT default_key FROM chats WHERE tg_tag = @tgTag", conn);
                cmd.Parameters.AddWithValue("tgTag", tgTag);
                return cmd.ExecuteScalar() as string ?? string.Empty; // Возвращаем пустую строку, если ключ не найден
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении ключа по умолчанию: {ex.Message}");
                return string.Empty;
            }
        }
        public static string GetLastKey(string tgTag)
        {
            try
            {
                using var conn = new NpgsqlConnection(ConnectionString);
                conn.Open();

                using var cmd = new NpgsqlCommand("SELECT last_key FROM chats WHERE tg_tag = @tgTag", conn);
                cmd.Parameters.AddWithValue("tgTag", tgTag);

                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? string.Empty; // Возвращает last_key или пустую строку, если значение отсутствует
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении last_key: {ex.Message}");
                return string.Empty;
            }
        }

    }
}
