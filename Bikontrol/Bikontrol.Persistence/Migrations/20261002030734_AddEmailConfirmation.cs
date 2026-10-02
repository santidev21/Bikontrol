using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikontrol.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmationTokenExpires",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmationTokenHash",
                table: "users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            // Grandfather existing accounts: they predate email verification, so
            // mark them confirmed instead of locking them out after deploy.
            migrationBuilder.Sql(
                "UPDATE users SET \"EmailConfirmedAt\" = \"CreatedAt\" WHERE \"EmailConfirmedAt\" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenExpires",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenHash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmedAt",
                table: "users");
        }
    }
}
