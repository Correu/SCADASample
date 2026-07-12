using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SCADASampleAPI.Data;

#nullable disable

namespace SCADASampleAPI.Data.Migrations;

/// <summary>
/// Repair migration: ensures all schema changes from 20260711000000_StationsProductsMultiFluid
/// are physically present in the database. Every statement is idempotent so this migration is
/// safe to re-run regardless of the current partial state of the database.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260712000000_FixSchemaInconsistency")]
public partial class FixSchemaInconsistency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- Products catalog
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

        migrationBuilder.Sql("""
            -- Station groupings
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
                        FOREIGN KEY ([PipelineId]) REFERENCES [Pipelines]([PipelineId])
                        ON DELETE CASCADE
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

        migrationBuilder.Sql("""
            -- StationId nullable column on process locations
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

        migrationBuilder.Sql("""
            -- Per-fluid rows table on transfer legs
            IF OBJECT_ID(N'[ScadaProcessTransferFluids]', N'U') IS NULL
            BEGIN
                CREATE TABLE [ScadaProcessTransferFluids] (
                    [ProcessTransferFluidId] int          NOT NULL IDENTITY(1,1),
                    [ProcessTransferId]      int          NOT NULL,
                    [FluidCode]              nvarchar(32) NOT NULL,
                    [FlowRateFraction]       float        NOT NULL,
                    [OutflowWeight]          float        NOT NULL,
                    [CurrentFlowRate]        float        NOT NULL,
                    CONSTRAINT [PK_ScadaProcessTransferFluids]
                        PRIMARY KEY ([ProcessTransferFluidId]),
                    CONSTRAINT [FK_ScadaProcessTransferFluids_ScadaProcessTransfers_ProcessTransferId]
                        FOREIGN KEY ([ProcessTransferId])
                        REFERENCES [ScadaProcessTransfers]([ProcessTransferId])
                        ON DELETE CASCADE
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

        migrationBuilder.Sql("""
            -- Migrate existing scalar FluidCode / OutflowWeight data, then drop the old columns.
            -- Must use dynamic SQL: SQL Server validates column names at batch compile time,
            -- so static IF COL_LENGTH gates still fail when the columns are already gone.
            IF COL_LENGTH(N'ScadaProcessTransfers', N'FluidCode') IS NOT NULL
            BEGIN
                DECLARE @migrateFluids NVARCHAR(MAX) = N'
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

                    DECLARE @df_fluid NVARCHAR(256);
                    SELECT @df_fluid = d.[name]
                    FROM sys.default_constraints d
                    JOIN sys.columns c
                      ON d.parent_object_id = c.object_id
                     AND d.parent_column_id = c.column_id
                    WHERE c.object_id = OBJECT_ID(N''ScadaProcessTransfers'')
                      AND c.[name] = N''FluidCode'';
                    IF @df_fluid IS NOT NULL
                        EXEC(N''ALTER TABLE [ScadaProcessTransfers] DROP CONSTRAINT ['' + @df_fluid + N'']'');

                    ALTER TABLE [ScadaProcessTransfers] DROP COLUMN [FluidCode];
                ';
                EXEC sp_executesql @migrateFluids;
            END

            IF COL_LENGTH(N'ScadaProcessTransfers', N'OutflowWeight') IS NOT NULL
            BEGIN
                DECLARE @dropWeight NVARCHAR(MAX) = N'
                    DECLARE @df_weight NVARCHAR(256);
                    SELECT @df_weight = d.[name]
                    FROM sys.default_constraints d
                    JOIN sys.columns c
                      ON d.parent_object_id = c.object_id
                     AND d.parent_column_id = c.column_id
                    WHERE c.object_id = OBJECT_ID(N''ScadaProcessTransfers'')
                      AND c.[name] = N''OutflowWeight'';
                    IF @df_weight IS NOT NULL
                        EXEC(N''ALTER TABLE [ScadaProcessTransfers] DROP CONSTRAINT ['' + @df_weight + N'']'');

                    ALTER TABLE [ScadaProcessTransfers] DROP COLUMN [OutflowWeight];
                ';
                EXEC sp_executesql @dropWeight;
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This was a repair migration — no rollback is meaningful.
    }
}
