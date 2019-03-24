using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FileManager.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    userId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(nullable: true),
                    secondName = table.Column<string>(nullable: true),
                    login = table.Column<string>(nullable: true),
                    PasswordHash = table.Column<byte[]>(nullable: true),
                    HashKey = table.Column<byte[]>(nullable: true),
                    Role = table.Column<string>(nullable: true),
                    Token = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.userId);
                });

            migrationBuilder.CreateTable(
                name: "Objects",
                columns: table => new
                {
                    objectId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    userId = table.Column<int>(nullable: false),
                    objectName = table.Column<string>(nullable: true),
                    type = table.Column<bool>(nullable: false),
                    binaryData = table.Column<byte[]>(nullable: true),
                    right = table.Column<int>(nullable: false),
                    left = table.Column<int>(nullable: false),
                    level = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Objects", x => x.objectId);
                    table.ForeignKey(
                        name: "FK_Objects_Users_userId",
                        column: x => x.userId,
                        principalTable: "Users",
                        principalColumn: "userId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    permissionId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    parentUserId = table.Column<int>(nullable: true),
                    childUserId = table.Column<int>(nullable: true),
                    objectId = table.Column<int>(nullable: false),
                    write = table.Column<bool>(nullable: false),
                    read = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.permissionId);
                    table.ForeignKey(
                        name: "FK_Permissions_Users_childUserId",
                        column: x => x.childUserId,
                        principalTable: "Users",
                        principalColumn: "userId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Permissions_Objects_objectId",
                        column: x => x.objectId,
                        principalTable: "Objects",
                        principalColumn: "objectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Permissions_Users_parentUserId",
                        column: x => x.parentUserId,
                        principalTable: "Users",
                        principalColumn: "userId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Objects_userId",
                table: "Objects",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_childUserId",
                table: "Permissions",
                column: "childUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_objectId",
                table: "Permissions",
                column: "objectId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_parentUserId",
                table: "Permissions",
                column: "parentUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Objects");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
