using System.Diagnostics;
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

            if (reminder is { ReminderDays.Length: > 0, IsSave: false })
            {
                _creationStages.Remove(stage);
                reminder.IsSave = true;
                text = "Сохранено";
            }
            else
            {
                text = $"Ошибка. Кол-во дней: {reminder.ReminderDays} ---- Статус: {reminder.IsSave}";
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
        string usage;
        var stage = _creationStages.FirstOrDefault(x => x.UserId == message.From.Id);

        if (stage != null)
        {
            switch (stage.StageType)
            {
                case CreationStage.Stages.Title:
                    usage = "Введите время напоминания";
                    SetReminderTitle(stage, message.Text);
                    break;
                case CreationStage.Stages.Time:
                    usage = SetReminderTime(stage, message.Text);
                    break;
                case CreationStage.Stages.Day:
                    usage = "Введите дату напоминания";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else
        {
            usage = "Доступные команды:\n" +
                    "/create_reminder - создать напоминание\n" +
                    "/list_reminder   - список всех напоминаний";
        }

        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: usage,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Установить название напоминанию
    /// </summary>
    private static void SetReminderTitle(CreationStage stage, string message)
    {
        _reminders.First(x => x.ReminderId == stage.ReminderId).Title = message;
        stage.StageType = CreationStage.Stages.Time;
    }

    /// <summary>
    /// Установить время напоминания
    /// </summary>
    private static string SetReminderTime(CreationStage stage, string message)
    {
        try
        {
            TimeOnly time = TimeOnly.Parse(message);
            _reminders.First(x => x.ReminderId == stage.ReminderId).RimenderTime = time;
        }
        catch (Exception e)
        {
            return "не удалось конвертировать значение";
        }

        stage.StageType = CreationStage.Stages.Day;

        return "Введите дату напоминания";
    }

    /// <summary>
    /// Установить дни 
    /// </summary>
    private static void SetReminderDays(CreationStage stage, string message)
    {
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