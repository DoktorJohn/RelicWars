using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeploymentPhasesAndSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnitDeployments_DueMovement",
                table: "UnitDeployments");

            migrationBuilder.DropIndex(
                name: "IX_UnitDeployments_TargetCityId",
                table: "UnitDeployments");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DepartureTime",
                table: "UnitDeployments",
                type: "datetime2(3)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ArrivalTime",
                table: "UnitDeployments",
                type: "datetime2(3)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "LegEndX",
                table: "UnitDeployments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LegEndY",
                table: "UnitDeployments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LegStartX",
                table: "UnitDeployments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LegStartY",
                table: "UnitDeployments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Phase",
                table: "UnitDeployments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StationedAt",
                table: "UnitDeployments",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE deployment
                SET Phase = CASE
                        WHEN deployment.UnitDeploymentMovementStatus = 1 THEN 1
                        WHEN deployment.TargetCityId = deployment.OriginCityId THEN 2
                        ELSE 0
                    END,
                    LegStartX = CASE WHEN deployment.TargetCityId = deployment.OriginCityId THEN COALESCE(targetCity.X, originCity.X) ELSE originCity.X END,
                    LegStartY = CASE WHEN deployment.TargetCityId = deployment.OriginCityId THEN COALESCE(targetCity.Y, originCity.Y) ELSE originCity.Y END,
                    LegEndX = COALESCE(targetCity.X, originCity.X),
                    LegEndY = COALESCE(targetCity.Y, originCity.Y),
                    StationedAt = CASE WHEN deployment.UnitDeploymentMovementStatus = 1 THEN deployment.ArrivalTime ELSE NULL END
                FROM UnitDeployments deployment
                INNER JOIN Cities originCity ON originCity.Id = deployment.OriginCityId
                LEFT JOIN Cities targetCity ON targetCity.Id = deployment.TargetCityId;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_UnitDeployments_DueMovement",
                table: "UnitDeployments",
                columns: new[] { "Phase", "UnitDeploymentMovementStatus", "ArrivalTime" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitDeployments_TargetSupport",
                table: "UnitDeployments",
                columns: new[] { "TargetCityId", "Type", "Phase" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnitDeployments_DueMovement",
                table: "UnitDeployments");

            migrationBuilder.DropIndex(
                name: "IX_UnitDeployments_TargetSupport",
                table: "UnitDeployments");

            migrationBuilder.DropColumn(
                name: "LegEndX",
                table: "UnitDeployments");

            migrationBuilder.DropColumn(
                name: "LegEndY",
                table: "UnitDeployments");

            migrationBuilder.DropColumn(
                name: "LegStartX",
                table: "UnitDeployments");

            migrationBuilder.DropColumn(
                name: "LegStartY",
                table: "UnitDeployments");

            migrationBuilder.DropColumn(
                name: "Phase",
                table: "UnitDeployments");

            migrationBuilder.DropColumn(
                name: "StationedAt",
                table: "UnitDeployments");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DepartureTime",
                table: "UnitDeployments",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ArrivalTime",
                table: "UnitDeployments",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)");

            migrationBuilder.CreateIndex(
                name: "IX_UnitDeployments_DueMovement",
                table: "UnitDeployments",
                columns: new[] { "UnitDeploymentMovementStatus", "ArrivalTime" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitDeployments_TargetCityId",
                table: "UnitDeployments",
                column: "TargetCityId");
        }
    }
}
