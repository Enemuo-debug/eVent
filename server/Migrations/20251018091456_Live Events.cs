using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_Vent.Migrations
{
    /// <inheritdoc />
    public partial class LiveEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isLive",
                table: "Events",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isLive",
                table: "Events");
        }
    }
}
