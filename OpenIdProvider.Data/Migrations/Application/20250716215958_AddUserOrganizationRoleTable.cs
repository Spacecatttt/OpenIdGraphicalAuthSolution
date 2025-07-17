using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenIdProvider.Data.Migrations.Application
{
    /// <inheritdoc />
    public partial class AddUserOrganizationRoleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserOrganizationRole_AspNetUsers_UserId",
                table: "UserOrganizationRole");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOrganizationRole_Organizations_OrganizationId",
                table: "UserOrganizationRole");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserOrganizationRole",
                table: "UserOrganizationRole");

            migrationBuilder.RenameTable(
                name: "UserOrganizationRole",
                newName: "UserOrganizationRoles");

            migrationBuilder.RenameIndex(
                name: "IX_UserOrganizationRole_OrganizationId",
                table: "UserOrganizationRoles",
                newName: "IX_UserOrganizationRoles_OrganizationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserOrganizationRoles",
                table: "UserOrganizationRoles",
                columns: new[] { "UserId", "OrganizationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserOrganizationRoles_AspNetUsers_UserId",
                table: "UserOrganizationRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOrganizationRoles_Organizations_OrganizationId",
                table: "UserOrganizationRoles",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserOrganizationRoles_AspNetUsers_UserId",
                table: "UserOrganizationRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOrganizationRoles_Organizations_OrganizationId",
                table: "UserOrganizationRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserOrganizationRoles",
                table: "UserOrganizationRoles");

            migrationBuilder.RenameTable(
                name: "UserOrganizationRoles",
                newName: "UserOrganizationRole");

            migrationBuilder.RenameIndex(
                name: "IX_UserOrganizationRoles_OrganizationId",
                table: "UserOrganizationRole",
                newName: "IX_UserOrganizationRole_OrganizationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserOrganizationRole",
                table: "UserOrganizationRole",
                columns: new[] { "UserId", "OrganizationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_UserOrganizationRole_AspNetUsers_UserId",
                table: "UserOrganizationRole",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOrganizationRole_Organizations_OrganizationId",
                table: "UserOrganizationRole",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
