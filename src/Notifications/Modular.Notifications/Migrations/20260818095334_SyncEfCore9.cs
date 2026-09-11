using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modular.Notifications.Migrations
{
    /// <inheritdoc />
    public partial class SyncEfCore9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No schema changes - this migration only syncs the EF Core model snapshot.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No schema changes - this migration only syncs the EF Core model snapshot.
        }
    }
}
