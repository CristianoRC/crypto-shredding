using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Customers.Core.Migrations.KeyVault
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EncryptionKeys",
                columns: table => new
                {
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<byte[]>(type: "bytea", nullable: false),
                    InitializationVector = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncryptionKeys", x => x.CustomerId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EncryptionKeys");
        }
    }
}
