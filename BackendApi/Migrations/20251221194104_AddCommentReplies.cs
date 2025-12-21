using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentReplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentCommentId",
                table: "RecipeComments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeComments_ParentCommentId",
                table: "RecipeComments",
                column: "ParentCommentId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeComments_RecipeComments_ParentCommentId",
                table: "RecipeComments",
                column: "ParentCommentId",
                principalTable: "RecipeComments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecipeComments_RecipeComments_ParentCommentId",
                table: "RecipeComments");

            migrationBuilder.DropIndex(
                name: "IX_RecipeComments_ParentCommentId",
                table: "RecipeComments");

            migrationBuilder.DropColumn(
                name: "ParentCommentId",
                table: "RecipeComments");
        }
    }
}
