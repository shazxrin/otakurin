using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Otakurin.Persistence.Migrations
{
    public partial class GameScreenshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScreenshotsUrlsString",
                table: "Games",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScreenshotsUrlsString",
                table: "Games");
        }
    }
}
