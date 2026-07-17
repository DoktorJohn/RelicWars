using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RepairConversationLegacyParticipantColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[Conversations]', N'U') IS NOT NULL
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM sys.foreign_keys
                        WHERE name = N'FK_Conversations_WorldPlayers_Participant1Id'
                          AND parent_object_id = OBJECT_ID(N'[dbo].[Conversations]'))
                    BEGIN
                        ALTER TABLE [dbo].[Conversations]
                        DROP CONSTRAINT [FK_Conversations_WorldPlayers_Participant1Id];
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM sys.foreign_keys
                        WHERE name = N'FK_Conversations_WorldPlayers_Participant2Id'
                          AND parent_object_id = OBJECT_ID(N'[dbo].[Conversations]'))
                    BEGIN
                        ALTER TABLE [dbo].[Conversations]
                        DROP CONSTRAINT [FK_Conversations_WorldPlayers_Participant2Id];
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE name = N'IX_Conversations_Participant1Id'
                          AND object_id = OBJECT_ID(N'[dbo].[Conversations]'))
                    BEGIN
                        DROP INDEX [IX_Conversations_Participant1Id]
                        ON [dbo].[Conversations];
                    END;

                    IF EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE name = N'IX_Conversations_Participant2Id'
                          AND object_id = OBJECT_ID(N'[dbo].[Conversations]'))
                    BEGIN
                        DROP INDEX [IX_Conversations_Participant2Id]
                        ON [dbo].[Conversations];
                    END;

                    IF COL_LENGTH(N'dbo.Conversations', N'Participant1Id') IS NOT NULL
                    BEGIN
                        ALTER TABLE [dbo].[Conversations]
                        DROP COLUMN [Participant1Id];
                    END;

                    IF COL_LENGTH(N'dbo.Conversations', N'Participant2Id') IS NOT NULL
                    BEGIN
                        ALTER TABLE [dbo].[Conversations]
                        DROP COLUMN [Participant2Id];
                    END;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Participant1Id",
                table: "Conversations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "Participant2Id",
                table: "Conversations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.Sql(
                """
                UPDATE c
                SET Participant1Id = p.WorldPlayerId
                FROM Conversations c
                CROSS APPLY (
                    SELECT TOP (1) cp.WorldPlayerId
                    FROM ConversationParticipants cp
                    WHERE cp.ConversationId = c.Id
                    ORDER BY cp.JoinedAt, cp.Id
                ) p;

                UPDATE c
                SET Participant2Id = COALESCE(p.WorldPlayerId, c.Participant1Id)
                FROM Conversations c
                OUTER APPLY (
                    SELECT TOP (1) cp.WorldPlayerId
                    FROM ConversationParticipants cp
                    WHERE cp.ConversationId = c.Id
                      AND cp.WorldPlayerId <> c.Participant1Id
                    ORDER BY cp.JoinedAt, cp.Id
                ) p;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_Participant1Id",
                table: "Conversations",
                column: "Participant1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_Participant2Id",
                table: "Conversations",
                column: "Participant2Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_WorldPlayers_Participant1Id",
                table: "Conversations",
                column: "Participant1Id",
                principalTable: "WorldPlayers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_WorldPlayers_Participant2Id",
                table: "Conversations",
                column: "Participant2Id",
                principalTable: "WorldPlayers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

        }
    }
}
