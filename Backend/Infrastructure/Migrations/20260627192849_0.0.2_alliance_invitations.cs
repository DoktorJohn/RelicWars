using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _002_alliance_invitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllianceInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllianceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvitedWorldPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvitedByWorldPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateLastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllianceInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllianceInvitations_Alliances_AllianceId",
                        column: x => x.AllianceId,
                        principalTable: "Alliances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AllianceInvitations_WorldPlayers_InvitedByWorldPlayerId",
                        column: x => x.InvitedByWorldPlayerId,
                        principalTable: "WorldPlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllianceInvitations_WorldPlayers_InvitedWorldPlayerId",
                        column: x => x.InvitedWorldPlayerId,
                        principalTable: "WorldPlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllianceInvitations_AllianceId_InvitedWorldPlayerId",
                table: "AllianceInvitations",
                columns: new[] { "AllianceId", "InvitedWorldPlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_AllianceInvitations_InvitedByWorldPlayerId",
                table: "AllianceInvitations",
                column: "InvitedByWorldPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_AllianceInvitations_InvitedWorldPlayerId",
                table: "AllianceInvitations",
                column: "InvitedWorldPlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllianceInvitations");
        }
    }
}
