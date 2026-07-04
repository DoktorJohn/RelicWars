using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class _002a : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConversationParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorldPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateLastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationParticipants_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConversationParticipants_WorldPlayers_WorldPlayerId",
                        column: x => x.WorldPlayerId,
                        principalTable: "WorldPlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipants_ConversationId_WorldPlayerId",
                table: "ConversationParticipants",
                columns: new[] { "ConversationId", "WorldPlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipants_WorldPlayerId",
                table: "ConversationParticipants",
                column: "WorldPlayerId");

            migrationBuilder.Sql(@"
INSERT INTO ConversationParticipants (Id, ConversationId, WorldPlayerId, JoinedAt, LastReadAt, DateCreated, DateLastModified, IsDeleted)
SELECT NEWID(), Id, Participant1Id, LastMessageDate, LastMessageDate, SYSUTCDATETIME(), SYSUTCDATETIME(), 0
FROM Conversations
UNION ALL
SELECT NEWID(), Id, Participant2Id, LastMessageDate, NULL, SYSUTCDATETIME(), SYSUTCDATETIME(), 0
FROM Conversations
WHERE Participant2Id <> Participant1Id
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationParticipants");
        }
    }
}
