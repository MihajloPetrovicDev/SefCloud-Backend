using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SefCloud.Backend.Migrations
{
    /// <inheritdoc />
    public partial class CreateContainerItemsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContainerItems",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("SqlServer:Indentity", "1, 1"),
                    ContainerId = table.Column<int>(nullable: false),
                    FileName = table.Column<string>(nullable: false),
                    FileSize = table.Column<long>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContainerItems_Containers_Id",
                        column: x => x.ContainerId,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
