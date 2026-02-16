using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    username = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sites",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_sites", x => x.id);
                    table.ForeignKey(
                        name: "FK_Site_Account",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "web_sessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    connection_id = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_web_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_WebSessions_Account",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "processes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    instance_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    connection_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_online = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    machine_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_sending = table.Column<bool>(type: "boolean", nullable: false),
                    last_received = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_online = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    account_entity_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_processes", x => x.id);
                    table.ForeignKey(
                        name: "f_k_processes__sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "f_k_processes_accounts_account_entity_id",
                        column: x => x.account_entity_id,
                        principalTable: "accounts",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "secrets",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_secrets", x => x.id);
                    table.ForeignKey(
                        name: "FK_Secret_Site",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "web_subscriptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    process_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    renewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_web_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_WebSubscriptions_Process",
                        column: x => x.process_id,
                        principalTable: "processes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_WebSubscriptions_Session",
                        column: x => x.session_id,
                        principalTable: "web_sessions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "i_x_processes_account_entity_id",
                table: "processes",
                column: "account_entity_id");

            migrationBuilder.CreateIndex(
                name: "i_x_processes_site_id",
                table: "processes",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "i_x_secrets_site_id",
                table: "secrets",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "i_x_sites_account_id",
                table: "sites",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "i_x_web_sessions_account_id",
                table: "web_sessions",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_web_sessions_connection_id",
                table: "web_sessions",
                column: "connection_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_web_subscriptions_process_id",
                table: "web_subscriptions",
                column: "process_id");

            migrationBuilder.CreateIndex(
                name: "i_x_web_subscriptions_session_id",
                table: "web_subscriptions",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_web_subscriptions_session_id_process_id",
                table: "web_subscriptions",
                columns: new[] { "session_id", "process_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "secrets");

            migrationBuilder.DropTable(
                name: "web_subscriptions");

            migrationBuilder.DropTable(
                name: "processes");

            migrationBuilder.DropTable(
                name: "web_sessions");

            migrationBuilder.DropTable(
                name: "sites");

            migrationBuilder.DropTable(
                name: "accounts");
        }
    }
}
