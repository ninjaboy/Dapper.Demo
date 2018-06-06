DECLARE @CurrentMigration [nvarchar](max)

DECLARE @ContextName [nvarchar](max)
SELECT @ContextName = N'Dapper.Demo.Repositories'

IF object_id('[dbo].[__Migrations]') IS NOT NULL
    SELECT @CurrentMigration =
        (SELECT TOP (1)
        [Project1].[MigrationId] AS [MigrationId]
        FROM ( SELECT [Extent1].[MigrationId] AS [MigrationId]
            FROM [dbo].[__Migrations] AS [Extent1]
            WHERE [Extent1].[ContextKey] = @ContextName
        )  AS [Project1]
        ORDER BY [Project1].[MigrationId] DESC)
ELSE 
    CREATE TABLE [dbo].[__Migrations](
        [MigrationId] [nvarchar](150) NOT NULL,
        [ContextKey] [nvarchar](300) NOT NULL
    ) 

IF @CurrentMigration IS NULL
    SET @CurrentMigration = '0'

IF @CurrentMigration < '201805311134_InitialSetup'
BEGIN
   CREATE TABLE Users(
        UserId uniqueidentifier NOT NULL,
        Username nvarchar(256) NOT NULL, 
        Email nvarchar(256) NOT NULL, 
        PasswordHash nvarchar(512) NOT NULL, 
        DeactivatedOn date NULL, 
        GDPRSignedOn date NULL, 
        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (UserId ASC))

    CREATE TABLE UserRoles(
        UserId uniqueidentifier NOT NULL,
        RoleId uniqueidentifier NOT NULL)

    CREATE TABLE Roles(
        RoleId uniqueidentifier NOT NULL, 
        [Type] nvarchar(256) NOT NULL, 
        CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (RoleId ASC)) 

    ALTER TABLE [dbo].[UserRoles]  WITH CHECK ADD  CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY([RoleId])
        REFERENCES [dbo].[Roles] ([RoleId])

    ALTER TABLE [dbo].[UserRoles] CHECK CONSTRAINT [FK_UserRoles_Roles]

    ALTER TABLE [dbo].[UserRoles]  WITH CHECK ADD  CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY([UserId])
        REFERENCES [dbo].[Users] ([UserId])
        ON DELETE CASCADE

    ALTER TABLE [dbo].[UserRoles] CHECK CONSTRAINT [FK_UserRoles_Users]

    INSERT INTO __Migrations (MigrationId, ContextKey) VALUES ('201805311134_InitialSetup', @ContextName)
END

IF @CurrentMigration < '201805311206_ConcurrencyExample'
BEGIN
    ALTER TABLE dbo.Users ADD
        ConcurrencyToken rowversion NOT NULL

    INSERT INTO __Migrations (MigrationId, ContextKey) VALUES ('201805311206_ConcurrencyExample', @ContextName)
END