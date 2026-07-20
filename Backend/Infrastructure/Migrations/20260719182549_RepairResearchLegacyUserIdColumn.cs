using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RepairResearchLegacyUserIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'dbo.Researches', N'UserId') IS NOT NULL
                BEGIN
                    DECLARE @defaultConstraint sysname;

                    SELECT @defaultConstraint = dc.name
                    FROM sys.default_constraints AS dc
                    INNER JOIN sys.columns AS c
                        ON c.default_object_id = dc.object_id
                    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Researches')
                      AND c.name = N'UserId';

                    IF @defaultConstraint IS NOT NULL
                        EXEC(N'ALTER TABLE [dbo].[Researches] DROP CONSTRAINT [' + @defaultConstraint + N']');

                    ALTER TABLE [dbo].[Researches] DROP COLUMN [UserId];
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'dbo.Researches', N'UserId') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Researches]
                    ADD [UserId] uniqueidentifier NOT NULL
                        CONSTRAINT [DF_Researches_UserId] DEFAULT ('00000000-0000-0000-0000-000000000000') WITH VALUES;
                END
                """);
        }
    }
}
