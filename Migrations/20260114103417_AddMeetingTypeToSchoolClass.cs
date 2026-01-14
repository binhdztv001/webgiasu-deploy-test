using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Webgiasu.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingTypeToSchoolClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MeetingType",
                table: "SchoolClasses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MeetingType",
                table: "SchoolClasses");
        }
    }
}
