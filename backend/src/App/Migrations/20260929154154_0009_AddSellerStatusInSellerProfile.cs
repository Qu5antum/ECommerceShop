using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class _0009_AddSellerStatusInSellerProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "seller_profiles");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "seller_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "seller_profiles");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "seller_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
