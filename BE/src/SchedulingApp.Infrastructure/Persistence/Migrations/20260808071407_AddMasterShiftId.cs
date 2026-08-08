using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterShiftId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MasterShiftId",
                table: "Shifts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MasterShiftId",
                table: "Shifts");
        }
    }
}
