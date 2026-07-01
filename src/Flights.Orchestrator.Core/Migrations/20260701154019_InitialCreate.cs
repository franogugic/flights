using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flights.Orchestrator.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BacklogTasks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectName = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    AcceptanceCriteria = table.Column<string>(type: "TEXT", nullable: false),
                    DependsOnTaskIds = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IterationCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastDeveloperSummaryJson = table.Column<string>(type: "TEXT", nullable: true),
                    LastReviewerVerdictJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BacklogTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IterationRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BacklogTaskId = table.Column<string>(type: "TEXT", nullable: false),
                    IterationNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    DeveloperSummaryJson = table.Column<string>(type: "TEXT", nullable: false),
                    ReviewerVerdictJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IterationRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PendingQuestions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectName = table.Column<string>(type: "TEXT", nullable: false),
                    BacklogTaskId = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestionText = table.Column<string>(type: "TEXT", nullable: false),
                    ContextJson = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AnswerText = table.Column<string>(type: "TEXT", nullable: true),
                    Consumed = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AnsweredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingQuestions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IterationRecords_BacklogTaskId",
                table: "IterationRecords",
                column: "BacklogTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingQuestions_Status",
                table: "PendingQuestions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BacklogTasks");

            migrationBuilder.DropTable(
                name: "IterationRecords");

            migrationBuilder.DropTable(
                name: "PendingQuestions");
        }
    }
}
