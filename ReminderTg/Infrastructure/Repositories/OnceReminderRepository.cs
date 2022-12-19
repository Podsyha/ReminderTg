using Microsoft.EntityFrameworkCore;
using ReminderTg.Infrastructure.Context;
using ReminderTg.Infrastructure.Models;

namespace ReminderTg.Infrastructure.Repositories;

public sealed class OnceReminderRepository : DbContextRepository, IOnceReminderRepository
{
    public OnceReminderRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task AddReminder(OnceReminderModel model)
        => await AddModelAsync(model);

    public async void RemoveReminder(OnceReminderModel model)
    {
        await RemoveModel(model);
    }

    public async void UpdateReminder(OnceReminderModel model)
    {
        await UpdateModel(model);
    }

    public async Task<IList<OnceReminderModel>> GetAllUserReminders(long userId)
        => await DbContext.OnceReminder.Where(x => x.UserId == userId && x.IsSave).ToListAsync();

    public async Task<OnceReminderModel> GetReminderById(Guid reminderId)
        => await DbContext.OnceReminder.FirstOrDefaultAsync(x => x.Id == reminderId);
}