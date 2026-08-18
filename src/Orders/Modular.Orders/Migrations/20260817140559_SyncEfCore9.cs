using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modular.Orders.Migrations
{
    /// <inheritdoc />
    public partial class SyncEfCore9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CanceledDate",
                schema: "Orders",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ShippedDate",
                schema: "Orders",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedDate",
                schema: "Orders",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                schema: "Orders",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                schema: "Orders",
                table: "OrderItems",
                column: "ProductId",
                principalSchema: "Orders",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                schema: "Orders",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ProductId",
                schema: "Orders",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "CanceledDate",
                schema: "Orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippedDate",
                schema: "Orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SubmittedDate",
                schema: "Orders",
                table: "Orders");
        }
    }
}
