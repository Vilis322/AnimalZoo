using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnimalZoo.App.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Animals",
                columns: table => new
                {
                    UniqueId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Age = table.Column<double>(type: "float", nullable: false),
                    Mood = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AnimalType = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Animals", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "Enclosures",
                columns: table => new
                {
                    AnimalId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EnclosureName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enclosures", x => x.AnimalId);
                    table.ForeignKey(
                        name: "FK_Enclosures_Animals_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animals",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Animals_AnimalType",
                table: "Animals",
                column: "AnimalType");

            migrationBuilder.CreateIndex(
                name: "IX_Enclosures_EnclosureName",
                table: "Enclosures",
                column: "EnclosureName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Enclosures");

            migrationBuilder.DropTable(
                name: "Animals");
        }
    }
}
