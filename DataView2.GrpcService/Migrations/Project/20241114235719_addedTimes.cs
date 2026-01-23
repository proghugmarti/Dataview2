using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataView2.GrpcService.Migrations
{
    /// <inheritdoc />
    public partial class addedTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Time",
                table: "VideoFrame");

            migrationBuilder.AddColumn<long>(
                name: "CameraTime",
                table: "VideoFrame",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "PCTime",
                table: "VideoFrame",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CameraTime",
                table: "VideoFrame");

            migrationBuilder.DropColumn(
                name: "PCTime",
                table: "VideoFrame");

            migrationBuilder.AddColumn<DateTime>(
                name: "Time",
                table: "VideoFrame",
                type: "TEXT",
                nullable: true);
        }
    }
}
