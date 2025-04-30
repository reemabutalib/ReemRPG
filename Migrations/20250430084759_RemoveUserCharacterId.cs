using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReemRPG.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserCharacterId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCharacters_Characters_CharacterId",
                table: "UserCharacters");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCharacters_Characters_CharacterId",
                table: "UserCharacters",
                column: "CharacterId",
                principalTable: "Characters",
                principalColumn: "CharacterId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCharacters_Characters_CharacterId",
                table: "UserCharacters");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCharacters_Characters_CharacterId",
                table: "UserCharacters",
                column: "CharacterId",
                principalTable: "Characters",
                principalColumn: "CharacterId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
