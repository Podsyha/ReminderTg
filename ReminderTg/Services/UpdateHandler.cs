using ReminderTg.Infrastructure.Models;
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
            { CallbackQuery: { } callbackQuery } => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
            _ => Task.CompletedTask
        };

        await handler;
    }

    /// <summary>
    /// 
    /// </summary>
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

        if (callbackQuery.Data is not { } messageTextData)
            return;

        Task action = messageTextData.Split(' ')[0] switch
        {
            "/end_reminder" => CallbackQueryEndReminder(callbackQuery, cancellationToken),
            _ => OtherCallbackQuery(callbackQuery, cancellationToken)
        };

        await action;
    }

    private async Task OtherCallbackQuery(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var stage = _creationStages.FirstOrDefault(x => x.UserId == callbackQuery.From.Id);

        if (stage != null)
            await SetReminderDays(stage, callbackQuery, cancellationToken);
    }


    //TODO копия метода из обработчика message, переписать и актуализировать
    private async Task<Message> CallbackQueryEndReminder(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        string text = string.Empty;
        var stage = _creationStages.FirstOrDefault(x => x.UserId == callbackQuery.From.Id);

        if (stage is { StageType: CreationStage.Stages.Day })
        {
            var reminder = _reminders.FirstOrDefault(x => x.UserId == callbackQuery.From.Id);

            if (reminder is { ReminderDays.Count: > 0, IsSave: false })
            {
                _creationStages.Remove(stage);
                reminder.IsSave = true;
                text = "Сохранено";
            }
            else
            {
                text = $"Ошибка. Кол-во дней: {reminder.ReminderDays?.Count}. Статус: {reminder.IsSave}";
            }
        }
        else
        {
            text = $"Ошибка. Состояние: {stage?.StageType}";
        }

        return await _botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message.Chat.Id,
            text: text,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обработчик сообщения
    /// </summary>
    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Receive message type: {message.Type}");
        if (message.Text is not { } messageText)
            return;

        Task<Message> action = messageText.Split(' ')[0] switch
        {
            "/create_reminder" => InitializeReminder(message, cancellationToken),
            "/end_reminder" => MessageEndReminder(message, cancellationToken),
            "/list_reminder" => GetReminderList(message, cancellationToken),
            _ => OtherMessage(message, cancellationToken)
        };

        Message sentMessage = await action;
        _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
    }

    /// <summary>
    /// Получить список всех напоминаний юзера
    /// </summary>
    private async Task<Message> GetReminderList(Message message,
        CancellationToken cancellationToken)
    {
        var reminders = _reminders.Where(x => x.UserId == message.From.Id).Select(x => x.Title);

        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: string.Join(", ", reminders),
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Завершить заполнение и сохранить напоминание
    /// </summary>
    /// <returns></returns>
    private async Task<Message> MessageEndReminder(Message message,
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

        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: text,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Создать напоминание
    /// </summary>
    private async Task<Message> InitializeReminder(Message message,
        CancellationToken cancellationToken)
    {
        ReminderModel reminder = new(message.From.Id);
        CreationStage stage = new(message.Chat.Id, reminder.ReminderId);

        _reminders.Add(reminder);
        _creationStages.Add(stage);

        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Введите название напоминания",
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    private async Task<Message> OtherMessage(Message message,
        CancellationToken cancellationToken)
    {
        var stage = _creationStages.FirstOrDefault(x => x.UserId == message.From.Id);

        if (stage != null)
        {
            return stage.StageType switch
            {
                CreationStage.Stages.Title => await SetReminderTitle(stage, message, cancellationToken),
                CreationStage.Stages.Time => await SetReminderTime(stage, message, cancellationToken),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        string usage = "Доступные команды:\n" +
                       "/create_reminder - создать напоминание\n" +
                       "/list_reminder   - список всех напоминаний";

        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: usage,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Установить название напоминанию
    /// </summary>
    private async Task<Message> SetReminderTitle(CreationStage stage, Message message,
        CancellationToken cancellationToken)
    {
        _reminders.First(x => x.ReminderId == stage.ReminderId).Title = message.Text;
        stage.StageType = CreationStage.Stages.Time;

        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Введите время напоминания:",
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Установить время напоминания
    /// </summary>
    private async Task<Message> SetReminderTime(CreationStage stage, Message message,
        CancellationToken cancellationToken)
    {
        bool isParsed = TimeOnly.TryParse(message.Text, out TimeOnly resultTime);

        if (!isParsed)
        {
            return await _botClient.SendTextMessageAsync(
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
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("ПН", "1"),
                    InlineKeyboardButton.WithCallbackData("ВТ", "2"),
                    InlineKeyboardButton.WithCallbackData("СР", "3"),
                    InlineKeyboardButton.WithCallbackData("ЧТ", "4"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("ПТ", "5"),
                    InlineKeyboardButton.WithCallbackData("СБ", "6"),
                    InlineKeyboardButton.WithCallbackData("ВС", "0")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("ЗАВЕРШИТЬ", "/end_reminder"),
                }
            });

        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Выберите дни напоминания:",
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Установить дни 
    /// </summary>
    private async Task SetReminderDays(CreationStage stage, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        bool isParsed = Int32.TryParse(callbackQuery.Data, out int resultNumberDay);

        if (!isParsed)
        {
            await _botClient.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: "Ошибка конвертации даты, попробуйте снова:",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
            
            return;
        }

        DayOfWeek dayOfWeek = (DayOfWeek)resultNumberDay;

        _reminders.First(x => x.ReminderId == stage.ReminderId).ReminderDays.Add(dayOfWeek);

        await _botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: $"Received {callbackQuery.Data}",
            cancellationToken: cancellationToken);
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient _, Exception exception,
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