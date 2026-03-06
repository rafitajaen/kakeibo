using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kakeibo.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayPreferencesAndCategoryCustomization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "currency_decimal_separator",
                table: "users",
                type: "character varying(1)",
                maxLength: 1,
                nullable: false,
                defaultValue: ".");

            migrationBuilder.AddColumn<string>(
                name: "currency_display",
                table: "users",
                type: "character varying(6)",
                maxLength: 6,
                nullable: false,
                defaultValue: "symbol");

            migrationBuilder.AddColumn<string>(
                name: "currency_group_separator",
                table: "users",
                type: "character varying(1)",
                maxLength: 1,
                nullable: false,
                defaultValue: ",");

            migrationBuilder.AddColumn<string>(
                name: "currency_symbol_position",
                table: "users",
                type: "character varying(6)",
                maxLength: 6,
                nullable: false,
                defaultValue: "before");

            migrationBuilder.AddColumn<int>(
                name: "month_start_day",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "week_start_day",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "background_color",
                table: "categories",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "icon",
                table: "categories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "text_color",
                table: "categories",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "background_color", "icon", "text_color" },
                values: new object[] { "#EFF6FF", "Home", "#1D4ED8" });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "background_color", "icon", "text_color" },
                values: new object[] { "#F0FDF4", "Car", "#15803D" });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "background_color", "icon", "text_color" },
                values: new object[] { "#FFF7ED", "UtensilsCrossed", "#C2410C" });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "background_color", "icon", "text_color" },
                values: new object[] { "#FFF1F2", "Heart", "#BE123C" });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "background_color", "icon", "text_color" },
                values: new object[] { "#FAF5FF", "Tv", "#7E22CE" });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "background_color", "icon", "text_color" },
                values: new object[] { "#FDF2F8", "ShoppingCart", "#9D174D" });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "background_color", "icon", "text_color" },
                values: new object[] { "#EEF2FF", "BookOpen", "#3730A3" });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                columns: new[] { "background_color", "icon", "text_color" },
                values: new object[] { "#F0FDFA", "Repeat", "#0F766E" });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                columns: new[] { "background_color", "icon", "text_color" },
                values: new object[] { "#ECFDF5", "PiggyBank", "#065F46" });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000a"),
                columns: new[] { "background_color", "icon", "text_color" },
                values: new object[] { "#FEFCE8", "CreditCard", "#854D0E" });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000b"),
                columns: new[] { "background_color", "icon", "text_color" },
                values: new object[] { "#FFF1F2", "Gift", "#9F1239" });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000c"),
                columns: new[] { "background_color", "icon", "text_color" },
                values: new object[] { "#F9FAFB", "MoreHorizontal", "#374151" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "currency_decimal_separator",
                table: "users");

            migrationBuilder.DropColumn(
                name: "currency_display",
                table: "users");

            migrationBuilder.DropColumn(
                name: "currency_group_separator",
                table: "users");

            migrationBuilder.DropColumn(
                name: "currency_symbol_position",
                table: "users");

            migrationBuilder.DropColumn(
                name: "month_start_day",
                table: "users");

            migrationBuilder.DropColumn(
                name: "week_start_day",
                table: "users");

            migrationBuilder.DropColumn(
                name: "background_color",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "icon",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "text_color",
                table: "categories");
        }
    }
}
