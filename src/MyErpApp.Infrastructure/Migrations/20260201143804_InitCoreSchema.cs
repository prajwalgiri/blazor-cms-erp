using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyErpApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitCoreSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntitySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JsonData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntitySnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UiPages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UiPages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UiComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UiPageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TailwindHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UiComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UiComponents_UiPages_UiPageId",
                        column: x => x.UiPageId,
                        principalTable: "UiPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UiComponents_UiPageId",
                table: "UiComponents",
                column: "UiPageId");

            migrationBuilder.CreateIndex(
                name: "IX_UiPages_Name",
                table: "UiPages",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntitySnapshots");

            migrationBuilder.DropTable(
                name: "UiComponents");

            migrationBuilder.DropTable(
                name: "UiPages");
        }
    }
}
