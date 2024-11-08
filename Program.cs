using System;
using System.IO; // Явно используем File из System.IO
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramEncryptionBot.Commands;

namespace TelegramEncryptionBot
{
    class Program
    {
        public static TelegramBotClient BotClient = null!; // Установка начального значения null! для устранения предупреждения

        static async Task Main(string[] args)
        {
            try
            {
                // Путь к конфигурационному файлу
                string configPath = @"C:\Users\yvtln\RiderProjects\YvtlnCryptoBot\YvtlnCryptoBot\config.json";

                
                // Проверка наличия файла config.json
                if (!System.IO.File.Exists(configPath)) // Указание System.IO.File
                {
                    Console.WriteLine($"Ошибка: файл конфигурации '{configPath}' не найден в рабочем каталоге '{Environment.CurrentDirectory}'.");
                    return;
                }

                // Загрузка токена из конфигурации
                var config = JsonSerializer.Deserialize<BotConfig>(System.IO.File.ReadAllText(configPath)); // Указание System.IO.File

                if (config == null || string.IsNullOrWhiteSpace(config.BotToken))
                {
                    Console.WriteLine("Ошибка: токен бота не найден в конфигурации.");
                    return;
                }

                BotClient = new TelegramBotClient(config.BotToken);

                var cts = new CancellationTokenSource();
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery } // Указание типов обновлений
                };

                BotClient.StartReceiving(
                    updateHandler: HandleUpdateAsync,
                    errorHandler: HandleErrorAsync,
                    receiverOptions: receiverOptions,
                    cancellationToken: cts.Token
                );

                var botInfo = await BotClient.GetMeAsync();
                Console.WriteLine($"Бот {botInfo.Username} запущен.");
                Console.ReadLine();
                cts.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запуске бота: {ex.Message}");
            }
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.Message && update.Message != null)
                {
                    await CommandsHandler.HandleMessageAsync(botClient, update.Message, cancellationToken);
                }
                else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
                {
                    await CommandsHandler.HandleCallbackQueryAsync(botClient, update.CallbackQuery, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке обновления: {ex.Message}");
            }
        }

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }

    public class BotConfig
    {
        public string BotToken { get; set; } = string.Empty; // Установка значения по умолчанию
        public string AdminPassword1 { get; set; } = string.Empty;
        public string AdminPassword2 { get; set; } = string.Empty;
    }
}
