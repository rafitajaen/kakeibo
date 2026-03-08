using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kakeibo.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_platform_policies",
                table: "platform_policies");

            migrationBuilder.RenameTable(
                name: "platform_policies",
                newName: "platform_policy");

            migrationBuilder.AddPrimaryKey(
                name: "pk_platform_policy",
                table: "platform_policy",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_platform_policy",
                table: "platform_policy");

            migrationBuilder.RenameTable(
                name: "platform_policy",
                newName: "platform_policies");

            migrationBuilder.AddPrimaryKey(
                name: "pk_platform_policies",
                table: "platform_policies",
                column: "id");
        }
    }
}
