using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_Vent.Migrations
{
    /// <inheritdoc />
    public partial class EventUniqueIdentifierIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UUID",
                table: "GeneralForms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UUID",
                table: "GeneralForms");
        }
    }
}
