using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TikkieTerug.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddClubCompetitionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompetitionId",
                table: "Clubs",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompetitionId",
                table: "Clubs");
        }
    }
}
