using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikontrol.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentObjectStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SizeBytes",
                table: "maintenance_record_attachments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "maintenance_record_attachments",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            // Existing rows keep their data URL in the database: record the decoded
            // size (base64 payload length * 3/4) so the per-user quota counts them.
            migrationBuilder.Sql(@"
                UPDATE maintenance_record_attachments
                SET ""SizeBytes"" = ((length(""DataUrl"") - position(',' in ""DataUrl"")) * 3) / 4
                WHERE ""DataUrl"" <> '' AND position(',' in ""DataUrl"") > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "maintenance_record_attachments");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "maintenance_record_attachments");
        }
    }
}
