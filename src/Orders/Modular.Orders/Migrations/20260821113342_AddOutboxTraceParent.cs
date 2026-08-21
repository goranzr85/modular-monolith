using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modular.Orders.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxTraceParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TraceParent",
                schema: "Orders",
                table: "OutboxMessages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TraceParent",
                schema: "Orders",
                table: "OutboxMessages");
        }
    }
}
