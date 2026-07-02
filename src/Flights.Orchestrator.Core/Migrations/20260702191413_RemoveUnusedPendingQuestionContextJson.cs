using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flights.Orchestrator.Core.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedPendingQuestionContextJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContextJson",
                table: "PendingQuestions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContextJson",
                table: "PendingQuestions",
                type: "TEXT",
                nullable: true);
        }
    }
}
