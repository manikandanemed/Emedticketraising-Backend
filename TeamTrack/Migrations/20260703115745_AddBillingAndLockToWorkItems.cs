using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingAndLockToWorkItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "developer_bill_lock",
                table: "work_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "fixed_bill_number",
                table: "work_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "raised_bill_number",
                table: "work_items",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "developer_bill_lock",
                table: "work_items");

            migrationBuilder.DropColumn(
                name: "fixed_bill_number",
                table: "work_items");

            migrationBuilder.DropColumn(
                name: "raised_bill_number",
                table: "work_items");
        }
    }
}
