using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Dynamite.Infrastructure.Persistence;

#nullable disable

namespace Dynamite.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260613000000_GiveawayMultiPreSelect")]
    public partial class GiveawayMultiPreSelect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename single-winner column to comma-separated multi-winner column
            migrationBuilder.RenameColumn(
                name: "PreSelectedWinnerId",
                table: "Giveaways",
                newName: "PreSelectedWinnerIds");

            // Change type from bigint (ulong stored as long) to varchar(512)
            migrationBuilder.AlterColumn<string>(
                name: "PreSelectedWinnerIds",
                table: "Giveaways",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "PreSelectedWinnerIds",
                table: "Giveaways",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "PreSelectedWinnerIds",
                table: "Giveaways",
                newName: "PreSelectedWinnerId");
        }
    }
}
