/*
  Use this when EF migrations fail part-way: SQL Server may have tables but no row in __EFMigrationsHistory,
  so MigrateAsync() keeps re-running CREATE TABLE and errors on "already an object named ...".

  Run with sqlcmd or Azure Data Studio against your dev instance, then start the API again.

  Example:
    sqlcmd -S localhost,1433 -U sa -P 'yourStrong(!)Password' -i ResetDevDatabase.sql
*/

USE [SCADASampleDB];
GO

/* App tables (dependency order) */
IF OBJECT_ID(N'[dbo].[ScadaProcessTransfers]', N'U') IS NOT NULL DROP TABLE [dbo].[ScadaProcessTransfers];
IF OBJECT_ID(N'[dbo].[ScadaProcessLocations]', N'U') IS NOT NULL DROP TABLE [dbo].[ScadaProcessLocations];
IF OBJECT_ID(N'[dbo].[ScadaAlarms]', N'U') IS NOT NULL DROP TABLE [dbo].[ScadaAlarms];
IF OBJECT_ID(N'[dbo].[Alarms]', N'U') IS NOT NULL DROP TABLE [dbo].[Alarms];
IF OBJECT_ID(N'[dbo].[ScadaTags]', N'U') IS NOT NULL DROP TABLE [dbo].[ScadaTags];
IF OBJECT_ID(N'[dbo].[Tags]', N'U') IS NOT NULL DROP TABLE [dbo].[Tags];
IF OBJECT_ID(N'[dbo].[Pipelines]', N'U') IS NOT NULL DROP TABLE [dbo].[Pipelines];

/* Identity */
IF OBJECT_ID(N'[dbo].[AspNetRoleClaims]', N'U') IS NOT NULL DROP TABLE [dbo].[AspNetRoleClaims];
IF OBJECT_ID(N'[dbo].[AspNetUserClaims]', N'U') IS NOT NULL DROP TABLE [dbo].[AspNetUserClaims];
IF OBJECT_ID(N'[dbo].[AspNetUserLogins]', N'U') IS NOT NULL DROP TABLE [dbo].[AspNetUserLogins];
IF OBJECT_ID(N'[dbo].[AspNetUserRoles]', N'U') IS NOT NULL DROP TABLE [dbo].[AspNetUserRoles];
IF OBJECT_ID(N'[dbo].[AspNetUserTokens]', N'U') IS NOT NULL DROP TABLE [dbo].[AspNetUserTokens];
IF OBJECT_ID(N'[dbo].[AspNetRoles]', N'U') IS NOT NULL DROP TABLE [dbo].[AspNetRoles];
IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NOT NULL DROP TABLE [dbo].[AspNetUsers];

IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NOT NULL DROP TABLE [dbo].[__EFMigrationsHistory];
GO

PRINT 'SCADASampleDB app tables dropped. Run the API to apply migrations from a clean state.';
GO
