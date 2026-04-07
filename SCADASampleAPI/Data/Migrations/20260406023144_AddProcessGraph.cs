using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCADASampleAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessGraph : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScadaProcessLocations",
                columns: table => new
                {
                    ProcessLocationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PipelineId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Capacity = table.Column<double>(type: "float", nullable: false),
                    CurrentVolume = table.Column<double>(type: "float", nullable: false),
                    LayoutX = table.Column<double>(type: "float", nullable: false),
                    LayoutY = table.Column<double>(type: "float", nullable: false),
                    LastUpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScadaProcessLocations", x => x.ProcessLocationId);
                    table.ForeignKey(
                        name: "FK_ScadaProcessLocations_Pipelines_PipelineId",
                        column: x => x.PipelineId,
                        principalTable: "Pipelines",
                        principalColumn: "PipelineId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScadaProcessTransfers",
                columns: table => new
                {
                    ProcessTransferId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PipelineId = table.Column<int>(type: "int", nullable: false),
                    FromLocationId = table.Column<int>(type: "int", nullable: false),
                    ToLocationId = table.Column<int>(type: "int", nullable: false),
                    IsPumpRunning = table.Column<bool>(type: "bit", nullable: false),
                    ValveOpen = table.Column<bool>(type: "bit", nullable: false),
                    MaxFlowRate = table.Column<double>(type: "float", nullable: false),
                    CurrentFlowRate = table.Column<double>(type: "float", nullable: false),
                    LastUpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScadaProcessTransfers", x => x.ProcessTransferId);
                    table.ForeignKey(
                        name: "FK_ScadaProcessTransfers_Pipelines_PipelineId",
                        column: x => x.PipelineId,
                        principalTable: "Pipelines",
                        principalColumn: "PipelineId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScadaProcessTransfers_ScadaProcessLocations_FromLocationId",
                        column: x => x.FromLocationId,
                        principalTable: "ScadaProcessLocations",
                        principalColumn: "ProcessLocationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScadaProcessTransfers_ScadaProcessLocations_ToLocationId",
                        column: x => x.ToLocationId,
                        principalTable: "ScadaProcessLocations",
                        principalColumn: "ProcessLocationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScadaProcessLocations_PipelineId",
                table: "ScadaProcessLocations",
                column: "PipelineId");

            migrationBuilder.CreateIndex(
                name: "IX_ScadaProcessTransfers_FromLocationId",
                table: "ScadaProcessTransfers",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScadaProcessTransfers_PipelineId",
                table: "ScadaProcessTransfers",
                column: "PipelineId");

            migrationBuilder.CreateIndex(
                name: "IX_ScadaProcessTransfers_ToLocationId",
                table: "ScadaProcessTransfers",
                column: "ToLocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScadaProcessTransfers");

            migrationBuilder.DropTable(
                name: "ScadaProcessLocations");
        }
    }
}
