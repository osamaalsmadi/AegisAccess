using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisAccess.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PortfolioGovernanceV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessReviewCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedByUsername = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessReviewCampaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SodPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FirstRoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SecondRoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SodPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SodPolicies_Roles_FirstRoleId",
                        column: x => x.FirstRoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SodPolicies_Roles_SecondRoleId",
                        column: x => x.SecondRoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccessReviewItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CampaignId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccessRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Decision = table.Column<int>(type: "INTEGER", nullable: false),
                    ReviewerUsername = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ReviewComment = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RiskAtReview = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessReviewItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessReviewItems_AccessRequests_AccessRequestId",
                        column: x => x.AccessRequestId,
                        principalTable: "AccessRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessReviewItems_AccessReviewCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "AccessReviewCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessReviewCampaigns_Name",
                table: "AccessReviewCampaigns",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AccessReviewCampaigns_Status_DueAtUtc",
                table: "AccessReviewCampaigns",
                columns: new[] { "Status", "DueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessReviewItems_AccessRequestId",
                table: "AccessReviewItems",
                column: "AccessRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessReviewItems_CampaignId_AccessRequestId",
                table: "AccessReviewItems",
                columns: new[] { "CampaignId", "AccessRequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SodPolicies_FirstRoleId_SecondRoleId",
                table: "SodPolicies",
                columns: new[] { "FirstRoleId", "SecondRoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SodPolicies_Name",
                table: "SodPolicies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SodPolicies_SecondRoleId",
                table: "SodPolicies",
                column: "SecondRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessReviewItems");

            migrationBuilder.DropTable(
                name: "SodPolicies");

            migrationBuilder.DropTable(
                name: "AccessReviewCampaigns");
        }
    }
}
