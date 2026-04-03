using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPilot.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMonitorSchedulerState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveFailureCount",
                table: "monitors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveSuccessCount",
                table: "monitors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "LastCheckSucceeded",
                table: "monitors",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCheckedAtUtc",
                table: "monitors",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastErrorMessage",
                table: "monitors",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastResponseTimeMs",
                table: "monitors",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastStatusCode",
                table: "monitors",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_monitors_IsActive_LastCheckedAtUtc",
                table: "monitors",
                columns: new[] { "IsActive", "LastCheckedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_monitors_IsActive_LastCheckedAtUtc",
                table: "monitors");

            migrationBuilder.DropColumn(
                name: "ConsecutiveFailureCount",
                table: "monitors");

            migrationBuilder.DropColumn(
                name: "ConsecutiveSuccessCount",
                table: "monitors");

            migrationBuilder.DropColumn(
                name: "LastCheckSucceeded",
                table: "monitors");

            migrationBuilder.DropColumn(
                name: "LastCheckedAtUtc",
                table: "monitors");

            migrationBuilder.DropColumn(
                name: "LastErrorMessage",
                table: "monitors");

            migrationBuilder.DropColumn(
                name: "LastResponseTimeMs",
                table: "monitors");

            migrationBuilder.DropColumn(
                name: "LastStatusCode",
                table: "monitors");
        }
    }
}
