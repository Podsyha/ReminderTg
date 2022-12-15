using Microsoft.EntityFrameworkCore;
using ReminderTg.Infrastructure.Context;
using ReminderTg.Infrastructure.Models;

namespace ReminderTg.Infrastructure.Repositories;

public sealed class ReminderRepository : DbContextRepository, IReminderRepository
{
    public ReminderRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task AddModelAsync(ReminderModel model)
        => await DbContext.AddAsync(model);

    public void RemoveModel(ReminderModel model)
    {
        DbContext.Remove(model);
    }

    public void UpdateModel(ReminderModel model)
    {
        DbContext.Update(model);
    }

    public async Task<IList<ReminderModel>> GetAllUserReminders(long userId)
        => await DbContext.Reminder.Where(x => x.UserId == userId).ToListAsync();

    public async Task<ReminderModel> GetReminderById(Guid reminderId)
        => await DbContext.Reminder.FirstOrDefaultAsync(x => x.Id == reminderId);
}