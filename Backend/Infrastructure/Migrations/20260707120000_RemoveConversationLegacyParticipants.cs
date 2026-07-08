using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveConversationLegacyParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_WorldPlayers_Participant1Id",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_WorldPlayers_Participant2Id",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_Participant1Id",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_Participant2Id",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Participant1Id",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Participant2Id",
                table: "Conversations");
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
                ) p
                WHERE c.Participant1Id = '00000000-0000-0000-0000-000000000000';

                UPDATE c
                SET Participant2Id = p.WorldPlayerId
                FROM Conversations c
                CROSS APPLY (
                    SELECT TOP (1) cp.WorldPlayerId
                    FROM ConversationParticipants cp
                    WHERE cp.ConversationId = c.Id
                      AND cp.WorldPlayerId <> c.Participant1Id
                    ORDER BY cp.JoinedAt, cp.Id
                ) p
                WHERE c.Participant2Id = '00000000-0000-0000-0000-000000000000';

                UPDATE Conversations
                SET Participant2Id = Participant1Id
                WHERE Participant2Id = '00000000-0000-0000-0000-000000000000'
                  AND Participant1Id <> '00000000-0000-0000-0000-000000000000';
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
