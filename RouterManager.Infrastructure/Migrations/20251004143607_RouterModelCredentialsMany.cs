using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RouterManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RouterModelCredentialsMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RouterCredentials_RouterModelId",
                table: "RouterCredentials");

            migrationBuilder.CreateIndex(
                name: "IX_RouterCredentials_RouterModelId",
                table: "RouterCredentials",
                column: "RouterModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RouterCredentials_RouterModelId",
                table: "RouterCredentials");

            migrationBuilder.CreateIndex(
                name: "IX_RouterCredentials_RouterModelId",
                table: "RouterCredentials",
                column: "RouterModelId",
                unique: true);
        }
    }
}
