using ReminderTg.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ReminderTg.Services;

public class UpdateHandler : IUpdateHandler
{
    public UpdateHandler(ITelegramBotClient botClient, ILogger<UpdateHandler> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private static List<ReminderModel> _reminders = new();
    private static List<CreationStage> _creationStages = new();


    /// <summary>
    /// Хендлер событий в боте
    /// </summary>
    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
            _ => Task.CompletedTask
        };

        await handler;
    }

    /// <summary>
    /// Обработчик сообщения
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Receive message type: {message.Type}");
        if (message.Text is not { } messageText)
            return;

        Task<Message> action = messageText.Split(' ')[0] switch
        {
            "/create_reminder" => InitializeReminder(_botClient, message, cancellationToken),
            "/end_reminder" => EndReminder(_botClient, message, cancellationToken),
            "/list_reminder" => GetReminderList(_botClient, message, cancellationToken),
            _ => Usage(_botClient, message, cancellationToken)
        };

        Message sentMessage = await action;
        _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
    }

    /// <summary>
    /// Получить список всех напоминаний юзера
    /// </summary>
    private async Task<Message> GetReminderList(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        var reminders = _reminders.Where(x => x.UserId == message.From.Id).Select(x => x.Title);

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: string.Join(", ", reminders),
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Завершить заполнение и сохранить напоминание
    /// </summary>
    /// <returns></returns>
    private async Task<Message> EndReminder(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        string text = string.Empty;
        var stage = _creationStages.FirstOrDefault(x => x.UserId == message.From.Id);

        if (stage is { StageType: CreationStage.Stages.Day })
        {
            var reminder = _reminders.FirstOrDefault(x => x.UserId == message.From.Id);

            if (reminder is { ReminderDays.Count: > 0, IsSave: false })
            {
                _creationStages.Remove(stage);
                reminder.IsSave = true;
                text = "Сохранено";
            }
            else
            {
                text = $"Ошибка. Кол-во дней: {reminder.ReminderDays.Count}. Статус: {reminder.IsSave}";
            }
        }
        else
        {
            text = $"Ошибка. Состояние: {stage.StageType}";
        }

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: text,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Создать напоминание
    /// </summary>
    private async Task<Message> InitializeReminder(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        ReminderModel reminder = new(message.From.Id);
        CreationStage stage = new(message.Chat.Id, reminder.ReminderId);

        _reminders.Add(reminder);
        _creationStages.Add(stage);

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Введите название напоминания",
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    private static async Task<Message> Usage(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        var stage = _creationStages.FirstOrDefault(x => x.UserId == message.From.Id);

        if (stage != null)
        {
            return stage.StageType switch
            {
                CreationStage.Stages.Title => await SetReminderTitle(stage, botClient, message, cancellationToken),
                CreationStage.Stages.Time => await SetReminderTime(stage, botClient, message, cancellationToken),
                CreationStage.Stages.Day => await SetReminderDays(stage, botClient, message, cancellationToken),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        string usage = "Доступные команды:\n" +
                       "/create_reminder - создать напоминание\n" +
                       "/list_reminder   - список всех напоминаний";

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: usage,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Установить название напоминанию
    /// </summary>
    private static async Task<Message> SetReminderTitle(CreationStage stage, ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        _reminders.First(x => x.ReminderId == stage.ReminderId).Title = message.Text;
        stage.StageType = CreationStage.Stages.Time;

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Введите время напоминания:",
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Установить время напоминания
    /// </summary>
    private static async Task<Message> SetReminderTime(CreationStage stage, ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        bool isParsed = TimeOnly.TryParse(message.Text, out TimeOnly resultTime);

        if (!isParsed)
        {
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Ошибка конвертации времени, попробуйте снова:",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        _reminders.First(x => x.ReminderId == stage.ReminderId).ReminderTime = resultTime;
        stage.StageType = CreationStage.Stages.Day;

        InlineKeyboardMarkup inlineKeyboard = new(
            new[]
            {
                // first row
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("ПН", "1"),
                    InlineKeyboardButton.WithCallbackData("ВТ", "2"),
                    InlineKeyboardButton.WithCallbackData("СР", "3"),
                    InlineKeyboardButton.WithCallbackData("ЧТ", "4"),
                },
                // second row
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("ПТ", "5"),
                    InlineKeyboardButton.WithCallbackData("СБ", "6"),
                    InlineKeyboardButton.WithCallbackData("ВС", "0")
                },
            });

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Выберите дни напоминания:",
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Установить дни 
    /// </summary>
    private static async Task<Message> SetReminderDays(CreationStage stage, ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        bool isParsed = Int32.TryParse(message.Text, out int resultNumberDay);

        if (!isParsed)
        {
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Ошибка конвертации даты, попробуйте снова:",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        _reminders.First(x => x.ReminderId == stage.ReminderId).ReminderDays.Add((DayOfWeek)resultNumberDay);

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: resultNumberDay.ToString(),
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", errorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
}