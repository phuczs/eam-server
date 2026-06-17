using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EAM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PaymentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsExemptFromAutomatedRules",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "UserBankAccountId",
                table: "Payments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserBankAccountId1",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserBankAccount",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EncryptedAccountNumber = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Last4 = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBankAccount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBankAccount_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserBankAccountId1",
                table: "Payments",
                column: "UserBankAccountId1");

            migrationBuilder.CreateIndex(
                name: "IX_user_bank_accounts_is_primary",
                table: "UserBankAccount",
                column: "IsPrimary");

            migrationBuilder.CreateIndex(
                name: "IX_user_bank_accounts_user_id",
                table: "UserBankAccount",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_UserBankAccount_UserBankAccountId1",
                table: "Payments",
                column: "UserBankAccountId1",
                principalTable: "UserBankAccount",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_UserBankAccount_UserBankAccountId1",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "UserBankAccount");

            migrationBuilder.DropIndex(
                name: "IX_Payments_UserBankAccountId1",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IsExemptFromAutomatedRules",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserBankAccountId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "UserBankAccountId1",
                table: "Payments");
        }
    }
}
