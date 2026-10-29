using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikontrol.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeUserEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Lookups compare against the normalized form (trim + lower) so the
            // unique index on "Email" serves them directly. Rewrite legacy rows,
            // skipping any whose normalized form already belongs to another
            // account: those stay untouched for manual review (the integrity
            // audit reports them) instead of failing the deploy.
            migrationBuilder.Sql(@"
                UPDATE users u
                SET ""Email"" = lower(btrim(u.""Email""))
                WHERE u.""Email"" <> lower(btrim(u.""Email""))
                  AND NOT EXISTS (
                      SELECT 1 FROM users o
                      WHERE o.""Id"" <> u.""Id""
                        AND o.""Email"" = lower(btrim(u.""Email"")));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-only normalization: the original casing is not recoverable.
        }
    }
}
