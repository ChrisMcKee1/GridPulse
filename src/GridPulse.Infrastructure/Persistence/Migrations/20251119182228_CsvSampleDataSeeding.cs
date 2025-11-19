using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GridPulse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CsvSampleDataSeeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "outage_events",
                keyColumn: "Id",
                keyValue: new Guid("b1a4b46c-6e54-4d79-b3cb-9d537c8af901"));

            migrationBuilder.DeleteData(
                table: "outages",
                keyColumn: "Id",
                keyValue: new Guid("17aa5a0f-4f5f-45ec-8dc0-1b53e876c111"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "outages",
                columns: new[] { "Id", "Cause", "EstimatedRestoration", "LastUpdatedAt", "ReportedAt", "ServiceAddress", "ServiceLocationId", "Status" },
                values: new object[] { new Guid("17aa5a0f-4f5f-45ec-8dc0-1b53e876c111"), "Tree on line", new DateTimeOffset(new DateTime(2024, 10, 18, 18, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2024, 10, 18, 15, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2024, 10, 18, 14, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "123 Contoso Ave, Apex, NC", new Guid("f0b8c3a0-4f85-4f50-9b26-7b6b4c1cf001"), "CrewDispatched" });

            migrationBuilder.InsertData(
                table: "outage_events",
                columns: new[] { "Id", "CreatedBy", "Message", "OutageId", "StatusFrom", "StatusTo", "Timestamp", "Type" },
                values: new object[] { new Guid("b1a4b46c-6e54-4d79-b3cb-9d537c8af901"), "system", "Outage acknowledged", new Guid("17aa5a0f-4f5f-45ec-8dc0-1b53e876c111"), 0, 1, new DateTimeOffset(new DateTime(2024, 10, 18, 14, 45, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "StatusChange" });
        }
    }
}
