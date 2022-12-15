namespace ReminderTg.Infrastructure.Context;

/// <summary>
/// Репозиторий контекста
/// </summary>
public sealed class DbContextRepository : AppDbContext, IDbContextRepository
{
    public DbContextRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Добавить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    public async Task AddModelAsync<T>(T model) where T : class
        => await _dbContext.AddAsync(model);

    /// <summary>
    /// Удалить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    public void RemoveModel<T>(T model) where T : class
        => _dbContext.Remove(model);

    /// <summary>
    /// Обновить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    public void UpdateModel<T>(T model) where T : class
        => _dbContext.Update(model);
}