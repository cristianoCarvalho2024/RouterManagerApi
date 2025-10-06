using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RouterManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSerialNumberToUpdatePackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TargetVersion",
                table: "UpdatePackages",
                newName: "RequestPayload");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "UpdatePackages",
                newName: "ModelIdentifier");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UpdatePackages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "FirmwareVersion",
                table: "UpdatePackages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProviderId",
                table: "UpdatePackages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "UpdatePackages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UpdatePackages");

            migrationBuilder.DropColumn(
                name: "FirmwareVersion",
                table: "UpdatePackages");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "UpdatePackages");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "UpdatePackages");

            migrationBuilder.RenameColumn(
                name: "RequestPayload",
                table: "UpdatePackages",
                newName: "TargetVersion");

            migrationBuilder.RenameColumn(
                name: "ModelIdentifier",
                table: "UpdatePackages",
                newName: "Description");
        }
    }
}
