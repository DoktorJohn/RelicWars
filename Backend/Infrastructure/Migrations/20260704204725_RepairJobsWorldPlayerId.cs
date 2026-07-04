using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RepairJobsWorldPlayerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'dbo.Jobs', N'UserId') IS NOT NULL
                   AND COL_LENGTH(N'dbo.Jobs', N'WorldPlayerId') IS NULL
                BEGIN
                    EXEC sp_rename N'[dbo].[Jobs].[UserId]', N'WorldPlayerId', N'COLUMN';
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'dbo.Jobs', N'WorldPlayerId') IS NOT NULL
                   AND COL_LENGTH(N'dbo.Jobs', N'UserId') IS NULL
                BEGIN
                    EXEC sp_rename N'[dbo].[Jobs].[WorldPlayerId]', N'UserId', N'COLUMN';
                END
                """);
        }
    }
}
