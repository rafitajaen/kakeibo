using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kakeibo.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_categories_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "id", "created_at", "deleted_at", "name", "updated_at", "user_id" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null, "Housing", NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null },
                    { new Guid("10000000-0000-0000-0000-000000000002"), NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null, "Transportation", NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null },
                    { new Guid("10000000-0000-0000-0000-000000000003"), NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null, "Food & Dining", NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null },
                    { new Guid("10000000-0000-0000-0000-000000000004"), NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null, "Health & Wellness", NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null },
                    { new Guid("10000000-0000-0000-0000-000000000005"), NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null, "Entertainment & Leisure", NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null },
                    { new Guid("10000000-0000-0000-0000-000000000006"), NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null, "Shopping & Personal", NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null },
                    { new Guid("10000000-0000-0000-0000-000000000007"), NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null, "Education", NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null },
                    { new Guid("10000000-0000-0000-0000-000000000008"), NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null, "Subscriptions & Bills", NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null },
                    { new Guid("10000000-0000-0000-0000-000000000009"), NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null, "Savings & Investments", NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null },
                    { new Guid("10000000-0000-0000-0000-00000000000a"), NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null, "Debt & Loans", NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null },
                    { new Guid("10000000-0000-0000-0000-00000000000b"), NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null, "Gifts & Donations", NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null },
                    { new Guid("10000000-0000-0000-0000-00000000000c"), NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null, "Other", NodaTime.Instant.FromUnixTimeTicks(17672256000000000L), null }
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_user_id",
                table: "categories",
                column: "user_id");

            // Partial unique index: enforces unique (name, user_id) only for non-archived categories.
            // This allows re-using a name after archiving a category.
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX uq_categories_name_user ON categories(name, user_id) " +
                "WHERE deleted_at IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS uq_categories_name_user;");
            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
