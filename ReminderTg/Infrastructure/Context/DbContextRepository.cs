namespace ReminderTg.Infrastructure.Context;

/// <summary>
/// Репозиторий контекста
/// </summary>
public class DbContextRepository
{
    protected DbContextRepository(AppDbContext dbContext)
    {
        DbContext = dbContext;
    }

    protected readonly AppDbContext DbContext;

    /// <summary>
    /// Добавить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    protected async Task AddModelAsync<T>(T model) where T : class
    {
        await DbContext.AddAsync(model);
        await SaveChangesAsync();
    }

    /// <summary>
    /// Удалить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    protected async Task RemoveModel<T>(T model) where T : class
    {
        DbContext.Remove(model);
        await SaveChangesAsync();
    }

    /// <summary>
    /// Обновить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    protected async Task UpdateModel<T>(T model) where T : class
    {
        DbContext.Update(model);
        await SaveChangesAsync();
    }

    private async Task SaveChangesAsync() => await DbContext.SaveChangesAsync();
}