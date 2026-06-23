using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniPulse.IoT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryPartitioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing Telemetry table
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"Telemetry\" CASCADE;");

            // Recreate Telemetry table as range-partitioned by Timestamp
            migrationBuilder.Sql(@"
                CREATE TABLE ""Telemetry"" (
                    ""Id"" uuid NOT NULL,
                    ""TenantId"" uuid NOT NULL,
                    ""DeviceId"" uuid NOT NULL,
                    ""Temperature"" double precision NOT NULL,
                    ""Pressure"" double precision NOT NULL,
                    ""Timestamp"" timestamp with time zone NOT NULL,
                    ""CreatedAtUtc"" timestamp with time zone NOT NULL,
                    ""CreatedBy"" character varying(100),
                    ""LastModifiedAtUtc"" timestamp with time zone,
                    ""LastModifiedBy"" character varying(100),
                    CONSTRAINT ""PK_Telemetry"" PRIMARY KEY (""Id"", ""Timestamp"")
                ) PARTITION BY RANGE (""Timestamp"");
            ");

            // Recreate DeviceId Index
            migrationBuilder.Sql("CREATE INDEX \"IX_Telemetry_DeviceId\" ON \"Telemetry\" (\"DeviceId\");");

            // Recreate Foreign Key to Devices
            migrationBuilder.Sql(@"
                ALTER TABLE ""Telemetry"" 
                ADD CONSTRAINT ""FK_Telemetry_Devices_DeviceId"" 
                FOREIGN KEY (""DeviceId"") REFERENCES ""Devices"" (""Id"") 
                ON DELETE CASCADE;
            ");

            // Create initial monthly partitions for 2026 and 2027
            for (int year = 2026; year <= 2027; year++)
            {
                for (int month = 1; month <= 12; month++)
                {
                    var partitionName = $"Telemetry_y{year}m{month:D2}";
                    var startDate = $"{year}-{month:D2}-01 00:00:00Z";
                    var nextYear = month == 12 ? year + 1 : year;
                    var nextMonth = month == 12 ? 1 : month + 1;
                    var endDate = $"{nextYear}-{nextMonth:D2}-01 00:00:00Z";

                    migrationBuilder.Sql($@"
                        CREATE TABLE IF NOT EXISTS ""{partitionName}"" PARTITION OF ""Telemetry""
                        FOR VALUES FROM ('{startDate}') TO ('{endDate}');
                    ");
                }
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop Telemetry table (and all its partitions)
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"Telemetry\" CASCADE;");

            // Recreate standard Telemetry table without partitioning
            migrationBuilder.Sql(@"
                CREATE TABLE ""Telemetry"" (
                    ""Id"" uuid NOT NULL,
                    ""TenantId"" uuid NOT NULL,
                    ""DeviceId"" uuid NOT NULL,
                    ""Temperature"" double precision NOT NULL,
                    ""Pressure"" double precision NOT NULL,
                    ""Timestamp"" timestamp with time zone NOT NULL,
                    ""CreatedAtUtc"" timestamp with time zone NOT NULL,
                    ""CreatedBy"" character varying(100),
                    ""LastModifiedAtUtc"" timestamp with time zone,
                    ""LastModifiedBy"" character varying(100),
                    CONSTRAINT ""PK_Telemetry"" PRIMARY KEY (""Id"")
                );
            ");

            // Recreate DeviceId Index
            migrationBuilder.Sql("CREATE INDEX \"IX_Telemetry_DeviceId\" ON \"Telemetry\" (\"DeviceId\");");

            // Recreate Foreign Key to Devices
            migrationBuilder.Sql(@"
                ALTER TABLE ""Telemetry"" 
                ADD CONSTRAINT ""FK_Telemetry_Devices_DeviceId"" 
                FOREIGN KEY (""DeviceId"") REFERENCES ""Devices"" (""Id"") 
                ON DELETE CASCADE;
            ");
        }
    }
}
