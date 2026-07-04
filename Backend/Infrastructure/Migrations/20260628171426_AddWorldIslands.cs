using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorldIslands : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorldIslands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CellX = table.Column<int>(type: "int", nullable: false),
                    CellY = table.Column<int>(type: "int", nullable: false),
                    CenterX = table.Column<int>(type: "int", nullable: false),
                    CenterY = table.Column<int>(type: "int", nullable: false),
                    Shape = table.Column<int>(type: "int", nullable: false),
                    MajorRadius = table.Column<float>(type: "real", nullable: false),
                    MinorRadius = table.Column<float>(type: "real", nullable: false),
                    RotationDegrees = table.Column<float>(type: "real", nullable: false),
                    EdgeRoughness = table.Column<float>(type: "real", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateLastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorldIslands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorldIslands_World_WorldId",
                        column: x => x.WorldId,
                        principalTable: "World",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorldIslands_World_Cell",
                table: "WorldIslands",
                columns: new[] { "WorldId", "CellX", "CellY" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorldIslands");
        }
    }
}
