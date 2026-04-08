using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shoplists.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniquePositionPerShoplist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShoplistItem_ShoplistId",
                table: "ShoplistItem");

            migrationBuilder.CreateIndex(
                name: "IX_ShoplistItem_ShoplistId_Position",
                table: "ShoplistItem",
                columns: new[] { "ShoplistId", "Position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShoplistItem_ShoplistId_Position",
                table: "ShoplistItem");

            migrationBuilder.CreateIndex(
                name: "IX_ShoplistItem_ShoplistId",
                table: "ShoplistItem",
                column: "ShoplistId");
        }
    }
}
