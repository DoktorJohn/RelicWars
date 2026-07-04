using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RealtimePersistenceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UnitDeployments_DueMovement",
                table: "UnitDeployments",
                columns: new[] { "UnitDeploymentMovementStatus", "NextStepTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Due",
                table: "Jobs",
                columns: new[] { "IsCompleted", "ExecutionTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnitDeployments_DueMovement",
                table: "UnitDeployments");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_Due",
                table: "Jobs");
        }
    }
}
