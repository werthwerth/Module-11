using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace VoiceTexterBot
{
    internal class Bot : BackgroundService
    {
        private ITelegramBotClient _telegramClient;

        public Bot(ITelegramBotClient telegramClient)
        {
            _telegramClient = telegramClient;
        }

        private int _curentMode = 0;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _telegramClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                new ReceiverOptions() { AllowedUpdates = { } }, // Здесь выбираем, какие обновления хотим получать. В данном случае разрешены все
                cancellationToken: stoppingToken);

            Console.WriteLine("Бот запущен");
        }

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message != null)
            {
                //  Обрабатываем нажатия на кнопки  из Telegram Bot API: https://core.telegram.org/bots/api#callbackquery
                if (update.Message.Text == "/numbers")
                {
                    _curentMode = 1;
                    await _telegramClient.SendTextMessageAsync(update.Message.Chat.Id, "You chose Numbers mode", cancellationToken: cancellationToken);
                }
                else if (update.Message.Text == "/symbols")
                {
                    _curentMode = 2;
                    await _telegramClient.SendTextMessageAsync(update.Message.Chat.Id, "You chose String mode", cancellationToken: cancellationToken);
                }
                else
                {
                    if (_curentMode == 0)
                    {
                        await _telegramClient.SendTextMessageAsync(update.Message.Chat.Id, string.Format("You type: {0}", update.Message.Text), cancellationToken: cancellationToken);
                    }
                    else if (_curentMode == 1)
                    {
                        Regex regex = new Regex(@"^\d+$");
                        MatchCollection matches = regex.Matches(update.Message.Text.Replace(" ", ""));
                        if (matches.Count > 0)
                        {
                            long Sum = 0;
                            foreach (string item in update.Message.Text.Split(" "))
                            {
                                long.TryParse(item, out long Int);
                                Sum += Int;
                            }
                            await _telegramClient.SendTextMessageAsync(update.Message.Chat.Id, string.Format("Sum of numbers is: {0}", Sum), cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await _telegramClient.SendTextMessageAsync(update.Message.Chat.Id, "Wrong input", cancellationToken: cancellationToken);
                        }
                    }
                    else if (_curentMode == 2)
                    {
                        await _telegramClient.SendTextMessageAsync(update.Message.Chat.Id, string.Format("Your string lengts is: {0}", update.Message.Text.Length), cancellationToken: cancellationToken);
                    }
                }
            }
            return;
        }

        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Задаем сообщение об ошибке в зависимости от того, какая именно ошибка произошла
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            // Выводим в консоль информацию об ошибке
            Console.WriteLine(errorMessage);

            // Задержка перед повторным подключением
            Console.WriteLine("Ожидаем 10 секунд перед повторным подключением.");
            Thread.Sleep(10000);

            return Task.CompletedTask;
        }
    }
}