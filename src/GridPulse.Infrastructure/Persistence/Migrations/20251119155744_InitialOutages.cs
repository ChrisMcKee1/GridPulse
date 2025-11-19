using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridPulse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialOutages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "Reported"),
                    ReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EstimatedRestoration = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Cause = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outage_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StatusFrom = table.Column<int>(type: "integer", nullable: true),
                    StatusTo = table.Column<int>(type: "integer", nullable: true),
                    Message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outage_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_outage_events_outages_OutageId",
                        column: x => x.OutageId,
                        principalTable: "outages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "outages",
                columns: new[] { "Id", "Cause", "EstimatedRestoration", "LastUpdatedAt", "ReportedAt", "ServiceAddress", "ServiceLocationId", "Status" },
                values: new object[] { new Guid("17aa5a0f-4f5f-45ec-8dc0-1b53e876c111"), "Tree on line", new DateTimeOffset(new DateTime(2024, 10, 18, 18, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2024, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2024, 10, 18, 14, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "123 Contoso Ave, Apex, NC", new Guid("f0b8c3a0-4f85-4f50-9b26-7b6b4c1cf001"), "CrewDispatched" });

            migrationBuilder.InsertData(
                table: "outage_events",
                columns: new[] { "Id", "CreatedBy", "Message", "OutageId", "StatusFrom", "StatusTo", "Timestamp", "Type" },
                values: new object[] { new Guid("b1a4b46c-6e54-4d79-b3cb-9d537c8af901"), "system", "Outage acknowledged", new Guid("17aa5a0f-4f5f-45ec-8dc0-1b53e876c111"), 0, 1, new DateTimeOffset(new DateTime(2024, 10, 18, 14, 45, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "StatusChange" });

            migrationBuilder.CreateIndex(
                name: "IX_outage_events_OutageId",
                table: "outage_events",
                column: "OutageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outage_events");

            migrationBuilder.DropTable(
                name: "outages");
        }
    }
}
