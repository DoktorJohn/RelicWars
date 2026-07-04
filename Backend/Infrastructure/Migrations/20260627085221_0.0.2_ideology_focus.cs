using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _002_ideology_focus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppliesToCategory",
                table: "WorldModifiers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExcludeElite",
                table: "WorldModifiers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AppliesToCategory",
                table: "UnitStackModifiers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExcludeElite",
                table: "UnitStackModifiers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "UnitDeployments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AppliesToCategory",
                table: "UnitDeploymentModifiers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExcludeElite",
                table: "UnitDeploymentModifiers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AppliesToCategory",
                table: "PlayerModifiers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExcludeElite",
                table: "PlayerModifiers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AppliesToCategory",
                table: "CityModifiers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExcludeElite",
                table: "CityModifiers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CraftedEquipment",
                table: "Cities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastResistanceUpdate",
                table: "Cities",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<double>(
                name: "Resistance",
                table: "Cities",
                type: "float",
                nullable: false,
                defaultValue: 100.0);

            migrationBuilder.AddColumn<double>(
                name: "ResistanceTarget",
                table: "Cities",
                type: "float",
                nullable: false,
                defaultValue: 100.0);

            migrationBuilder.AddColumn<double>(
                name: "Damage",
                table: "Buildings",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "AppliesToCategory",
                table: "AllianceModifiers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExcludeElite",
                table: "AllianceModifiers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppliesToCategory",
                table: "WorldModifiers");

            migrationBuilder.DropColumn(
                name: "ExcludeElite",
                table: "WorldModifiers");

            migrationBuilder.DropColumn(
                name: "AppliesToCategory",
                table: "UnitStackModifiers");

            migrationBuilder.DropColumn(
                name: "ExcludeElite",
                table: "UnitStackModifiers");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "UnitDeployments");

            migrationBuilder.DropColumn(
                name: "AppliesToCategory",
                table: "UnitDeploymentModifiers");

            migrationBuilder.DropColumn(
                name: "ExcludeElite",
                table: "UnitDeploymentModifiers");

            migrationBuilder.DropColumn(
                name: "AppliesToCategory",
                table: "PlayerModifiers");

            migrationBuilder.DropColumn(
                name: "ExcludeElite",
                table: "PlayerModifiers");

            migrationBuilder.DropColumn(
                name: "AppliesToCategory",
                table: "CityModifiers");

            migrationBuilder.DropColumn(
                name: "ExcludeElite",
                table: "CityModifiers");

            migrationBuilder.DropColumn(
                name: "CraftedEquipment",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "LastResistanceUpdate",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "Resistance",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "ResistanceTarget",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "Damage",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "AppliesToCategory",
                table: "AllianceModifiers");

            migrationBuilder.DropColumn(
                name: "ExcludeElite",
                table: "AllianceModifiers");
        }
    }
}
