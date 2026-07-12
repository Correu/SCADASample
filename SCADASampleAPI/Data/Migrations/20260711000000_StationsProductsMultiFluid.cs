using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SCADASampleAPI.Data;

#nullable disable

namespace SCADASampleAPI.Data.Migrations;

/// <inheritdoc />
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260711000000_StationsProductsMultiFluid")]
public partial class StationsProductsMultiFluid : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // All operations are idempotent (IF NOT EXISTS / IF COL_LENGTH checks) so this
        // migration can be safely re-applied after a partial failure.

        // --- Products catalog ---
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[Products]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Products] (
                    [Code]        nvarchar(32)  NOT NULL,
                    [Name]        nvarchar(128) NOT NULL,
                    [HexColor]    nvarchar(16)  NOT NULL,
                    [ProductType] nvarchar(32)  NOT NULL,
                    [Density]     float         NOT NULL,
                    [IsActive]    bit           NOT NULL,
                    CONSTRAINT [PK_Products] PRIMARY KEY ([Code])
                );
            END
            """);

        // --- Station groupings ---
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[ScadaStations]', N'U') IS NULL
            BEGIN
                CREATE TABLE [ScadaStations] (
                    [StationId]  int           NOT NULL IDENTITY(1,1),
                    [PipelineId] int           NOT NULL,
                    [Name]       nvarchar(128) NOT NULL,
                    [Code]       nvarchar(32)  NOT NULL,
                    [LayoutX]    float         NOT NULL,
                    [LayoutY]    float         NOT NULL,
                    CONSTRAINT [PK_ScadaStations] PRIMARY KEY ([StationId]),
                    CONSTRAINT [FK_ScadaStations_Pipelines_PipelineId]
                        FOREIGN KEY ([PipelineId]) REFERENCES [Pipelines]([PipelineId]) ON DELETE CASCADE
                );
            END
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = N'IX_ScadaStations_PipelineId'
                  AND object_id = OBJECT_ID(N'[ScadaStations]'))
            BEGIN
                CREATE INDEX [IX_ScadaStations_PipelineId] ON [ScadaStations] ([PipelineId]);
            END
            """);

        // --- StationId nullable FK on process locations ---
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'ScadaProcessLocations', N'StationId') IS NULL
            BEGIN
                ALTER TABLE [ScadaProcessLocations] ADD [StationId] int NULL;
            END
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = N'IX_ScadaProcessLocations_StationId'
                  AND object_id = OBJECT_ID(N'[ScadaProcessLocations]'))
            BEGIN
                CREATE INDEX [IX_ScadaProcessLocations_StationId]
                    ON [ScadaProcessLocations] ([StationId]);
            END
            IF NOT EXISTS (
                SELECT 1 FROM sys.foreign_keys
                WHERE name = N'FK_ScadaProcessLocations_ScadaStations_StationId')
            BEGIN
                ALTER TABLE [ScadaProcessLocations]
                ADD CONSTRAINT [FK_ScadaProcessLocations_ScadaStations_StationId]
                    FOREIGN KEY ([StationId]) REFERENCES [ScadaStations]([StationId])
                    ON DELETE NO ACTION;
            END
            """);

        // --- Per-fluid rows on transfer legs ---
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[ScadaProcessTransferFluids]', N'U') IS NULL
            BEGIN
                CREATE TABLE [ScadaProcessTransferFluids] (
                    [ProcessTransferFluidId] int          NOT NULL IDENTITY(1,1),
                    [ProcessTransferId]      int          NOT NULL,
                    [FluidCode]              nvarchar(32) NOT NULL,
                    [FlowRateFraction]       float        NOT NULL,
                    [OutflowWeight]          float        NOT NULL,
                    [CurrentFlowRate]        float        NOT NULL,
                    CONSTRAINT [PK_ScadaProcessTransferFluids] PRIMARY KEY ([ProcessTransferFluidId]),
                    CONSTRAINT [FK_ScadaProcessTransferFluids_ScadaProcessTransfers_ProcessTransferId]
                        FOREIGN KEY ([ProcessTransferId])
                        REFERENCES [ScadaProcessTransfers]([ProcessTransferId]) ON DELETE CASCADE
                );
            END
            IF NOT EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = N'IX_ScadaProcessTransferFluids_ProcessTransferId_FluidCode'
                  AND object_id = OBJECT_ID(N'[ScadaProcessTransferFluids]'))
            BEGIN
                CREATE UNIQUE INDEX [IX_ScadaProcessTransferFluids_ProcessTransferId_FluidCode]
                    ON [ScadaProcessTransferFluids] ([ProcessTransferId], [FluidCode]);
            END
            """);

        // --- Migrate existing FluidCode/OutflowWeight data before dropping scalar columns ---
        // The entire block is gated on the source columns still existing so it is safe to
        // re-run after a partial failure.
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'ScadaProcessTransfers', N'FluidCode') IS NOT NULL
            BEGIN
                -- Copy existing scalar fluid data into the per-fluid table (skip if already copied)
                INSERT INTO [ScadaProcessTransferFluids]
                    ([ProcessTransferId], [FluidCode], [FlowRateFraction], [OutflowWeight], [CurrentFlowRate])
                SELECT
                    [ProcessTransferId],
                    [FluidCode],
                    1.0,
                    ISNULL([OutflowWeight], 1.0),
                    0.0
                FROM [ScadaProcessTransfers]
                WHERE NOT EXISTS (
                    SELECT 1 FROM [ScadaProcessTransferFluids] f
                    WHERE f.[ProcessTransferId] = [ScadaProcessTransfers].[ProcessTransferId]);

                -- Drop auto-generated DEFAULT constraint on FluidCode (required before DROP COLUMN)
                DECLARE @df_fluid NVARCHAR(256);
                SELECT @df_fluid = d.[name]
                FROM sys.default_constraints d
                JOIN sys.columns c
                  ON d.parent_object_id = c.object_id
                 AND d.parent_column_id = c.column_id
                WHERE c.object_id = OBJECT_ID(N'ScadaProcessTransfers')
                  AND c.[name] = N'FluidCode';
                IF @df_fluid IS NOT NULL
                    EXEC(N'ALTER TABLE [ScadaProcessTransfers] DROP CONSTRAINT [' + @df_fluid + N']');

                ALTER TABLE [ScadaProcessTransfers] DROP COLUMN [FluidCode];
            END

            IF COL_LENGTH(N'ScadaProcessTransfers', N'OutflowWeight') IS NOT NULL
            BEGIN
                -- Drop auto-generated DEFAULT constraint on OutflowWeight if present
                DECLARE @df_weight NVARCHAR(256);
                SELECT @df_weight = d.[name]
                FROM sys.default_constraints d
                JOIN sys.columns c
                  ON d.parent_object_id = c.object_id
                 AND d.parent_column_id = c.column_id
                WHERE c.object_id = OBJECT_ID(N'ScadaProcessTransfers')
                  AND c.[name] = N'OutflowWeight';
                IF @df_weight IS NOT NULL
                    EXEC(N'ALTER TABLE [ScadaProcessTransfers] DROP CONSTRAINT [' + @df_weight + N']');

                ALTER TABLE [ScadaProcessTransfers] DROP COLUMN [OutflowWeight];
            END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Restore scalar columns first
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

        // Restore primary fluid back to scalar columns (take first fluid per transfer)
        migrationBuilder.Sql("""
            UPDATE t
            SET t.[FluidCode]    = f.[FluidCode],
                t.[OutflowWeight] = f.[OutflowWeight]
            FROM [ScadaProcessTransfers] t
            INNER JOIN (
                SELECT [ProcessTransferId],
                       MIN([FluidCode])    AS [FluidCode],
                       MAX([OutflowWeight]) AS [OutflowWeight]
                FROM [ScadaProcessTransferFluids]
                GROUP BY [ProcessTransferId]
            ) f ON f.[ProcessTransferId] = t.[ProcessTransferId];
            """);

        migrationBuilder.DropForeignKey(
            name: "FK_ScadaProcessLocations_ScadaStations_StationId",
            table: "ScadaProcessLocations");

        migrationBuilder.DropIndex(
            name: "IX_ScadaProcessLocations_StationId",
            table: "ScadaProcessLocations");

        migrationBuilder.DropColumn(
            name: "StationId",
            table: "ScadaProcessLocations");

        migrationBuilder.DropTable(name: "ScadaProcessTransferFluids");
        migrationBuilder.DropTable(name: "ScadaStations");
        migrationBuilder.DropTable(name: "Products");
    }
}
