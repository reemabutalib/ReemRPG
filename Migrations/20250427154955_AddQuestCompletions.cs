using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReemRPG.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestCompletions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserCharacters",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<bool>(
                name: "Repeatable",
                table: "Quests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Repeatable",
                table: "Quests");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserCharacters",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
