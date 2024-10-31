using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace YvetlinTgBot
{
    public class Program
    {
        private static ITelegramBotClient botClient;
        private static BotConfig config;

        public static async Task Main()
        {
            // Загружаем конфигурацию
            config = LoadConfig("config.json");

            botClient = new TelegramBotClient(config.Token);
            var me = await botClient.GetMeAsync();
            Console.WriteLine($"Start listening for @{me.Username}");

            botClient.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync));

            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
        }

        private static BotConfig LoadConfig(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                throw new FileNotFoundException("Configuration file not found.");
            }

            string json = System.IO.File.ReadAllText(path);
            return JsonSerializer.Deserialize<BotConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        

        private static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message.Text != null)
            {
                await CommandsHandler.HandleMessageAsync(bot, update.Message, config);
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                await CommandsHandler.HandleCallbackQueryAsync(bot, update.CallbackQuery, config);
            }
        }

        private static Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(exception.Message);
            return Task.CompletedTask;
        }
    }

    public class BotConfig
    {
        public string Token { get; set; }
        public string[] AdminPasswords { get; set; }
    }
}
