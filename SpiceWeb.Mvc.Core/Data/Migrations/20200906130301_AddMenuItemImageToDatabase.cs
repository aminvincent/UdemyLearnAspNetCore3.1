using Microsoft.EntityFrameworkCore.Migrations;

namespace SpiceWeb.Mvc.Core.Data.Migrations
{
    public partial class AddMenuItemImageToDatabase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "MenuItem",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image",
                table: "MenuItem");
        }
    }
}
