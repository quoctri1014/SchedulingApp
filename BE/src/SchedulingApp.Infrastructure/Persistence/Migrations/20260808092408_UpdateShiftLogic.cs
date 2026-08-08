using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShiftLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "Shifts",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "EndTime",
                table: "Shifts",
                newName: "EndDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "Shifts",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "EndDate",
                table: "Shifts",
                newName: "EndTime");
        }
    }
}
