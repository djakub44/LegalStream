using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Outbox_ver2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OutboxRequests_Messages_MessageId",
                table: "OutboxRequests");

            migrationBuilder.DropIndex(
                name: "IX_OutboxRequests_MessageId",
                table: "OutboxRequests");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "OutboxRequests");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "OutboxRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Payload",
                table: "OutboxRequests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "OutboxRequests",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "OutboxRequests");

            migrationBuilder.DropColumn(
                name: "Payload",
                table: "OutboxRequests");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "OutboxRequests");

            migrationBuilder.AddColumn<Guid>(
                name: "MessageId",
                table: "OutboxRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_OutboxRequests_MessageId",
                table: "OutboxRequests",
                column: "MessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_OutboxRequests_Messages_MessageId",
                table: "OutboxRequests",
                column: "MessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
