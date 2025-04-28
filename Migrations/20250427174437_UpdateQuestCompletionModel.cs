using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReemRPG.Migrations
{
    /// <inheritdoc />
    public partial class UpdateQuestCompletionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Characters_CharacterId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_CharacterId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "CharacterId",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "Level",
                table: "Characters",
                newName: "BaseStrength");

            migrationBuilder.RenameColumn(
                name: "Health",
                table: "Characters",
                newName: "BaseIntelligence");

            migrationBuilder.RenameColumn(
                name: "Gold",
                table: "Characters",
                newName: "BaseHealth");

            migrationBuilder.RenameColumn(
                name: "Experience",
                table: "Characters",
                newName: "BaseAttackPower");

            migrationBuilder.RenameColumn(
                name: "AttackPower",
                table: "Characters",
                newName: "BaseAgility");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "QuestCompletions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    QuestId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CompletedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExperienceGained = table.Column<int>(type: "INTEGER", nullable: false),
                    GoldGained = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestCompletions_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "CharacterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestCompletions_CharacterId",
                table: "QuestCompletions",
                column: "CharacterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestCompletions");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Characters");

            migrationBuilder.RenameColumn(
                name: "BaseStrength",
                table: "Characters",
                newName: "Level");

            migrationBuilder.RenameColumn(
                name: "BaseIntelligence",
                table: "Characters",
                newName: "Health");

            migrationBuilder.RenameColumn(
                name: "BaseHealth",
                table: "Characters",
                newName: "Gold");

            migrationBuilder.RenameColumn(
                name: "BaseAttackPower",
                table: "Characters",
                newName: "Experience");

            migrationBuilder.RenameColumn(
                name: "BaseAgility",
                table: "Characters",
                newName: "AttackPower");

            migrationBuilder.AddColumn<int>(
                name: "CharacterId",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_CharacterId",
                table: "Items",
                column: "CharacterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Characters_CharacterId",
                table: "Items",
                column: "CharacterId",
                principalTable: "Characters",
                principalColumn: "CharacterId");
        }
    }
}
