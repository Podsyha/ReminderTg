using ReminderTg.Infrastructure.Models;

namespace ReminderTg.Infrastructure.Repositories;

public interface IOnceReminderRepository
{
    /// <summary>
    /// Добавить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    public Task AddReminder(OnceReminderModel model);
    /// <summary>
    /// Удалить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    public void RemoveReminder(OnceReminderModel model);
    /// <summary>
    /// Обновить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    public void UpdateReminder(OnceReminderModel model);
    /// <summary>
    /// Получить все напоминания пользователя
    /// </summary>
    /// <param name="userId">Id пользователя</param>
    public Task<IList<OnceReminderModel>> GetAllUserReminders(long userId);
    /// <summary>
    /// Получить напоминание по ID
    /// </summary>
    /// <param name="reminderId">ID напоминания</param>
    public Task<OnceReminderModel> GetReminderById(Guid reminderId);
}