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

            // Основной сценарий регистрации
            if (text == "/start")
            {
                await botClient.SendTextMessageAsync(chatId, "👋 Привет! Я бот шифровщик 🔒. Для начала нужно зарегистрироваться.\nПожалуйста, укажи свой Telegram тег (например, @username):", cancellationToken: cancellationToken);
            }
            else if (text.StartsWith("@") && !Database.UserExists(text))
            {
                // Сохраняем Telegram тег
                var tgTag = text;
                Database.AddChatData(tgTag, "", "");  // Сохраняем тег, оставляем ключ и сообщение пустыми
                await botClient.SendTextMessageAsync(chatId, "🔑 Теперь нужно задать себе ключ.", cancellationToken: cancellationToken);

                // Отправляем кнопки для выбора ключа
                var replyMarkup = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🖊️ Ввести собственный ключ", "custom_key"),
                        InlineKeyboardButton.WithCallbackData("🎲 Использовать рандомный", "random_key")
                    }
                });
                await botClient.SendTextMessageAsync(chatId, "Выберите способ установки ключа:", replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            }
            else if (Database.UserExists(text))
            {
                await botClient.SendTextMessageAsync(chatId, "❌ Этот Telegram тег уже зарегистрирован. Пожалуйста, попробуйте другой.", cancellationToken: cancellationToken);
            }
        }

        public static async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var tgTag = callbackQuery.From.Username;

            if (callbackQuery.Data == "custom_key")
            {
                await botClient.SendTextMessageAsync(chatId, "🖊️ Введите ваш собственный ключ:", cancellationToken: cancellationToken);
                Database.SetUserStage(tgTag, "waiting_for_custom_key");
            }
            else if (callbackQuery.Data == "random_key")
            {
                var randomKey = Database.GenerateRandomHexKey();
                Database.SetDefaultKey(tgTag, randomKey);
                await botClient.SendTextMessageAsync(chatId, $"🎉 Ваш ключ установлен! Теперь можно начинать шифрование и дешифрование.", cancellationToken: cancellationToken);
            }
        }

        public static async Task HandleCustomKeyMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var tgTag = message.From.Username;
            var customKey = message.Text;
            Database.SetDefaultKey(tgTag, customKey);
            Database.SetUserStage(tgTag, "registered");
            await botClient.SendTextMessageAsync(message.Chat.Id, "🔐 Ваш собственный ключ сохранен! Теперь можно начинать шифрование и дешифрование.", cancellationToken: cancellationToken);
        }
    }
}
