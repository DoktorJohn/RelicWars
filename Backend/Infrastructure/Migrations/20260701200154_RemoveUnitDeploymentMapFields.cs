using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnitDeploymentMapFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnitDeployments_DueMovement",
                table: "UnitDeployments");

            migrationBuilder.DropColumn(
                name: "CurrentX",
                table: "UnitDeployments");

            migrationBuilder.DropColumn(
                name: "CurrentY",
                table: "UnitDeployments");

            migrationBuilder.DropColumn(
                name: "FinalX",
                table: "UnitDeployments");

            migrationBuilder.DropColumn(
                name: "FinalY",
                table: "UnitDeployments");

            migrationBuilder.DropColumn(
                name: "LastStepTime",
                table: "UnitDeployments");

            migrationBuilder.DropColumn(
                name: "NextStepTime",
                table: "UnitDeployments");

            migrationBuilder.DropColumn(
                name: "NextX",
                table: "UnitDeployments");

            migrationBuilder.DropColumn(
                name: "NextY",
                table: "UnitDeployments");

            migrationBuilder.DropColumn(
                name: "RemainingPathJson",
                table: "UnitDeployments");

            migrationBuilder.CreateIndex(
                name: "IX_UnitDeployments_DueMovement",
                table: "UnitDeployments",
                columns: new[] { "UnitDeploymentMovementStatus", "ArrivalTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnitDeployments_DueMovement",
                table: "UnitDeployments");

            migrationBuilder.AddColumn<int>(
                name: "CurrentX",
                table: "UnitDeployments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentY",
                table: "UnitDeployments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FinalX",
                table: "UnitDeployments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FinalY",
                table: "UnitDeployments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastStepTime",
                table: "UnitDeployments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "NextStepTime",
                table: "UnitDeployments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "NextX",
                table: "UnitDeployments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NextY",
                table: "UnitDeployments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RemainingPathJson",
                table: "UnitDeployments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitDeployments_DueMovement",
                table: "UnitDeployments",
                columns: new[] { "UnitDeploymentMovementStatus", "NextStepTime" });
        }
    }
}
