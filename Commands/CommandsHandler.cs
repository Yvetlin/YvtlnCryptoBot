using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramEncryptionBot.Database;
using TelegramEncryptionBot.Crypto;

namespace TelegramEncryptionBot.Commands
{
    public static class CommandsHandler
    {
        public static async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var text = message.Text;
            var chatId = message.Chat.Id;

            // Обработка команды /start
            if (text == "/start")
            {
                var tgTag = message.From.Username;
                if (!DatabaseManager.UserExists(tgTag))
                {
                    DatabaseManager.AddChatData(tgTag, "", "");
                    await botClient.SendTextMessageAsync(chatId, "Добро пожаловать! Пожалуйста, зарегистрируйтесь, указав свой Telegram тег (например, @username):", cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "👋 Добро пожаловать обратно! Вот главное меню:", cancellationToken: cancellationToken);
                    await SendMainMenu(botClient, chatId, cancellationToken);
                }
            }
            else
            {
                var tgTag = message.From.Username;
                var userStage = DatabaseManager.GetUserStage(tgTag);

                switch (userStage)
                {
                    case "waiting_for_custom_key":
                        // Сохраняем пользовательский ключ
                        DatabaseManager.SetDefaultKey(tgTag, text);
                        await botClient.SendTextMessageAsync(chatId, "🔐 Ваш собственный ключ установлен!", cancellationToken: cancellationToken);
                        await SendMainMenu(botClient, chatId, cancellationToken);
                        break;

                    case "waiting_for_encryption_text":
                        // Выполняем шифрование
                        var encryptionKey = DatabaseManager.GetDefaultKey(tgTag);
                        var cipher = new IdeaCipher(encryptionKey, true);
                        var encryptedText = cipher.Encrypt(text);
                        await botClient.SendTextMessageAsync(chatId, $"🔒 Зашифрованный текст: {encryptedText}", cancellationToken: cancellationToken);
                        await SendMainMenu(botClient, chatId, cancellationToken);
                        break;

                    case "waiting_for_decryption_text":
                        // Выполняем дешифрование
                        encryptionKey = DatabaseManager.GetDefaultKey(tgTag);
                        cipher = new IdeaCipher(encryptionKey, false);
                        try
                        {
                            var decryptedText = cipher.Decrypt(text);
                            await botClient.SendTextMessageAsync(chatId, $"🔓 Расшифрованный текст: {decryptedText}", cancellationToken: cancellationToken);
                        }
                        catch
                        {
                            await botClient.SendTextMessageAsync(chatId, "❌ Ошибка дешифрования. Проверьте входные данные.", cancellationToken: cancellationToken);
                        }
                        await SendMainMenu(botClient, chatId, cancellationToken);
                        break;

                    default:
                        await botClient.SendTextMessageAsync(chatId, "Неизвестная команда. Пожалуйста, выберите действие в меню.", cancellationToken: cancellationToken);
                        break;
                }
            }
        }

        public static async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var tgTag = callbackQuery.From.Username;

            switch (callbackQuery.Data)
            {
                case "change_key":
                    // Предоставляем выбор между случайным ключом и собственным
                    var replyMarkup = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("🖊️ Ввести собственный ключ", "set_custom_key"),
                            InlineKeyboardButton.WithCallbackData("🎲 Использовать случайный ключ", "set_random_key")
                        }
                    });
                    await botClient.SendTextMessageAsync(chatId, "Выберите способ установки ключа:", replyMarkup: replyMarkup, cancellationToken: cancellationToken);
                    break;

                case "set_custom_key":
                    // Установка пользовательского ключа
                    await botClient.SendTextMessageAsync(chatId, "Введите ваш собственный ключ:", cancellationToken: cancellationToken);
                    DatabaseManager.SetUserStage(tgTag, "waiting_for_custom_key");
                    break;

                case "set_random_key":
                    // Установка случайного ключа
                    var randomKey = DatabaseManager.GenerateRandomHexKey();
                    DatabaseManager.SetDefaultKey(tgTag, randomKey);
                    await botClient.SendTextMessageAsync(chatId, $"🎉 Ваш новый ключ установлен: {randomKey}", cancellationToken: cancellationToken);
                    await SendMainMenu(botClient, chatId, cancellationToken);
                    break;

                case "encrypt":
                    await botClient.SendTextMessageAsync(chatId, "🔒 Введите текст для шифрования:", cancellationToken: cancellationToken);
                    DatabaseManager.SetUserStage(tgTag, "waiting_for_encryption_text");
                    break;

                case "decrypt":
                    await botClient.SendTextMessageAsync(chatId, "🔓 Введите текст для дешифрования:", cancellationToken: cancellationToken);
                    DatabaseManager.SetUserStage(tgTag, "waiting_for_decryption_text");
                    break;

                default:
                    await botClient.SendTextMessageAsync(chatId, "Неизвестная команда. Пожалуйста, выберите действие в меню.", cancellationToken: cancellationToken);
                    await SendMainMenu(botClient, chatId, cancellationToken);
                    break;
            }
        }

        public static async Task SendMainMenu(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var replyMarkup = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Изменить дефолтный ключ", "change_key"),
                    InlineKeyboardButton.WithCallbackData("Шифрование", "encrypt"),
                    InlineKeyboardButton.WithCallbackData("Дешифрование", "decrypt")
                }
            });

            await botClient.SendTextMessageAsync(chatId, "Главное меню\nВыберите следующие действия:", replyMarkup: replyMarkup, cancellationToken: cancellationToken);
        }
    }
}
