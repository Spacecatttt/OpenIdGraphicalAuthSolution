using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenIdProvider.Data.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddPrimaryOrganizationToUserAndUserOrganizationRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Organizations_OrganizationId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "AspNetUsers",
                newName: "PrimaryOrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_OrganizationId",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_PrimaryOrganizationId");

            migrationBuilder.CreateTable(
                name: "UserOrganizationRole",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AddedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOrganizationRole", x => new { x.UserId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_UserOrganizationRole_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserOrganizationRole_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserOrganizationRole_OrganizationId",
                table: "UserOrganizationRole",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Organizations_PrimaryOrganizationId",
                table: "AspNetUsers",
                column: "PrimaryOrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Organizations_PrimaryOrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "UserOrganizationRole");

            migrationBuilder.RenameColumn(
                name: "PrimaryOrganizationId",
                table: "AspNetUsers",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_PrimaryOrganizationId",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Organizations_OrganizationId",
                table: "AspNetUsers",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
