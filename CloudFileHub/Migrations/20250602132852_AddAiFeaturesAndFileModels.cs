using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudFileHub.Migrations
{
    /// <inheritdoc />
    public partial class AddAiFeaturesAndFileModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiCategory",
                table: "Files",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "Files",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AiTags",
                table: "Files",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAiAnalysis",
                table: "Files",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiAnalysisResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FileId = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tags = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Confidence = table.Column<double>(type: "double", nullable: false),
                    Language = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnalysisDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExtractedContent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAnalysisResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiAnalysisResults_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AiAnalysisResults_FileId",
                table: "AiAnalysisResults",
                column: "FileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiAnalysisResults");

            migrationBuilder.DropColumn(
                name: "AiCategory",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "AiTags",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "LastAiAnalysis",
                table: "Files");
        }
    }
}
