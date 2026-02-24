using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Kakeibo.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletMembersAndInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inviter_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitee_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    expires_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    accepted_at = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invitations", x => x.id);
                    table.ForeignKey(
                        name: "fk_invitations_users_inviter_user_id",
                        column: x => x.inviter_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_invitations_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wallet_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_wallet_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_wallet_members_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_invitations_code",
                table: "invitations",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invitations_inviter_user_id",
                table: "invitations",
                column: "inviter_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_invitations_wallet_id",
                table: "invitations",
                column: "wallet_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_members_user_id",
                table: "wallet_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_members_wallet_id_user_id",
                table: "wallet_members",
                columns: new[] { "wallet_id", "user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invitations");

            migrationBuilder.DropTable(
                name: "wallet_members");
        }
    }
}
