using Bikontrol.Application.DTOs.Audit;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;

namespace Bikontrol.Infrastructure.Services
{
    public class AuditLogService : IAuditLogService
    {
        private const int MaxLimit = 200;

        private readonly IAuditLogRepository _repository;
        private readonly ICurrentUserService _current;

        public AuditLogService(IAuditLogRepository repository, ICurrentUserService current)
        {
            _repository = repository;
            _current = current;
        }

        public async Task<IReadOnlyList<AuditLogDTO>> GetMyActivityAsync(int limit = 50)
        {
            var take = Math.Clamp(limit, 1, MaxLimit);
            var entries = await _repository.GetByUserIdAsync(_current.UserId, take);

            return entries.Select(e => new AuditLogDTO
            {
                Id = e.Id,
                EntityName = e.EntityName,
                EntityId = e.EntityId,
                Action = e.Action,
                Changes = e.Changes,
                CreatedAt = e.CreatedAt
            }).ToList();
        }
    }
}
