using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RouterManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToRouterProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adiciona coluna como NULL primeiro
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "RouterProfiles",
                type: "int",
                nullable: true);

            // Garante pelo menos um usuário existente
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Users])
BEGIN
    INSERT INTO [Users] ([Username],[PasswordHash],[Role]) VALUES ('migrator@local','placeholder-hash','User');
END");

            // Preenche UserId nos registros existentes
            migrationBuilder.Sql(@"
UPDATE RP
SET RP.[UserId] = U.[Id]
FROM [RouterProfiles] RP
CROSS APPLY (SELECT TOP(1) [Id] FROM [Users] ORDER BY [Id]) U
WHERE RP.[UserId] IS NULL;
");

            // Altera para NOT NULL
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "RouterProfiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouterProfiles_UserId",
                table: "RouterProfiles",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_RouterProfiles_Users_UserId",
                table: "RouterProfiles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RouterProfiles_Users_UserId",
                table: "RouterProfiles");

            migrationBuilder.DropIndex(
                name: "IX_RouterProfiles_UserId",
                table: "RouterProfiles");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "RouterProfiles");
        }
    }
}
