using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCADASampleAPI.Data.Migrations;

/// <inheritdoc />
public partial class ProcessLocationFluidsAndTransferFluidCode : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FluidCode",
            table: "ScadaProcessTransfers",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "MIX");

        migrationBuilder.AddColumn<double>(
            name: "OutflowWeight",
            table: "ScadaProcessTransfers",
            type: "float",
            nullable: false,
            defaultValue: 1.0);

        migrationBuilder.CreateTable(
            name: "ScadaProcessLocationFluids",
            columns: table => new
            {
                ProcessLocationFluidId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProcessLocationId = table.Column<int>(type: "int", nullable: false),
                FluidCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Volume = table.Column<double>(type: "float", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ScadaProcessLocationFluids", x => x.ProcessLocationFluidId);
                table.ForeignKey(
                    name: "FK_ScadaProcessLocationFluids_ScadaProcessLocations_ProcessLocationId",
                    column: x => x.ProcessLocationId,
                    principalTable: "ScadaProcessLocations",
                    principalColumn: "ProcessLocationId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ScadaProcessLocationFluids_ProcessLocationId_FluidCode",
            table: "ScadaProcessLocationFluids",
            columns: new[] { "ProcessLocationId", "FluidCode" },
            unique: true);

        migrationBuilder.Sql("""
            INSERT INTO ScadaProcessLocationFluids (ProcessLocationId, FluidCode, Volume)
            SELECT l.ProcessLocationId, N'MIX', l.CurrentVolume
            FROM ScadaProcessLocations l
            WHERE NOT EXISTS (
                SELECT 1 FROM ScadaProcessLocationFluids f WHERE f.ProcessLocationId = l.ProcessLocationId);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ScadaProcessLocationFluids");

        migrationBuilder.DropColumn(
            name: "FluidCode",
            table: "ScadaProcessTransfers");

        migrationBuilder.DropColumn(
            name: "OutflowWeight",
            table: "ScadaProcessTransfers");
    }
}
