using ReminderTg.Infrastructure.Models;

namespace ReminderTg.Infrastructure.Repositories;

/// <summary>
/// Обёртка над репозиторием хранения состояние создания напоминаний в кеше памяти
/// </summary>
public interface ICreationStagesRepository
{
    /// <summary>
    /// Добавить состояние в кеш
    /// </summary>
    public void AddStage(CreationStage state);
    /// <summary>
    /// Получить состояние из кеша по ID
    /// </summary>
    public CreationStage GetStage(long stateId);
    /// <summary>
    /// Удалить состояние из кеша
    /// </summary>
    public void RemoveStage(CreationStage state);
}