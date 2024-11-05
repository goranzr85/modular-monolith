using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modular.Customers.Migrations
{
    /// <inheritdoc />
    public partial class CreateIndexOnEmailAndPhoneColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                    CREATE UNIQUE INDEX "IX_Customer_Contact_Email"
                    ON "Users"."Customers" ("Email")
                    WHERE "Email" IS NOT NULL
                """);
            migrationBuilder.Sql("""
                    CREATE UNIQUE INDEX "IX_Customer_Contact_Phone"
                    ON "Users"."Customers" ("Phone")
                    WHERE "Phone" IS NOT NULL
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
              name: "IX_Customer_Contact_Email",
              table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customer_Contact_Phone",
                table: "Customers");

        }
    }
}
