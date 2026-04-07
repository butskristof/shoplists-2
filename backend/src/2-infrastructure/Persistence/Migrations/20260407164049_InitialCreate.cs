using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shoplists.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shoplists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shoplists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShoplistItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    IsChecked = table.Column<bool>(type: "boolean", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    ShoplistId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoplistItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoplistItem_Shoplists_ShoplistId",
                        column: x => x.ShoplistId,
                        principalTable: "Shoplists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoplistItem_ShoplistId",
                table: "ShoplistItem",
                column: "ShoplistId");

            migrationBuilder.CreateIndex(
                name: "IX_Shoplists_OwnerId",
                table: "Shoplists",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShoplistItem");

            migrationBuilder.DropTable(
                name: "Shoplists");
        }
    }
}
