using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllianceGeopolitics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Alliances",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<Guid>(
                name: "WorldId",
                table: "Alliances",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE alliances
                SET WorldId = members.WorldId
                FROM Alliances alliances
                CROSS APPLY (
                    SELECT TOP 1 WorldId
                    FROM WorldPlayers
                    WHERE AllianceId = alliances.Id
                    ORDER BY DateCreated
                ) members;

                IF EXISTS (SELECT 1 FROM Alliances WHERE WorldId IS NULL)
                    THROW 51000, 'Cannot assign a world to an alliance without members.', 1;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "WorldId",
                table: "Alliances",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AllianceRelations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllianceIdA = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllianceIdB = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InitiatorAllianceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RespondingAllianceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateLastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllianceRelations", x => x.Id);
                    table.CheckConstraint("CK_AllianceRelations_DifferentAlliances", "[AllianceIdA] <> [AllianceIdB]");
                    table.ForeignKey(
                        name: "FK_AllianceRelations_Alliances_AllianceIdA",
                        column: x => x.AllianceIdA,
                        principalTable: "Alliances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllianceRelations_Alliances_AllianceIdB",
                        column: x => x.AllianceIdB,
                        principalTable: "Alliances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllianceRelations_World_WorldId",
                        column: x => x.WorldId,
                        principalTable: "World",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alliances_WorldId_Name",
                table: "Alliances",
                columns: new[] { "WorldId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_AllianceRelations_AllianceIdA_AllianceIdB_RelationType_Status",
                table: "AllianceRelations",
                columns: new[] { "AllianceIdA", "AllianceIdB", "RelationType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AllianceRelations_AllianceIdB",
                table: "AllianceRelations",
                column: "AllianceIdB");

            migrationBuilder.CreateIndex(
                name: "IX_AllianceRelations_WorldId",
                table: "AllianceRelations",
                column: "WorldId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alliances_World_WorldId",
                table: "Alliances",
                column: "WorldId",
                principalTable: "World",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alliances_World_WorldId",
                table: "Alliances");

            migrationBuilder.DropTable(
                name: "AllianceRelations");

            migrationBuilder.DropIndex(
                name: "IX_Alliances_WorldId_Name",
                table: "Alliances");

            migrationBuilder.DropColumn(
                name: "WorldId",
                table: "Alliances");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Alliances",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
