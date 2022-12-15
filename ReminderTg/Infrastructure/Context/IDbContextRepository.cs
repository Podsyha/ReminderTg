namespace ReminderTg.Infrastructure.Context;

/// <summary>
/// Абстракция репозитория контекста
/// </summary>
public interface IDbContextRepository
{
    /// <summary>
    /// Добавить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    public Task AddModelAsync<T>(T model) where T : class;
    /// <summary>
    /// Удалить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    public void RemoveModel<T>(T model) where T : class;
    /// <summary>
    /// Обновить модель
    /// </summary>
    /// <param name="model">Сущность бд</param>
    public void UpdateModel<T>(T model) where T : class;
}