using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class changeTableNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BasketItemSideItems");

            migrationBuilder.DropTable(
                name: "daily_menu_items");

            migrationBuilder.DropTable(
                name: "daily_menus");

            migrationBuilder.AlterColumn<Guid>(
                name: "product_id",
                table: "BasketItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "menu_id",
                table: "BasketItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "menus",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menus", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "menu_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    special_price = table.Column<decimal>(type: "numeric", nullable: true),
                    estimated_quantity = table.Column<int>(type: "integer", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductId1 = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_menu_items_Products_ProductId1",
                        column: x => x.ProductId1,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_menu_items_menus_menu_id",
                        column: x => x.menu_id,
                        principalTable: "menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_menu_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_menu_items_productvariations_product_variation_id",
                        column: x => x.product_variation_id,
                        principalTable: "product_variations",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_basket_items_menu_id",
                table: "BasketItems",
                column: "menu_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_menu_id",
                table: "menu_items",
                column: "menu_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_product_id",
                table: "menu_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_product_variation_id",
                table: "menu_items",
                column: "product_variation_id");

            migrationBuilder.CreateIndex(
                name: "IX_menu_items_ProductId1",
                table: "menu_items",
                column: "ProductId1");

            migrationBuilder.AddForeignKey(
                name: "fk_basket_items_menus_menu_id",
                table: "BasketItems",
                column: "menu_id",
                principalTable: "menus",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_basket_items_menus_menu_id",
                table: "BasketItems");

            migrationBuilder.DropTable(
                name: "menu_items");

            migrationBuilder.DropTable(
                name: "menus");

            migrationBuilder.DropIndex(
                name: "ix_basket_items_menu_id",
                table: "BasketItems");

            migrationBuilder.DropColumn(
                name: "menu_id",
                table: "BasketItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "product_id",
                table: "BasketItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "BasketItemSideItems",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    basket_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    side_item_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_basket_item_side_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_basket_item_side_items_basket_items_basket_item_id",
                        column: x => x.basket_item_id,
                        principalTable: "BasketItems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_basket_item_side_items_products_side_item_product_id",
                        column: x => x.side_item_product_id,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "daily_menus",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_menus", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "daily_menu_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    daily_menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId1 = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    estimated_quantity = table.Column<int>(type: "integer", nullable: true),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    special_price = table.Column<decimal>(type: "numeric", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_menu_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_daily_menu_items_Products_ProductId1",
                        column: x => x.ProductId1,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_daily_menu_items_daily_menus_daily_menu_id",
                        column: x => x.daily_menu_id,
                        principalTable: "daily_menus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_daily_menu_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "Products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_basket_item_side_items_basket_item_id",
                table: "BasketItemSideItems",
                column: "basket_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_basket_item_side_items_side_item_product_id",
                table: "BasketItemSideItems",
                column: "side_item_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_daily_menu_items_daily_menu_id",
                table: "daily_menu_items",
                column: "daily_menu_id");

            migrationBuilder.CreateIndex(
                name: "ix_daily_menu_items_product_id",
                table: "daily_menu_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_daily_menu_items_ProductId1",
                table: "daily_menu_items",
                column: "ProductId1");
        }
    }
}
