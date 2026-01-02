using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_and_Sales_Tracker.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneNumberToSalesTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "SalesTransactions",
                type: "TEXT",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "SalesTransactions");
        }
    }
}
