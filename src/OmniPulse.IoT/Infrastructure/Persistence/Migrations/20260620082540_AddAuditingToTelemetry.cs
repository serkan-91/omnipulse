using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniPulse.IoT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditingToTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Telemetry",
                newName: "CreatedAtUtc");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Telemetry",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAtUtc",
                table: "Telemetry",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "Telemetry",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Telemetry");

            migrationBuilder.DropColumn(
                name: "LastModifiedAtUtc",
                table: "Telemetry");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "Telemetry");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Telemetry",
                newName: "CreatedAt");
        }
    }
}
