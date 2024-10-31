using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace YvetlinTgBot
{
    public static class CommandsHandler
    {
        private static Dictionary<long, string> userStates = new();
        private static Dictionary<long, BigInteger> userKeys = new();

        public static async Task HandleMessageAsync(ITelegramBotClient bot, Message message, BotConfig config)
        {
            var chatId = message.Chat.Id;
            var text = message.Text;

            if (text.StartsWith("/start"))
            {
                userStates[chatId] = "main";
                await SendMainMessage(bot, chatId);
            }
            else if (userStates.ContainsKey(chatId))
            {
                string state = userStates[chatId];

                if (state == "choose_key_encrypt" || state == "choose_key_decrypt")
                {
                    if (text == "default")
                    {
                        userKeys[chatId] = BigInteger.Parse("6E3272357538782F413F4428472B4B62", System.Globalization.NumberStyles.HexNumber);
                    }
                    else
                    {
                        try
                        {
                            userKeys[chatId] = BigInteger.Parse(text, System.Globalization.NumberStyles.HexNumber);
                        }
                        catch
                        {
                            await bot.SendTextMessageAsync(chatId, "Неверный формат ключа! Пожалуйста, используйте ключ в формате hex.");
                            return;
                        }
                    }

                    userStates[chatId] = state == "choose_key_encrypt" ? "enter_text_encrypt" : "enter_text_decrypt";
                    await bot.SendTextMessageAsync(chatId, "Пожалуйста, введите текст для шифровки/дешифровки.");
                }
                else if (state == "enter_text_encrypt")
                {
                    string plainText = text;
                    BigInteger key = userKeys[chatId];
                    IdeaCipher cipher = new IdeaCipher(key);

                    ulong plainValue = BitConverter.ToUInt64(Encoding.ASCII.GetBytes(plainText), 0);
                    ulong encryptedValue = cipher.Encrypt(plainValue);

                    await bot.SendTextMessageAsync(chatId, $"Зашифрованный текст: {encryptedValue:X}");
                    userStates[chatId] = "main";
                }
                else if (state == "enter_text_decrypt")
                {
                    try
                    {
                        ulong encryptedValue = ulong.Parse(text, System.Globalization.NumberStyles.HexNumber);
                        BigInteger key = userKeys[chatId];
                        IdeaCipher cipher = new IdeaCipher(key);

                        ulong decryptedValue = cipher.Decrypt(encryptedValue);
                        string decryptedText = Encoding.ASCII.GetString(BitConverter.GetBytes(decryptedValue));

                        await bot.SendTextMessageAsync(chatId, $"Расшифрованный текст: {decryptedText}");
                    }
                    catch
                    {
                        await bot.SendTextMessageAsync(chatId, "Неверный формат шифротекста! Пожалуйста, используйте текст в формате hex.");
                    }
                    userStates[chatId] = "main";
                }
            }
        }

        public static async Task HandleCallbackQueryAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, BotConfig config)
        {
            var chatId = callbackQuery.Message.Chat.Id;
            var data = callbackQuery.Data;

            if (data == "encrypt")
            {
                userStates[chatId] = "choose_key_encrypt";
                await bot.SendTextMessageAsync(chatId, "Выберите ключ: введите 'default' для ключа по умолчанию или введите собственный ключ (в формате hex).");
            }
            else if (data == "decrypt")
            {
                userStates[chatId] = "choose_key_decrypt";
                await bot.SendTextMessageAsync(chatId, "Выберите ключ: введите 'default' для ключа по умолчанию или введите собственный ключ (в формате hex).");
            }
        }

        private static async Task SendMainMessage(ITelegramBotClient bot, long chatId)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Шифровка", "encrypt") },
                new[] { InlineKeyboardButton.WithCallbackData("Дешифровка", "decrypt") }
            });

            await bot.SendTextMessageAsync(chatId, "Добрый день! Я бот, запрограммированный на шифровку и дешифровку сообщений!", replyMarkup: keyboard);
        }
    }
}
