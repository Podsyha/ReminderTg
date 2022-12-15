using Microsoft.EntityFrameworkCore;
using ReminderTg.Infrastructure.Context;
using ReminderTg.Infrastructure.Models;

namespace ReminderTg.Infrastructure.Repositories;

public sealed class ReminderRepository : DbContextRepository, IReminderRepository
{
    public ReminderRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task AddReminder(ReminderModel model)
        => await AddModelAsync(model);

    public async void RemoveReminder(ReminderModel model)
    {
        await RemoveModel(model);
    }

    public async void UpdateReminder(ReminderModel model)
    {
        await UpdateModel(model);
    }

    public async Task<IList<ReminderModel>> GetAllUserReminders(long userId)
        => await DbContext.Reminder.Where(x => x.UserId == userId && x.IsSave).ToListAsync();

    public async Task<ReminderModel> GetReminderById(Guid reminderId)
        => await DbContext.Reminder.FirstOrDefaultAsync(x => x.Id == reminderId);
}