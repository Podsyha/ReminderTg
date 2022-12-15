using Microsoft.EntityFrameworkCore;
using ReminderTg.Infrastructure.Context;
using ReminderTg.Infrastructure.Models;

namespace ReminderTg.Infrastructure.Repositories;

public sealed class ReminderRepository : IReminderRepository
{
    public Task<ReminderModel> Add()
    {
        AppDbContext.ApiContext test = new();
        test.Reminder.
    }

    public Task<ReminderModel> Remove()
    {
        throw new NotImplementedException();
    }

    public Task<ReminderModel> Update(ReminderModel model)
    {
        throw new NotImplementedException();
    }
}