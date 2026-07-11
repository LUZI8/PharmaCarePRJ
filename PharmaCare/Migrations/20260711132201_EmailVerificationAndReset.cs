using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmaCare.Migrations
{
    /// <inheritdoc />
    public partial class EmailVerificationAndReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationCode",
                table: "User",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationCodeExpiry",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "User",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetCode",
                table: "User",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetCodeExpiry",
                table: "User",
                type: "datetime2",
                nullable: true);

            // Grandfather existing accounts as already verified so they aren't locked out of login.
            migrationBuilder.Sql("UPDATE [User] SET IsEmailVerified = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailVerificationCode",
                table: "User");

            migrationBuilder.DropColumn(
                name: "EmailVerificationCodeExpiry",
                table: "User");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "User");

            migrationBuilder.DropColumn(
                name: "PasswordResetCode",
                table: "User");

            migrationBuilder.DropColumn(
                name: "PasswordResetCodeExpiry",
                table: "User");
        }
    }
}
