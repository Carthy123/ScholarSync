using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScholarSync.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentClassRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SchoolClassId",
                table: "Students",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_SchoolClassId",
                table: "Students",
                column: "SchoolClassId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Classes_SchoolClassId",
                table: "Students",
                column: "SchoolClassId",
                principalTable: "Classes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Classes_SchoolClassId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_SchoolClassId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "SchoolClassId",
                table: "Students");
        }
    }
}
