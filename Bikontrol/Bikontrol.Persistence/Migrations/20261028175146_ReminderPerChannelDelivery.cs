using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bikontrol.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReminderPerChannelDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailDeliveredAt",
                table: "reminder_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PushDeliveredAt",
                table: "reminder_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_reminder_logs_EmailDeliveredAt_CreatedAt",
                table: "reminder_logs",
                columns: new[] { "EmailDeliveredAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_reminder_logs_PushDeliveredAt_CreatedAt",
                table: "reminder_logs",
                columns: new[] { "PushDeliveredAt", "CreatedAt" });

            // Backfill: before this migration a log held a single delivery channel.
            migrationBuilder.Sql(
                @"UPDATE reminder_logs SET ""EmailDeliveredAt"" = ""DeliveredAt"" WHERE ""Channel"" = 'Email' AND ""DeliveredAt"" IS NOT NULL;");
            migrationBuilder.Sql(
                @"UPDATE reminder_logs SET ""PushDeliveredAt"" = ""DeliveredAt"" WHERE ""Channel"" = 'Push' AND ""DeliveredAt"" IS NOT NULL;");

            // The audit trail used to copy inline images/attachments (base64) into
            // every change; keep the entry but drop the payload.
            migrationBuilder.Sql(
                @"UPDATE audit_logs SET ""Changes"" = jsonb_set(""Changes"", '{Image}', '""[binary omitted]""')
                  WHERE ""EntityName"" = 'Motorcycle' AND ""Changes"" ? 'Image' AND length(""Changes""::text) > 1000;");
            migrationBuilder.Sql(
                @"UPDATE audit_logs SET ""Changes"" = jsonb_set(""Changes"", '{DataUrl}', '""[binary omitted]""')
                  WHERE ""EntityName"" = 'MaintenanceRecordAttachment' AND ""Changes"" ? 'DataUrl' AND length(""Changes""::text) > 1000;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_reminder_logs_EmailDeliveredAt_CreatedAt",
                table: "reminder_logs");

            migrationBuilder.DropIndex(
                name: "IX_reminder_logs_PushDeliveredAt_CreatedAt",
                table: "reminder_logs");

            migrationBuilder.DropColumn(
                name: "EmailDeliveredAt",
                table: "reminder_logs");

            migrationBuilder.DropColumn(
                name: "PushDeliveredAt",
                table: "reminder_logs");
        }
    }
}
