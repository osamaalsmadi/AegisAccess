using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisAccess.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessGovernanceWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: false),
                    RequesterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ResourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestedRoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BusinessJustification = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DurationDays = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AccessExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RiskScore = table.Column<int>(type: "INTEGER", nullable: false),
                    RiskLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    RiskReasons = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Recommendation = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DecidedById = table.Column<Guid>(type: "TEXT", nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DecisionComment = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessRequests_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessRequests_Roles_RequestedRoleId",
                        column: x => x.RequestedRoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessRequests_Users_DecidedById",
                        column: x => x.DecidedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessRequests_Users_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessRequests_CorrelationId",
                table: "AccessRequests",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessRequests_DecidedById",
                table: "AccessRequests",
                column: "DecidedById");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRequests_RequestedRoleId",
                table: "AccessRequests",
                column: "RequestedRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRequests_RequesterId_Status",
                table: "AccessRequests",
                columns: new[] { "RequesterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessRequests_ResourceId_RequestedRoleId",
                table: "AccessRequests",
                columns: new[] { "ResourceId", "RequestedRoleId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessRequests");
        }
    }
}
