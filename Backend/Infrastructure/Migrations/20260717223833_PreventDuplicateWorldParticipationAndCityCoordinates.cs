using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PreventDuplicateWorldParticipationAndCityCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorldPlayers_PlayerProfileId",
                table: "WorldPlayers");

            migrationBuilder.DropIndex(
                name: "IX_Cities_WorldId",
                table: "Cities");

            migrationBuilder.DropIndex(
                name: "IX_City_Coordinates",
                table: "Cities");

            migrationBuilder.CreateIndex(
                name: "UX_WorldPlayers_Profile_World",
                table: "WorldPlayers",
                columns: new[] { "PlayerProfileId", "WorldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_WorldMapObjects_Type_Coordinates",
                table: "WorldMapObjects",
                columns: new[] { "WorldId", "X", "Y", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Cities_World_Coordinates",
                table: "Cities",
                columns: new[] { "WorldId", "X", "Y" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_WorldPlayers_Profile_World",
                table: "WorldPlayers");

            migrationBuilder.DropIndex(
                name: "UX_WorldMapObjects_Type_Coordinates",
                table: "WorldMapObjects");

            migrationBuilder.DropIndex(
                name: "UX_Cities_World_Coordinates",
                table: "Cities");

            migrationBuilder.CreateIndex(
                name: "IX_WorldPlayers_PlayerProfileId",
                table: "WorldPlayers",
                column: "PlayerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_WorldId",
                table: "Cities",
                column: "WorldId");

            migrationBuilder.CreateIndex(
                name: "IX_City_Coordinates",
                table: "Cities",
                columns: new[] { "X", "Y" });
        }
    }
}
