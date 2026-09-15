using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class _0004_imageUrlInProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_products_seller_profiles_SellerProfileId",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "FK_products_users_SellerId",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_SellerId",
                table: "products");

            migrationBuilder.DropColumn(
                name: "SellerId",
                table: "products");

            migrationBuilder.AlterColumn<Guid>(
                name: "SellerProfileId",
                table: "products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_products_seller_profiles_SellerProfileId",
                table: "products",
                column: "SellerProfileId",
                principalTable: "seller_profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_products_seller_profiles_SellerProfileId",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "products");

            migrationBuilder.AlterColumn<Guid>(
                name: "SellerProfileId",
                table: "products",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "SellerId",
                table: "products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_products_SellerId",
                table: "products",
                column: "SellerId");

            migrationBuilder.AddForeignKey(
                name: "FK_products_seller_profiles_SellerProfileId",
                table: "products",
                column: "SellerProfileId",
                principalTable: "seller_profiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_products_users_SellerId",
                table: "products",
                column: "SellerId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
