using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniPulse.Modules.IoTModule.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssetTypeToString_RoleAliasing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Assets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleRole",
                table: "Assets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResponsibleRole",
                table: "Assets");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Assets",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }
    }
}
