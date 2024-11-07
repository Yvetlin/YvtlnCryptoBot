using System;
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
        public static TelegramBotClient BotClient;

        static async Task Main(string[] args)
        {
            try
            {
                // Загрузка токена из конфигурации
                var configContent = System.IO.File.ReadAllText(@"C:\Users\yvetlin\Documents\GitHub\YvtlnCryptoBot\config.json");
                var config = JsonSerializer.Deserialize<BotConfig>(configContent);
                
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
    }
}
