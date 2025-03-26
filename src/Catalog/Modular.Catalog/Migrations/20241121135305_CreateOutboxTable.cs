using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modular.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class CreateOutboxTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Products",
                schema: "Catalogs",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Id",
                schema: "Catalogs",
                table: "Products");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Products",
                schema: "Catalogs",
                table: "Products",
                column: "Sku");

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "Catalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.Sql(
                 @"CREATE INDEX ""IX_OutboxMessages_ProcessedOnUtc"" ON ""Catalogs"".""OutboxMessages"" (""ProcessedOnUtc"") WHERE ""ProcessedOnUtc"" IS NOT NULL;");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "Catalogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Products",
                schema: "Catalogs",
                table: "Products");
        }
    }
}
