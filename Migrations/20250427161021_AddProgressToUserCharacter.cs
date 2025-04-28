using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReemRPG.Migrations
{
    /// <inheritdoc />
    public partial class AddProgressToUserCharacter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Experience",
                table: "UserCharacters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Gold",
                table: "UserCharacters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "UserCharacters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_UserCharacters_AspNetUsers_UserId",
                table: "UserCharacters",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCharacters_AspNetUsers_UserId",
                table: "UserCharacters");

            migrationBuilder.DropColumn(
                name: "Experience",
                table: "UserCharacters");

            migrationBuilder.DropColumn(
                name: "Gold",
                table: "UserCharacters");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "UserCharacters");
        }
    }
}
