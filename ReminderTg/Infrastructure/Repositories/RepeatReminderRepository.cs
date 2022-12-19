using Microsoft.EntityFrameworkCore;
using ReminderTg.Infrastructure.Context;
using ReminderTg.Infrastructure.Models;

namespace ReminderTg.Infrastructure.Repositories;

public sealed class RepeatReminderRepository : DbContextRepository, IRepeatReminderRepository
{
    public RepeatReminderRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task AddReminder(RepeatReminderModel model)
        => await AddModelAsync(model);

    public async void RemoveReminder(RepeatReminderModel model)
    {
        await RemoveModel(model);
    }

    public async void UpdateReminder(RepeatReminderModel model)
    {
        await UpdateModel(model);
    }

    public async Task<IList<RepeatReminderModel>> GetAllUserReminders(long userId)
        => await DbContext.RepeatReminder.Where(x => x.UserId == userId && x.IsSave).ToListAsync();

    public async Task<RepeatReminderModel> GetReminderById(Guid reminderId)
        => await DbContext.RepeatReminder.FirstOrDefaultAsync(x => x.Id == reminderId);
}