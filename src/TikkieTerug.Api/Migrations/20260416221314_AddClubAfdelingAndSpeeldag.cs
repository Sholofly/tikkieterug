using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TikkieTerug.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddClubAfdelingAndSpeeldag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AfdelingId",
                table: "Clubs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Speeldag",
                table: "Clubs",
                type: "TEXT",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AfdelingId",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "Speeldag",
                table: "Clubs");
        }
    }
}
