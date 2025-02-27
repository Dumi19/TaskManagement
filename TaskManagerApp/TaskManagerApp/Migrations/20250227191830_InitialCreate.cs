using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagerApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "timemanager");

            // Create Users table in the timemanager schema
            migrationBuilder.CreateTable(
                name: "Users",
                schema: "timemanager",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CurrentTask = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            // Create TaskTimes table in the timemanager schema
            migrationBuilder.CreateTable(
                name: "TaskTimes",
                schema: "timemanager",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskTimes_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "timemanager", // Reference the schema
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
       name: "TaskTimes",
       schema: "timemanager");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "timemanager");
        }
    }
}
