using ReminderTg.Infrastructure.Models;

namespace ReminderTg.Infrastructure.Repositories;

/// <summary>
/// Абстракция над хранением напоминаний
/// </summary>
public interface IRepeatReminderRepository
{
    /// <summary>
    /// Добавить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    public Task AddReminder(RepeatReminderModel model);
    /// <summary>
    /// Удалить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    public void RemoveReminder(RepeatReminderModel model);
    /// <summary>
    /// Обновить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    public void UpdateReminder(RepeatReminderModel model);
    /// <summary>
    /// Получить все напоминания пользователя
    /// </summary>
    /// <param name="userId">Id пользователя</param>
    public Task<IList<RepeatReminderModel>> GetAllUserReminders(long userId);
    /// <summary>
    /// Получить напоминание по ID
    /// </summary>
    /// <param name="reminderId">ID напоминания</param>
    public Task<RepeatReminderModel> GetReminderById(Guid reminderId);
}