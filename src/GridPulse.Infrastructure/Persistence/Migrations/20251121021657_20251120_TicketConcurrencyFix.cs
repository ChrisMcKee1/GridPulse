using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridPulse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20251120_TicketConcurrencyFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assignment_deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    CrewId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackingId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    LastError = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignment_deliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assignment_deliveries_crews_CrewId",
                        column: x => x.CrewId,
                        principalTable: "crews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assignment_deliveries_tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assignment_deliveries_CrewId",
                table: "assignment_deliveries",
                column: "CrewId");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_deliveries_ticket_crew",
                table: "assignment_deliveries",
                columns: new[] { "TicketId", "CrewId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assignment_deliveries");
        }
    }
}
