using Bikontrol.Application.DTOs.Retention;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bikontrol.Application.Interfaces
{
    /// <summary>
    /// Daily data-retention purge. Refresh tokens rotate on every session
    /// refresh and reminder logs accumulate on every engine run, so both grow
    /// without bound unless dead rows are deleted; audit logs are kept forever
    /// unless a positive retention is configured.
    /// </summary>
    public interface IRetentionService
    {
        Task<RetentionRunResult> PurgeAsync(CancellationToken cancellationToken = default);
    }
}
