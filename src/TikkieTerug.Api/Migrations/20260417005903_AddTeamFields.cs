using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TikkieTerug.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentClubId",
                table: "Clubs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamNumber",
                table: "Clubs",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentClubId",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "TeamNumber",
                table: "Clubs");
        }
    }
}
