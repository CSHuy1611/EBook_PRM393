using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathIBook.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleQuizzesPerTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quizzes_ChapterId",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_LessonId",
                table: "Quizzes");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_ChapterId",
                table: "Quizzes",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_LessonId",
                table: "Quizzes",
                column: "LessonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quizzes_ChapterId",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_LessonId",
                table: "Quizzes");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_ChapterId",
                table: "Quizzes",
                column: "ChapterId",
                unique: true,
                filter: "\"ChapterId\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_LessonId",
                table: "Quizzes",
                column: "LessonId",
                unique: true,
                filter: "\"LessonId\" IS NOT NULL AND \"IsDeleted\" = FALSE");
        }
    }
}
