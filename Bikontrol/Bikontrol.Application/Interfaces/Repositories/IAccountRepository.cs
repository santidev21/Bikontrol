namespace Bikontrol.Application.Interfaces.Repositories
{
    public interface IAccountRepository
    {
        /// <summary>
        /// Hard-deletes everything the user owns: attachments and records of their
        /// motorcycles, user maintenances, km history, the motorcycles themselves,
        /// reminder logs, push subscriptions and refresh tokens. The user row is
        /// left untouched (anonymized separately). Not persisted by SaveChanges —
        /// the deletes run immediately, so call it inside a transaction.
        /// </summary>
        Task PurgeUserDataAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
