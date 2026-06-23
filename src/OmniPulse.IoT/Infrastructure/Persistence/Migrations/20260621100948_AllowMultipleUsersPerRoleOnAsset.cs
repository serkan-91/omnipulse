using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniPulse.IoT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleUsersPerRoleOnAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetPermissions_AssetId_Role",
                table: "AssetPermissions");

            migrationBuilder.CreateIndex(
                name: "IX_AssetPermissions_AssetId_Role",
                table: "AssetPermissions",
                columns: new[] { "AssetId", "Role" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetPermissions_AssetId_Role",
                table: "AssetPermissions");

            migrationBuilder.CreateIndex(
                name: "IX_AssetPermissions_AssetId_Role",
                table: "AssetPermissions",
                columns: new[] { "AssetId", "Role" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }
    }
}
