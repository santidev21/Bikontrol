using System;

namespace Bikontrol.Application.Services
{
    /// <summary>
    /// Bounding for history listings: an optional client-driven limit with a
    /// hard ceiling. Absent/invalid still means "full history" (the original
    /// behaviour), so nothing breaks until the client opts in.
    /// </summary>
    public static class ListLimits
    {
        public const int MaxLimit = 500;
        public const int DefaultPageSize = 100;

        /// <summary>
        /// null/zero/negative means "no limit"; anything above the ceiling is
        /// clamped, so no explicit request can exceed it.
        /// </summary>
        public static int? Clamp(int? requested)
        {
            if (requested is null or <= 0)
                return null;

            return requested.Value > MaxLimit ? MaxLimit : requested;
        }
    }
}
