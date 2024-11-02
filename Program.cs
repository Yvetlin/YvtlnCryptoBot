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
        public static TelegramBotClient? BotClient;

        static async Task Main(string[] args)
        {
            // Загрузка токена из конфигурации
            var config = JsonSerializer.Deserialize<BotConfig>(System.IO.File.ReadAllText(@"C:\Users\yvetlin\Documents\GitHub\YvtlnCryptoBot\config.json"));
            BotClient = new TelegramBotClient(config!.BotToken);

            var cts = new CancellationTokenSource();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery } // Указание типов обновлений
            };

            BotClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync, // Изменение pollingErrorHandler на errorHandler
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var botInfo = await BotClient.GetMeAsync();
            Console.WriteLine($"Бот {botInfo.Username} запущен.");
            Console.ReadLine();
            cts.Cancel();
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message != null)
            {
                await CommandsHandler.HandleMessageAsync(botClient, update.Message);
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                await CommandsHandler.HandleCallbackQueryAsync(botClient, update.CallbackQuery);
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
        public string BotToken { get; set; }
    }
}
