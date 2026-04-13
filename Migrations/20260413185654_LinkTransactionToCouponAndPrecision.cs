using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QRCoupanWalletSystem.Migrations
{
    /// <inheritdoc />
    public partial class LinkTransactionToCouponAndPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CouponId",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CouponId",
                table: "Transactions",
                column: "CouponId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Coupons_CouponId",
                table: "Transactions",
                column: "CouponId",
                principalTable: "Coupons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Coupons_CouponId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_CouponId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CouponId",
                table: "Transactions");
        }
    }
}
