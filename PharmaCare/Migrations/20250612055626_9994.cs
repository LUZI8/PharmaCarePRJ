using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaCare.Migrations
{
    /// <inheritdoc />
    public partial class _9994 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactMessages_User_UserId",
                table: "ContactMessages");

            migrationBuilder.AddForeignKey(
                name: "FK_ContactMessages_User_UserId",
                table: "ContactMessages",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactMessages_User_UserId",
                table: "ContactMessages");

            migrationBuilder.AddForeignKey(
                name: "FK_ContactMessages_User_UserId",
                table: "ContactMessages",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId");
        }
    }
}
