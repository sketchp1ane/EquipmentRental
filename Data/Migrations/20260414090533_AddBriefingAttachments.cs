using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentRental.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBriefingAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BriefingAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BriefingId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BriefingAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BriefingAttachments_SafetyBriefings_BriefingId",
                        column: x => x.BriefingId,
                        principalTable: "SafetyBriefings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BriefingAttachments_BriefingId",
                table: "BriefingAttachments",
                column: "BriefingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BriefingAttachments");
        }
    }
}
