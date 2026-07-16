using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Homelab.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityAdministrationAudits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdentityAdministrationAudits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OccurredUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ActorUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    TargetUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    TargetRoleId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Detail = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityAdministrationAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityAdministrationAudits_ActorUserId_OccurredUtc",
                table: "IdentityAdministrationAudits",
                columns: new[] { "ActorUserId", "OccurredUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityAdministrationAudits_CorrelationId",
                table: "IdentityAdministrationAudits",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityAdministrationAudits_OccurredUtc",
                table: "IdentityAdministrationAudits",
                column: "OccurredUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityAdministrationAudits_TargetUserId_OccurredUtc",
                table: "IdentityAdministrationAudits",
                columns: new[] { "TargetUserId", "OccurredUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentityAdministrationAudits");
        }
    }
}
