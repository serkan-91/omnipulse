using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniPulse.Modules.IoTModule.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAlarmRulesAndEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlarmRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    MetricKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ThresholdValue = table.Column<double>(type: "double precision", nullable: false),
                    ComparisonOperator = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlarmRules_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlarmEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlarmRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggeredValue = table.Column<double>(type: "double precision", nullable: false),
                    ThresholdValue = table.Column<double>(type: "double precision", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TriggeredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlarmEvents_AlarmRules_AlarmRuleId",
                        column: x => x.AlarmRuleId,
                        principalTable: "AlarmRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlarmEvents_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlarmEvents_AlarmRuleId",
                table: "AlarmEvents",
                column: "AlarmRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AlarmEvents_DeviceId",
                table: "AlarmEvents",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AlarmEvents_TenantId",
                table: "AlarmEvents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AlarmRules_DeviceId",
                table: "AlarmRules",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AlarmRules_TenantId",
                table: "AlarmRules",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlarmEvents");

            migrationBuilder.DropTable(
                name: "AlarmRules");
        }
    }
}
