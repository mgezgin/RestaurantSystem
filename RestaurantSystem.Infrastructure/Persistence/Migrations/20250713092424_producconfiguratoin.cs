using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class producconfiguratoin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_menu_items_Products_ProductId1",
                table: "menu_items");

            migrationBuilder.DropForeignKey(
                name: "FK_product_categories_Products_ProductId1",
                table: "product_categories");

            migrationBuilder.DropForeignKey(
                name: "FK_product_images_Products_ProductId1",
                table: "product_images");

            migrationBuilder.DropForeignKey(
                name: "FK_product_variations_Products_ProductId1",
                table: "product_variations");

            migrationBuilder.DropIndex(
                name: "IX_product_variations_ProductId1",
                table: "product_variations");

            migrationBuilder.DropIndex(
                name: "IX_product_images_ProductId1",
                table: "product_images");

            migrationBuilder.DropIndex(
                name: "IX_product_categories_ProductId1",
                table: "product_categories");

            migrationBuilder.DropIndex(
                name: "IX_menu_items_ProductId1",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "product_variations");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "product_categories");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "menu_items");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductId1",
                table: "product_variations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId1",
                table: "product_images",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId1",
                table: "product_categories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId1",
                table: "menu_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_product_variations_ProductId1",
                table: "product_variations",
                column: "ProductId1");

            migrationBuilder.CreateIndex(
                name: "IX_product_images_ProductId1",
                table: "product_images",
                column: "ProductId1");

            migrationBuilder.CreateIndex(
                name: "IX_product_categories_ProductId1",
                table: "product_categories",
                column: "ProductId1");

            migrationBuilder.CreateIndex(
                name: "IX_menu_items_ProductId1",
                table: "menu_items",
                column: "ProductId1");

            migrationBuilder.AddForeignKey(
                name: "FK_menu_items_Products_ProductId1",
                table: "menu_items",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_product_categories_Products_ProductId1",
                table: "product_categories",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_product_images_Products_ProductId1",
                table: "product_images",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_product_variations_Products_ProductId1",
                table: "product_variations",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
