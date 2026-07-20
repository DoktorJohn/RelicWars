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
            migrationBuilder.Sql(
                """
                IF EXISTS
                (
                    SELECT 1
                    FROM WorldPlayers
                    GROUP BY PlayerProfileId, WorldId
                    HAVING COUNT_BIG(*) > 1
                )
                    THROW 51001, 'Cannot add UX_WorldPlayers_Profile_World: duplicate PlayerProfileId/WorldId participations remain. Inspect and resolve them before retrying the migration.', 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM Cities
                    GROUP BY WorldId, X, Y
                    HAVING COUNT_BIG(*) > 1
                )
                    THROW 51002, 'Cannot add UX_Cities_World_Coordinates: duplicate city coordinates remain. Inspect and resolve them before retrying the migration.', 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM WorldMapObjects
                    GROUP BY WorldId, X, Y, Type
                    HAVING COUNT_BIG(*) > 1
                )
                    THROW 51003, 'Cannot add UX_WorldMapObjects_World_Coordinates_Type: duplicate typed map-object coordinates remain. Inspect and resolve them before retrying the migration.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_WorldPlayers_PlayerProfileId",
                table: "WorldPlayers");

            migrationBuilder.DropIndex(
                name: "IX_WorldMapObject_Coordinates",
                table: "WorldMapObjects");

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
                name: "UX_WorldMapObjects_World_Coordinates_Type",
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
                name: "UX_WorldMapObjects_World_Coordinates_Type",
                table: "WorldMapObjects");

            migrationBuilder.DropIndex(
                name: "UX_Cities_World_Coordinates",
                table: "Cities");

            migrationBuilder.CreateIndex(
                name: "IX_WorldPlayers_PlayerProfileId",
                table: "WorldPlayers",
                column: "PlayerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_WorldMapObject_Coordinates",
                table: "WorldMapObjects",
                columns: new[] { "WorldId", "X", "Y" });

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
