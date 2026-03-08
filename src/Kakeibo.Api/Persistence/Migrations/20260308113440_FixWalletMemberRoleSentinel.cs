using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kakeibo.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixWalletMemberRoleSentinel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "role",
                table: "wallet_members",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Guest");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "role",
                table: "wallet_members",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Guest",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
