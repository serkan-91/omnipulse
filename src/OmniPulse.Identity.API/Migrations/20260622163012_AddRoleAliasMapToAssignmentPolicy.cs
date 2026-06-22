using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniPulse.Identity.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleAliasMapToAssignmentPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RoleAliasMapJson",
                table: "AssignmentPolicies",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoleAliasMapJson",
                table: "AssignmentPolicies");
        }
    }
}
