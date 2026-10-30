using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikontrol.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMotorcycleImageStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageKey",
                table: "Motorcycles",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageSizeBytes",
                table: "Motorcycles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ImageVersion",
                table: "Motorcycles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            // Photos still stored as data URLs keep living in the database: record the
            // decoded size (base64 payload length * 3/4) so the per-user quota counts them.
            migrationBuilder.Sql(@"
                UPDATE ""Motorcycles""
                SET ""ImageSizeBytes"" = ((length(""Image"") - position(',' in ""Image"")) * 3) / 4
                WHERE ""Image"" LIKE 'data:%' AND position(',' in ""Image"") > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageKey",
                table: "Motorcycles");

            migrationBuilder.DropColumn(
                name: "ImageSizeBytes",
                table: "Motorcycles");

            migrationBuilder.DropColumn(
                name: "ImageVersion",
                table: "Motorcycles");
        }
    }
}
