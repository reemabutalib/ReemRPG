using System;
using Microsoft.EntityFrameworkCore.Migrations;
using ReemRPG.Data;

#nullable disable

namespace ReemRPG.Migrations
{
    /// <inheritdoc />
    public partial class FixCharacterQuestsRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CharacterQuests",
                table: "CharacterQuests");

            migrationBuilder.DropIndex(
                name: "IX_CharacterQuests_CharacterId",
                table: "CharacterQuests");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletionDate",
                table: "CharacterQuests",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "CharacterQuests",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CharacterQuests",
                table: "CharacterQuests",
                columns: new[] { "CharacterId", "QuestId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CharacterQuests",
                table: "CharacterQuests");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "CharacterQuests",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletionDate",
                table: "CharacterQuests",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CharacterQuests",
                table: "CharacterQuests",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterQuests_CharacterId",
                table: "CharacterQuests",
                column: "CharacterId");
        }
    }
}
