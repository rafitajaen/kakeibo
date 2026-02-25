using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Kakeibo.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringPatternsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recurring_patterns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    transaction_type = table.Column<string>(type: "text", nullable: false),
                    frequency = table.Column<string>(type: "text", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_wallet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_date = table.Column<LocalDate>(type: "date", nullable: false),
                    end_date = table.Column<LocalDate>(type: "date", nullable: true),
                    next_occurrence = table.Column<LocalDate>(type: "date", nullable: false),
                    last_generated_at = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recurring_patterns", x => x.id);
                    table.ForeignKey(
                        name: "fk_recurring_patterns_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recurring_patterns_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_recurring_patterns_wallets_destination_wallet_id",
                        column: x => x.destination_wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recurring_patterns_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_recurring_patterns_category_id",
                table: "recurring_patterns",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_recurring_patterns_destination_wallet_id",
                table: "recurring_patterns",
                column: "destination_wallet_id");

            migrationBuilder.CreateIndex(
                name: "ix_recurring_patterns_next_occurrence",
                table: "recurring_patterns",
                column: "next_occurrence");

            migrationBuilder.CreateIndex(
                name: "ix_recurring_patterns_user_id",
                table: "recurring_patterns",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_recurring_patterns_user_id_next_occurrence",
                table: "recurring_patterns",
                columns: new[] { "user_id", "next_occurrence" });

            migrationBuilder.CreateIndex(
                name: "ix_recurring_patterns_wallet_id",
                table: "recurring_patterns",
                column: "wallet_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recurring_patterns");
        }
    }
}
