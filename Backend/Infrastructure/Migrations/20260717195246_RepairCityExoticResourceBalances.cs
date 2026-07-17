using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RepairCityExoticResourceBalances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO CityExoticResources
                    (Id, DateCreated, DateLastModified, IsDeleted, CityId, ResourceType, Amount)
                SELECT
                    NEWID(),
                    GETUTCDATE(),
                    GETUTCDATE(),
                    0,
                    city.Id,
                    resource.ResourceType,
                    0
                FROM Cities AS city
                CROSS JOIN (VALUES (0), (1), (2), (3), (4), (5), (6), (7), (8), (9))
                    AS resource(ResourceType)
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM CityExoticResources AS existing
                    WHERE existing.CityId = city.Id
                      AND existing.ResourceType = resource.ResourceType
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Existing and repaired zero balances cannot be distinguished safely.
        }
    }
}
