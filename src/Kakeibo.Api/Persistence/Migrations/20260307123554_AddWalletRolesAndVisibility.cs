using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kakeibo.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletRolesAndVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add role column first (default Guest) so we can backfill before dropping owner_id
            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "wallet_members",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Guest");

            // 2. Backfill Owner role: any WalletMember whose user_id matches the wallet's owner_id
            migrationBuilder.Sql(@"
                UPDATE wallet_members wm
                SET role = 'Owner'
                FROM wallets w
                WHERE wm.wallet_id = w.id
                  AND wm.user_id = w.owner_id;");

            // 3. Now it's safe to drop the owner_id column and its constraints
            migrationBuilder.DropForeignKey(
                name: "fk_wallets_users_owner_id",
                table: "wallets");

            migrationBuilder.DropIndex(
                name: "ix_wallets_owner_id",
                table: "wallets");

            migrationBuilder.DropColumn(
                name: "owner_id",
                table: "wallets");

            // 4. Add visibility column with Private default
            migrationBuilder.AddColumn<string>(
                name: "visibility",
                table: "wallets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Private");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "visibility",
                table: "wallets");

            migrationBuilder.DropColumn(
                name: "role",
                table: "wallet_members");

            migrationBuilder.AddColumn<Guid>(
                name: "owner_id",
                table: "wallets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_wallets_owner_id",
                table: "wallets",
                column: "owner_id");

            migrationBuilder.AddForeignKey(
                name: "fk_wallets_users_owner_id",
                table: "wallets",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
