using ReminderTg.Infrastructure.Models;
using ReminderTg.Infrastructure.Repositories;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ReminderTg.Services;

public class UpdateHandler : IUpdateHandler
{
    public UpdateHandler(ITelegramBotClient botClient, ILogger<UpdateHandler> logger,
        IReminderRepository reminderRepository, ICreationStagesRepository creationStagesRepository)
    {
        _botClient = botClient;
        _logger = logger;
        _reminderRepository = reminderRepository;
        _creationStagesRepository = creationStagesRepository;
    }

    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly IReminderRepository _reminderRepository;
    private readonly ICreationStagesRepository _creationStagesRepository;

    /// <summary>
    /// Хендлер событий в боте
    /// </summary>
    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        switch (update)
        {
            case { Message: { } message }:
                await BotOnMessageReceived(message, cancellationToken);
                break;
            case { CallbackQuery: { } callbackQuery }:
                await BotOnCallbackQueryReceived(callbackQuery, cancellationToken);
                break;
            default:
                await Task.CompletedTask;
                break;
        }
    }

    /// <summary>
    /// Обработчик callback запроса
    /// </summary>
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        if (callbackQuery.Data is not { } messageTextData)
            return;

        switch (messageTextData.Split(' ')[0])
        {
            case "/end_reminder":
                await CallbackQueryEndReminder(callbackQuery, cancellationToken);
                break;
            default:
                await OtherCallbackQuery(callbackQuery, cancellationToken);
                break;
        }
    }

    /// <summary>
    /// Обработка дефолтных запросов
    /// </summary>
    private async Task OtherCallbackQuery(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var stage = _creationStagesRepository.GetStage(callbackQuery.From.Id);

        if (stage != null)
            await SetReminderDays(stage, callbackQuery, cancellationToken);
    }

    /// <summary>
    /// Завершение ввода напоминания
    /// </summary>
    private async Task CallbackQueryEndReminder(CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        string text;
        var stage = _creationStagesRepository.GetStage(callbackQuery.From.Id);

        if (stage is { StageType: CreationStage.Stages.Day })
        {
            var reminder = await _reminderRepository.GetReminderById(stage.ReminderId);

            if (reminder is { ReminderDays.Count: > 0, IsSave: false })
            {
                _creationStagesRepository.RemoveStage(stage);
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

        await _botClient.SendTextMessageAsync(
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
        if (message.Text is not { } messageText)
            return;

        switch (messageText.Split(' ')[0])
        {
            case "/create_reminder":
                await InitializeReminder(message, cancellationToken);
                break;
            case "/list_reminder":
                await GetReminderList(message, cancellationToken);
                break;
            default:
                await OtherMessage(message, cancellationToken);
                break;
        }
    }

    /// <summary>
    /// Получить список всех напоминаний юзера
    /// </summary>
    private async Task GetReminderList(Message message,
        CancellationToken cancellationToken)
    {
        var reminders = await _reminderRepository.GetAllUserReminders(message.From.Id);

        foreach (var reminder in reminders)
        {
            InlineKeyboardMarkup inlineKeyboard =
                InlineKeyboardButton.WithCallbackData("Удалить", $"/delete_reminder_{reminder.Id}");

            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: reminder.ToString(),
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Создать напоминание
    /// </summary>
    private async Task InitializeReminder(Message message,
        CancellationToken cancellationToken)
    {
        ReminderModel reminder = new(message.From.Id);
        CreationStage stage = new(message.Chat.Id, reminder.Id);

        await _reminderRepository.AddReminder(reminder);
        _creationStagesRepository.AddStage(stage);

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Введите название напоминания:",
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    private async Task OtherMessage(Message message,
        CancellationToken cancellationToken)
    {
        var stage = _creationStagesRepository.GetStage(message.From.Id);

        if (stage != null)
        {
            switch (stage.StageType)
            {
                case CreationStage.Stages.Title:
                    await SetReminderTitle(stage, message, cancellationToken);
                    break;
                case CreationStage.Stages.Time:
                    await SetReminderTime(stage, message, cancellationToken);
                    break;
                case CreationStage.Stages.Day:
                    await Task.CompletedTask;
                    break;
                default:
                    await Task.CompletedTask;
                    break;
            }

            return;
        }

        const string usage = "Доступные команды:\n" +
                             "/create_reminder - создать напоминание\n" +
                             "/list_reminder   - список всех напоминаний";

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: usage,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Установить название напоминанию
    /// </summary>
    private async Task SetReminderTitle(CreationStage stage, Message message,
        CancellationToken cancellationToken)
    {
        var reminder = await _reminderRepository.GetReminderById(stage.ReminderId);
        reminder.Title = message.Text;
        stage.StageType = CreationStage.Stages.Time;
        _reminderRepository.UpdateReminder(reminder);

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Введите время напоминания:",
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Установить время напоминания
    /// </summary>
    private async Task SetReminderTime(CreationStage stage, Message message,
        CancellationToken cancellationToken)
    {
        //TODO сделать более умный парсер
        bool isParsed = TimeOnly.TryParse(message.Text, out TimeOnly resultTime);

        if (!isParsed)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Ошибка конвертации времени, попробуйте снова:",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);

            return;
        }

        var reminder = await _reminderRepository.GetReminderById(stage.ReminderId);
        reminder.ReminderTime = resultTime;
        stage.StageType = CreationStage.Stages.Day;
        _reminderRepository.UpdateReminder(reminder);

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

        await _botClient.SendTextMessageAsync(
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
        var reminder = await _reminderRepository.GetReminderById(stage.ReminderId);
        reminder.ReminderDays.Add(dayOfWeek);
        _reminderRepository.UpdateReminder(reminder);

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