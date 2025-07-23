using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenIdProvider.Data.Migrations.Application
{
    /// <inheritdoc />
    public partial class ClientUserOrganizationMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserClientPermission_AspNetUsers_UserId",
                table: "UserClientPermission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserClientPermission",
                table: "UserClientPermission");

            migrationBuilder.RenameTable(
                name: "UserClientPermission",
                newName: "UserClientPermissions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserClientPermissions",
                table: "UserClientPermissions",
                columns: new[] { "UserId", "ClientId" });

            migrationBuilder.CreateTable(
                name: "ClientOwnerships",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientOwnerships", x => new { x.OrganizationId, x.ClientId });
                    table.ForeignKey(
                        name: "FK_ClientOwnerships_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationClientPermissions",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationClientPermissions", x => new { x.OrganizationId, x.ClientId });
                    table.ForeignKey(
                        name: "FK_OrganizationClientPermissions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_UserClientPermissions_AspNetUsers_UserId",
                table: "UserClientPermissions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserClientPermissions_AspNetUsers_UserId",
                table: "UserClientPermissions");

            migrationBuilder.DropTable(
                name: "ClientOwnerships");

            migrationBuilder.DropTable(
                name: "OrganizationClientPermissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserClientPermissions",
                table: "UserClientPermissions");

            migrationBuilder.RenameTable(
                name: "UserClientPermissions",
                newName: "UserClientPermission");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserClientPermission",
                table: "UserClientPermission",
                columns: new[] { "UserId", "ClientId" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserClientPermission_AspNetUsers_UserId",
                table: "UserClientPermission",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
