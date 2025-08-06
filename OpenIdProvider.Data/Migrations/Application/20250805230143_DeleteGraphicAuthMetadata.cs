using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenIdProvider.Data.Migrations.Application
{
    /// <inheritdoc />
    public partial class DeleteGraphicAuthMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GraphicalAuthMetadata",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "GraphicalPasswordHash",
                table: "AspNetUsers",
                newName: "GraphicalPasswordKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GraphicalPasswordKey",
                table: "AspNetUsers",
                newName: "GraphicalPasswordHash");

            migrationBuilder.AddColumn<string>(
                name: "GraphicalAuthMetadata",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }
    }
}
