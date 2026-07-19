using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Customers.Core.Migrations.KeyVault
{
    /// <inheritdoc />
    public partial class EnvelopeEncryption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitializationVector",
                table: "EncryptionKeys");

            migrationBuilder.RenameColumn(
                name: "Key",
                table: "EncryptionKeys",
                newName: "WrappedKeyMaterial");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WrappedKeyMaterial",
                table: "EncryptionKeys",
                newName: "Key");

            migrationBuilder.AddColumn<byte[]>(
                name: "InitializationVector",
                table: "EncryptionKeys",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
