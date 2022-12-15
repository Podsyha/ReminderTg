namespace ReminderTg.Infrastructure.Context;

/// <summary>
/// Репозиторий контекста
/// </summary>
public class DbContextRepository : AppDbContext
{
    public DbContextRepository(AppDbContext dbContext)
    {
        DbContext = dbContext;
    }

    protected readonly AppDbContext DbContext;

    /// <summary>
    /// Добавить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    public async Task AddModelAsync<T>(T model) where T : class
        => await DbContext.AddAsync(model);

    /// <summary>
    /// Удалить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    public void RemoveModel<T>(T model) where T : class
        => DbContext.Remove(model);

    /// <summary>
    /// Обновить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    public void UpdateModel<T>(T model) where T : class
        => DbContext.Update(model);
}