using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Alphav002 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "YAxis",
                table: "World",
                newName: "Width");

            migrationBuilder.RenameColumn(
                name: "XAxis",
                table: "World",
                newName: "MapSeed");

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "World",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "WorldMapObjects",
                columns: table => new
                {
                    WorldId = table.Column<Guid>(type: "TEXT", nullable: false),
                    X = table.Column<short>(type: "INTEGER", nullable: false),
                    Y = table.Column<short>(type: "INTEGER", nullable: false),
                    Type = table.Column<byte>(type: "INTEGER", nullable: false),
                    ReferenceEntityId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorldMapObjects", x => new { x.WorldId, x.X, x.Y });
                    table.ForeignKey(
                        name: "FK_WorldMapObjects_World_WorldId",
                        column: x => x.WorldId,
                        principalTable: "World",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorldMapObjects");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "World");

            migrationBuilder.RenameColumn(
                name: "Width",
                table: "World",
                newName: "YAxis");

            migrationBuilder.RenameColumn(
                name: "MapSeed",
                table: "World",
                newName: "XAxis");
        }
    }
}
