using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamTrack.Migrations
{
    /// <inheritdoc />
    public partial class LinkCommentsToWorkItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_comments_tickets_ticket_id",
                table: "comments");

            migrationBuilder.RenameColumn(
                name: "ticket_id",
                table: "comments",
                newName: "TicketId");

            migrationBuilder.RenameIndex(
                name: "IX_comments_ticket_id",
                table: "comments",
                newName: "IX_comments_TicketId");

            migrationBuilder.AlterColumn<int>(
                name: "TicketId",
                table: "comments",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "work_item_id",
                table: "comments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_comments_work_item_id",
                table: "comments",
                column: "work_item_id");

            migrationBuilder.AddForeignKey(
                name: "FK_comments_tickets_TicketId",
                table: "comments",
                column: "TicketId",
                principalTable: "tickets",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_comments_work_items_work_item_id",
                table: "comments",
                column: "work_item_id",
                principalTable: "work_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_comments_tickets_TicketId",
                table: "comments");

            migrationBuilder.DropForeignKey(
                name: "FK_comments_work_items_work_item_id",
                table: "comments");

            migrationBuilder.DropIndex(
                name: "IX_comments_work_item_id",
                table: "comments");

            migrationBuilder.DropColumn(
                name: "work_item_id",
                table: "comments");

            migrationBuilder.RenameColumn(
                name: "TicketId",
                table: "comments",
                newName: "ticket_id");

            migrationBuilder.RenameIndex(
                name: "IX_comments_TicketId",
                table: "comments",
                newName: "IX_comments_ticket_id");

            migrationBuilder.AlterColumn<int>(
                name: "ticket_id",
                table: "comments",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_comments_tickets_ticket_id",
                table: "comments",
                column: "ticket_id",
                principalTable: "tickets",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
