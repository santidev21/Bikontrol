using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bikontrol.Application.Interfaces
{
    /// <summary>
    /// Ejecuta varias operaciones de escritura dentro de una única transacción
    /// de base de datos. Si alguna falla, todo se revierte (atomicidad).
    /// Si ya existe una transacción en curso, la acción se une a ella.
    /// </summary>
    public interface ITransactionManager
    {
        Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);
    }
}
