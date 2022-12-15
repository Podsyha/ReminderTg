using ReminderTg.Infrastructure.Models;

namespace ReminderTg.Infrastructure.Repositories;

/// <summary>
/// Абстракция над хранением напоминаний
/// </summary>
public interface IReminderRepository
{
    /// <summary>
    /// Добавить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    public Task AddReminder(ReminderModel model);
    /// <summary>
    /// Удалить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    public void RemoveReminder(ReminderModel model);
    /// <summary>
    /// Обновить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    public void UpdateReminder(ReminderModel model);
    /// <summary>
    /// Получить все напоминания пользователя
    /// </summary>
    /// <param name="userId">Id пользователя</param>
    public Task<IList<ReminderModel>> GetAllUserReminders(long userId);
    /// <summary>
    /// Получить напоминание по ID
    /// </summary>
    /// <param name="reminderId">ID напоминания</param>
    public Task<ReminderModel> GetReminderById(Guid reminderId);
}