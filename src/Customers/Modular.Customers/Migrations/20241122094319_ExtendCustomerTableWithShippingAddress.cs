using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modular.Customers.Migrations
{
    /// <inheritdoc />
    public partial class ExtendCustomerTableWithShippingAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShippingCity",
                schema: "Users",
                table: "Customers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingState",
                schema: "Users",
                table: "Customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingStreet",
                schema: "Users",
                table: "Customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingZip",
                schema: "Users",
                table: "Customers",
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippingCity",
                schema: "Users",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ShippingState",
                schema: "Users",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ShippingStreet",
                schema: "Users",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ShippingZip",
                schema: "Users",
                table: "Customers");
        }
    }
}
