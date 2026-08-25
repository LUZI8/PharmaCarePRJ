using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PharmaCare.Data;

#nullable disable

namespace PharmaCare.Migrations
{
    [DbContext(typeof(DataDbContext))]
    [Migration("20260825053500_AddProductInventoryFields")]
    public partial class AddProductInventoryFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "SKU", table: "Product", type: "nvarchar(50)", maxLength: 50, nullable: true);
            migrationBuilder.AddColumn<string>(name: "Barcode", table: "Product", type: "nvarchar(64)", maxLength: 64, nullable: true);
            migrationBuilder.AddColumn<string>(name: "Manufacturer", table: "Product", type: "nvarchar(150)", maxLength: 150, nullable: true);
            migrationBuilder.AddColumn<int>(name: "ReorderLevel", table: "Product", type: "int", nullable: false, defaultValue: 10);

            migrationBuilder.CreateIndex(name: "IX_Product_Barcode", table: "Product", column: "Barcode", unique: true, filter: "[Barcode] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_Product_SKU", table: "Product", column: "SKU", unique: true, filter: "[SKU] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Product_Barcode", table: "Product");
            migrationBuilder.DropIndex(name: "IX_Product_SKU", table: "Product");
            migrationBuilder.DropColumn(name: "Barcode", table: "Product");
            migrationBuilder.DropColumn(name: "Manufacturer", table: "Product");
            migrationBuilder.DropColumn(name: "ReorderLevel", table: "Product");
            migrationBuilder.DropColumn(name: "SKU", table: "Product");
        }
    }
}