using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReemRPG.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCharacterId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "UserCharacters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Id",
                table: "UserCharacters");
        }
    }
}
