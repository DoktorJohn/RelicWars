using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddCityExoticResourcesAndIslandProgress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastExoticResourceUpdate",
                table: "Cities",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<double>(
                name: "CoinInvestment",
                table: "WorldIslandExoticResources",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MetalInvestment",
                table: "WorldIslandExoticResources",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "StoneInvestment",
                table: "WorldIslandExoticResources",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "WoodInvestment",
                table: "WorldIslandExoticResources",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "CityExoticResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateLastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityExoticResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CityExoticResources_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CityExoticResources_City_Type",
                table: "CityExoticResources",
                columns: new[] { "CityId", "ResourceType" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityExoticResources");

            migrationBuilder.DropColumn(
                name: "LastExoticResourceUpdate",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "CoinInvestment",
                table: "WorldIslandExoticResources");

            migrationBuilder.DropColumn(
                name: "MetalInvestment",
                table: "WorldIslandExoticResources");

            migrationBuilder.DropColumn(
                name: "StoneInvestment",
                table: "WorldIslandExoticResources");

            migrationBuilder.DropColumn(
                name: "WoodInvestment",
                table: "WorldIslandExoticResources");
        }
    }
}
