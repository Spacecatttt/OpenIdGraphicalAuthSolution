using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenIdProvider.Data.Migrations.Application
{
    /// <inheritdoc />
    public partial class DeleteUserGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserGroups_AspNetUsers_ApplicationUserId",
                table: "UserGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_UserGroups_Groups_GroupId",
                table: "UserGroups");

            migrationBuilder.DropColumn(
                name: "AssignedDate",
                table: "UserGroups");

            migrationBuilder.RenameColumn(
                name: "GroupId",
                table: "UserGroups",
                newName: "UsersId");

            migrationBuilder.RenameColumn(
                name: "ApplicationUserId",
                table: "UserGroups",
                newName: "GroupsId");

            migrationBuilder.RenameIndex(
                name: "IX_UserGroups_GroupId",
                table: "UserGroups",
                newName: "IX_UserGroups_UsersId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserGroups_AspNetUsers_UsersId",
                table: "UserGroups",
                column: "UsersId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserGroups_Groups_GroupsId",
                table: "UserGroups",
                column: "GroupsId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserGroups_AspNetUsers_UsersId",
                table: "UserGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_UserGroups_Groups_GroupsId",
                table: "UserGroups");

            migrationBuilder.RenameColumn(
                name: "UsersId",
                table: "UserGroups",
                newName: "GroupId");

            migrationBuilder.RenameColumn(
                name: "GroupsId",
                table: "UserGroups",
                newName: "ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserGroups_UsersId",
                table: "UserGroups",
                newName: "IX_UserGroups_GroupId");

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedDate",
                table: "UserGroups",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddForeignKey(
                name: "FK_UserGroups_AspNetUsers_ApplicationUserId",
                table: "UserGroups",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserGroups_Groups_GroupId",
                table: "UserGroups",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
