using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class _0003_ProductsColumnInSellerProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "seller_profiles",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<Guid>(
                name: "SellerProfileId",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_SellerProfileId",
                table: "products",
                column: "SellerProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_products_seller_profiles_SellerProfileId",
                table: "products",
                column: "SellerProfileId",
                principalTable: "seller_profiles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_products_seller_profiles_SellerProfileId",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_SellerProfileId",
                table: "products");

            migrationBuilder.DropColumn(
                name: "SellerProfileId",
                table: "products");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "seller_profiles",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
