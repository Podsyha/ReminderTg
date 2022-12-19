using System.Globalization;
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
        IRepeatReminderRepository repeatReminderRepository, ICreationStagesRepository creationStagesRepository, IOnceReminderRepository onceReminderRepository)
    {
        _botClient = botClient;
        _logger = logger;
        _repeatReminderRepository = repeatReminderRepository;
        _creationStagesRepository = creationStagesRepository;
        _onceReminderRepository = onceReminderRepository;
    }

    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly IRepeatReminderRepository _repeatReminderRepository;
    private readonly ICreationStagesRepository _creationStagesRepository;
    private readonly IOnceReminderRepository _onceReminderRepository;

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

        var message = messageTextData.Split(' ')[0];

        if (message == "/end_reminder")
            await CallbackQueryEndReminder(callbackQuery, cancellationToken);
        else if (message.StartsWith("/delete_reminder"))
            await CallbackQueryDeleteReminder(callbackQuery, message, cancellationToken);
        else
            await OtherCallbackQuery(callbackQuery, cancellationToken);
    }

    /// <summary>
    /// Удаление напоминания
    /// </summary>
    private async Task CallbackQueryDeleteReminder(CallbackQuery callbackQuery, string message,
        CancellationToken cancellationToken)
    {
        var messageStrings = message.Split('_');
        try
        {
            var reminderId = new Guid(messageStrings[^1]);
            var reminder = await _repeatReminderRepository.GetReminderById(reminderId);
            
            _repeatReminderRepository.RemoveReminder(reminder);
            
            await _botClient.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: "Удалено",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            await _botClient.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: $"Ошибка удаления: {e.Message}",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
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
        else
            await WriteCommands(callbackQuery.From.Id, cancellationToken);
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
            var reminder = await _repeatReminderRepository.GetReminderById(stage.ReminderId);

            if (reminder is { ReminderDays.Count: > 0, IsSave: false })
            {
                _creationStagesRepository.RemoveStage(stage);
                reminder.IsSave = true;
                _repeatReminderRepository.UpdateReminder(reminder);
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
        
        var text = messageText.Split(' ')[0];

        if (text == "/create_repeat_reminder")
            await InitializeReminder(message, cancellationToken);
        else if (text.StartsWith("/short//"))
            await CreateShortReminder(message, cancellationToken);
        else if (text == "/list_reminder")
            await GetReminderList(message, cancellationToken);
        else if (text == "/help")
            await WriteHelpMessage(message.From.Id, cancellationToken);
        else
            await OtherMessage(message, cancellationToken);
    }

    /// <summary>
    /// Быстрое создание напоминания
    /// </summary>
    private async Task CreateShortReminder(Message message, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Получить список всех напоминаний юзера
    /// </summary>
    private async Task GetReminderList(Message message,
        CancellationToken cancellationToken)
    {
        var reminders = await _repeatReminderRepository.GetAllUserReminders(message.From.Id);

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
        RepeatReminderModel repeatReminder = new(message.From.Id);
        CreationStage stage = new(message.Chat.Id, repeatReminder.Id);

        await _repeatReminderRepository.AddReminder(repeatReminder);
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

        await WriteCommands(message.From.Id, cancellationToken);
    }

    /// <summary>
    /// Установить название напоминанию
    /// </summary>
    private async Task SetReminderTitle(CreationStage stage, Message message,
        CancellationToken cancellationToken)
    {
        var reminder = await _repeatReminderRepository.GetReminderById(stage.ReminderId);
        reminder.Title = message.Text;
        stage.StageType = CreationStage.Stages.Time;
        _repeatReminderRepository.UpdateReminder(reminder);

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

        var reminder = await _repeatReminderRepository.GetReminderById(stage.ReminderId);
        reminder.ReminderTime = resultTime;
        stage.StageType = CreationStage.Stages.Day;
        _repeatReminderRepository.UpdateReminder(reminder);

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

        if (!isParsed || resultNumberDay >= 7)
        {
            await _botClient.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: "Ошибка конвертации даты, попробуйте снова:",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);

            return;
        }

        DayOfWeek dayOfWeek = (DayOfWeek)resultNumberDay;
        var reminder = await _repeatReminderRepository.GetReminderById(stage.ReminderId);
        if (reminder.CheckAvailableDay(dayOfWeek))
        {
            reminder.ReminderDays.Add(dayOfWeek);
            _repeatReminderRepository.UpdateReminder(reminder);

            await _botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"Выбран: {CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetDayName(dayOfWeek)}",
                cancellationToken: cancellationToken);
        }
        else
        {
            reminder.ReminderDays.Remove(dayOfWeek);
            _repeatReminderRepository.UpdateReminder(reminder);

            await _botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"Исключен: {CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetDayName(dayOfWeek)}",
                cancellationToken: cancellationToken);
        }
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

    /// <summary>
    /// Вывести пользователю все доступные команды
    /// </summary>
    private async Task WriteCommands(ChatId chatId, CancellationToken cancellationToken)
    {
        const string usage = "Доступные команды:\n" +
                             "/create_repeat_reminder - создать повторяющееся напоминание\n" +
                             "/create_reminder - создать разовое напоминание(НЕДОСТУПНО)\n" +
                             "/list_reminder   - список всех напоминаний";

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: usage,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }
    
    private async Task WriteHelpMessage(ChatId chatId, CancellationToken cancellationToken)
    {
        const string usage = "Гайд:\n";

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: usage,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken);
    }
}