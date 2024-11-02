using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramEncryptionBot.Crypto;

namespace TelegramEncryptionBot.Commands
{
    public static class CommandsHandler
    {
        private static string encryptionKey = "default";
        private static bool isEncryptionMode = true;
        private static bool isAwaitingCustomKey = false; // Новая переменная для отслеживания состояния ожидания ключа

        public static async Task HandleMessageAsync(ITelegramBotClient botClient, Message? message)
        {
            if (message?.Text == null)
                return;

            // Главное меню с кнопками шифрования и дешифрования
            if (message.Text == "/start" || message.Text == "Главное меню")
            {
                isAwaitingCustomKey = false; // Сбрасываем состояние ожидания ключа
                var replyKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔒 Шифровка", "encrypt"),
                        InlineKeyboardButton.WithCallbackData("🔓 Дешифровка", "decrypt")
                    }
                });

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Добрый день! Я бот, запрограммированный на шифровку и дешифровку сообщений!",
                    replyMarkup: replyKeyboard
                );
            }
            // Обработка выбора режима шифрования/дешифрования
            else if (message.Text == "🔒 Шифровка" || message.Text == "🔓 Дешифровка")
            {
                isEncryptionMode = message.Text == "🔒 Шифровка";
                isAwaitingCustomKey = false; // Сбрасываем состояние ожидания ключа

                var replyKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Использовать стандартный ключ", "default_key"),
                        InlineKeyboardButton.WithCallbackData("Назначить свой ключ", "custom_key")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🏠 Главное меню", "main_menu")
                    }
                });

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Выберите ключ для использования или введите свой в формате hex.",
                    replyMarkup: replyKeyboard
                );
            }
            // Обработка пользовательского ключа
            else if (isAwaitingCustomKey)
            {
                encryptionKey = message.Text;
                isAwaitingCustomKey = false; // Сбрасываем состояние ожидания ключа

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Ключ успешно сохранен. Теперь введите сообщение, которое нужно зашифровать или расшифровать."
                );
            }
            // Шифровка или дешифровка сообщения
            else
            {
                try
                {
                    var cipher = new IdeaCipher(encryptionKey, isEncryptionMode);
                    string resultMessage = isEncryptionMode ? cipher.Encrypt(message.Text) : cipher.Decrypt(message.Text);

                    var replyKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("🏠 Главное меню", "main_menu")
                        }
                    });

                    string action = isEncryptionMode ? "Зашифрованное" : "Расшифрованное";
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"{action} сообщение: {resultMessage}",
                        replyMarkup: replyKeyboard
                    );
                }
                catch (Exception ex)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"Ошибка: {ex.Message}",
                        replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("🏠 Главное меню", "main_menu"))
                    );
                }
            }
        }

        public static async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            if (callbackQuery.Message == null)
                return;

            switch (callbackQuery.Data)
            {
                case "encrypt":
                    isEncryptionMode = true;
                    isAwaitingCustomKey = false;
                    await HandleMessageAsync(botClient, new Message { Chat = callbackQuery.Message.Chat, Text = "🔒 Шифровка" });
                    break;
                case "decrypt":
                    isEncryptionMode = false;
                    isAwaitingCustomKey = false;
                    await HandleMessageAsync(botClient, new Message { Chat = callbackQuery.Message.Chat, Text = "🔓 Дешифровка" });
                    break;
                case "default_key":
                    encryptionKey = "0A1B2C3D4E5F67890A1B2C3D4E5F6789";
                    isAwaitingCustomKey = false;
                    await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Используется стандартный ключ. Введите сообщение для обработки.");
                    break;
                case "custom_key":
                    isAwaitingCustomKey = true; // Включаем режим ожидания пользовательского ключа
                    await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Введите ваш ключ в формате hex.");
                    break;
                case "main_menu":
                    isAwaitingCustomKey = false;
                    await HandleMessageAsync(botClient, new Message { Chat = callbackQuery.Message.Chat, Text = "Главное меню" });
                    break;
            }
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }
    }
}
