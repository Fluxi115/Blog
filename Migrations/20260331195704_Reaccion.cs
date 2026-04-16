using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreakBlog.API.Migrations
{
    /// <inheritdoc />
    public partial class Reaccion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FireCount",
                table: "Posts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HeartCount",
                table: "Posts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FireCount",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "HeartCount",
                table: "Posts");
        }
    }
}
