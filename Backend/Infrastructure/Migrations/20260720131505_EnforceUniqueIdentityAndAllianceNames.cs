using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueIdentityAndAllianceNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS
                (
                    SELECT 1
                    FROM PlayerProfiles
                    WHERE UserName IS NULL OR LTRIM(RTRIM(UserName)) = '' OR LEN(UserName) > 256
                )
                    THROW 51004, 'Cannot enforce player identity uniqueness: a PlayerProfiles username is missing, blank, or longer than 256 characters.', 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM PlayerProfiles
                    WHERE Email IS NULL OR LTRIM(RTRIM(Email)) = '' OR LEN(Email) > 256
                )
                    THROW 51005, 'Cannot enforce player identity uniqueness: a PlayerProfiles email is missing, blank, or longer than 256 characters.', 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM PlayerProfiles
                    GROUP BY UPPER(LTRIM(RTRIM(UserName)))
                    HAVING COUNT_BIG(*) > 1
                )
                    THROW 51006, 'Cannot add UX_PlayerProfiles_NormalizedUserName: case-insensitive duplicate usernames remain. Inspect and resolve them before retrying the migration.', 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM PlayerProfiles
                    GROUP BY UPPER(LTRIM(RTRIM(Email)))
                    HAVING COUNT_BIG(*) > 1
                )
                    THROW 51007, 'Cannot add UX_PlayerProfiles_NormalizedEmail: case-insensitive duplicate emails remain. Inspect and resolve them before retrying the migration.', 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM Alliances
                    WHERE LTRIM(RTRIM(Name)) = '' OR LEN(Name) > 20
                )
                    THROW 51008, 'Cannot enforce alliance name uniqueness: an alliance name is blank or longer than 20 characters.', 1;

                IF EXISTS
                (
                    SELECT 1
                    FROM Alliances
                    GROUP BY WorldId, UPPER(LTRIM(RTRIM(Name)))
                    HAVING COUNT_BIG(*) > 1
                )
                    THROW 51009, 'Cannot add UX_Alliances_World_NormalizedName: case-insensitive duplicate alliance names remain in a world. Inspect and resolve them before retrying the migration.', 1;

                UPDATE PlayerProfiles
                SET NormalizedUserName = UPPER(LTRIM(RTRIM(UserName))),
                    NormalizedEmail = UPPER(LTRIM(RTRIM(Email))),
                    SecurityStamp = COALESCE(SecurityStamp, CONVERT(nvarchar(36), NEWID())),
                    ConcurrencyStamp = COALESCE(ConcurrencyStamp, CONVERT(nvarchar(36), NEWID())),
                    LockoutEnabled = 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Alliances_WorldId_Name",
                table: "Alliances");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "PlayerProfiles",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedUserName",
                table: "PlayerProfiles",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                table: "PlayerProfiles",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "PlayerProfiles",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Alliances",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Alliances",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE Alliances
                SET NormalizedName = UPPER(LTRIM(RTRIM(Name)));
                """);

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_PlayerProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "PlayerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_PlayerProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "PlayerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_PlayerProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "PlayerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_PlayerProfiles_NormalizedEmail",
                table: "PlayerProfiles",
                column: "NormalizedEmail",
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_PlayerProfiles_NormalizedUserName",
                table: "PlayerProfiles",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Alliances_World_NormalizedName",
                table: "Alliances",
                columns: new[] { "WorldId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropIndex(
                name: "UX_PlayerProfiles_NormalizedEmail",
                table: "PlayerProfiles");

            migrationBuilder.DropIndex(
                name: "UX_PlayerProfiles_NormalizedUserName",
                table: "PlayerProfiles");

            migrationBuilder.DropIndex(
                name: "UX_Alliances_World_NormalizedName",
                table: "Alliances");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Alliances");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "PlayerProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedUserName",
                table: "PlayerProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                table: "PlayerProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "PlayerProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Alliances",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_Alliances_WorldId_Name",
                table: "Alliances",
                columns: new[] { "WorldId", "Name" });
        }
    }
}
