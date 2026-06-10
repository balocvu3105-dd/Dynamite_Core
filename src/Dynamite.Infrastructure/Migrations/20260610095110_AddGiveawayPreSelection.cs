using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dynamite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGiveawayPreSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PreSelectedAt",
                table: "Giveaways",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PreSelectedBy",
                table: "Giveaways",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PreSelectedWinnerId",
                table: "Giveaways",
                type: "numeric(20,0)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreSelectedAt",
                table: "Giveaways");

            migrationBuilder.DropColumn(
                name: "PreSelectedBy",
                table: "Giveaways");

            migrationBuilder.DropColumn(
                name: "PreSelectedWinnerId",
                table: "Giveaways");
        }
    }
}
