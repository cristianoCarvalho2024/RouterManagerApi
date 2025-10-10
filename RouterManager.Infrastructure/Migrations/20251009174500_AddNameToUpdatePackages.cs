using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RouterManager.Infrastructure.Migrations
{
    public partial class AddNameToUpdatePackages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "UpdatePackages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "UpdatePackages");
        }
    }
}
