using ReminderTg.Infrastructure.Models;

namespace ReminderTg.Infrastructure.Repositories;

/// <summary>
/// Абстракция над хранением напоминаний
/// </summary>
public interface IReminderRepository
{
    public Task<ReminderModel> Add();
    public Task<ReminderModel> Remove();
    public Task<ReminderModel> Update(ReminderModel model);
}