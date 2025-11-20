using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridPulse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20251120_AddTicketsAndDispatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "crews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Region = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Skills = table.Column<string>(type: "text", nullable: false),
                    CurrentTicketCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastStatusUpdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    preferred_shift_end_minutes = table.Column<int>(type: "integer", nullable: false),
                    DeviceEndpoint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutageReferenceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AffectedAssets = table.Column<string>(type: "text", nullable: false),
                    CustomerImpact = table.Column<int>(type: "integer", nullable: false),
                    EtaMinutes = table.Column<int>(type: "integer", nullable: true),
                    AssignedCrewId = table.Column<Guid>(type: "uuid", nullable: true),
                    AutomationSource = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "ingestion"),
                    AuditVersion = table.Column<int>(type: "integer", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "crew_location_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CrewId = table.Column<Guid>(type: "uuid", nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SignalAgeSeconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsStale = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SpeedMph = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crew_location_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_crew_location_snapshots_crews_CrewId",
                        column: x => x.CrewId,
                        principalTable: "crews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assignment_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    CrewId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false),
                    Actor = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignment_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assignment_events_crews_CrewId",
                        column: x => x.CrewId,
                        principalTable: "crews",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_assignment_events_tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dispatch_recommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    CrewId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompositeScore = table.Column<double>(type: "double precision", precision: 5, scale: 4, nullable: false),
                    ScoreComponents = table.Column<string>(type: "text", nullable: false),
                    RecommendedRouteEtaMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsAutoSelected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsOverride = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    OverrideReason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispatch_recommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dispatch_recommendations_crews_CrewId",
                        column: x => x.CrewId,
                        principalTable: "crews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dispatch_recommendations_tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assignment_events_CrewId",
                table: "assignment_events",
                column: "CrewId");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_events_TicketId",
                table: "assignment_events",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_crew_location_snapshots_CrewId_CapturedAt",
                table: "crew_location_snapshots",
                columns: new[] { "CrewId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_crews_Region",
                table: "crews",
                column: "Region");

            migrationBuilder.CreateIndex(
                name: "IX_crews_Status",
                table: "crews",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_dispatch_recommendations_CrewId",
                table: "dispatch_recommendations",
                column: "CrewId");

            migrationBuilder.CreateIndex(
                name: "IX_dispatch_recommendations_TicketId_CrewId_CreatedAt",
                table: "dispatch_recommendations",
                columns: new[] { "TicketId", "CrewId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_tickets_AssignedCrewId",
                table: "tickets",
                column: "AssignedCrewId");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_Priority",
                table: "tickets",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_Status",
                table: "tickets",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assignment_events");

            migrationBuilder.DropTable(
                name: "crew_location_snapshots");

            migrationBuilder.DropTable(
                name: "dispatch_recommendations");

            migrationBuilder.DropTable(
                name: "crews");

            migrationBuilder.DropTable(
                name: "tickets");
        }
    }
}
