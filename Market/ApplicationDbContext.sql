IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(128) NOT NULL,
        [ProviderKey] nvarchar(128) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(128) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE TABLE [Property] (
        [Id] int NOT NULL IDENTITY,
        [Address] nvarchar(max) NOT NULL,
        [RentPrice] decimal(18,2) NOT NULL,
        [Size] int NOT NULL,
        [Description] nvarchar(max) NULL,
        [IsAvailable] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedUtc] datetime2 NULL,
        [UserId] nvarchar(450) NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [UpdatedUtc] datetime2 NULL,
        [ApprovalStatus] int NOT NULL,
        [ModerationNote] nvarchar(max) NULL,
        [ApprovedUtc] datetime2 NULL,
        [ApprovedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_Property] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Property_AspNetUsers_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Property_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE TABLE [UsersViewModel] (
        [Id] int NOT NULL IDENTITY,
        [UserName] nvarchar(max) NOT NULL,
        [Password] nvarchar(100) NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [Roles] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(max) NOT NULL,
        [SelectedRole] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_UsersViewModel] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UsersViewModel_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE TABLE [MaintenanceRequest] (
        [Id] int NOT NULL IDENTITY,
        [PropertyId] int NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_MaintenanceRequest] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MaintenanceRequest_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MaintenanceRequest_Property_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Property] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE TABLE [PropertyImage] (
        [Id] int NOT NULL IDENTITY,
        [PropertyId] int NOT NULL,
        [Url] nvarchar(300) NOT NULL,
        [ThumbUrl] nvarchar(300) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_PropertyImage] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PropertyImage_Property_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Property] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE TABLE [RentalAgreement] (
        [Id] int NOT NULL IDENTITY,
        [PropertyId] int NOT NULL,
        [TenantId] nvarchar(450) NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NULL,
        [MonthlyRent] decimal(18,2) NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_RentalAgreement] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RentalAgreement_AspNetUsers_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RentalAgreement_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RentalAgreement_Property_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Property] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE TABLE [RentalRequest] (
        [Id] int NOT NULL IDENTITY,
        [PropertyId] int NOT NULL,
        [RequesterId] nvarchar(450) NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [Status] int NOT NULL,
        [OwnerDecisionNote] nvarchar(max) NULL,
        CONSTRAINT [PK_RentalRequest] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RentalRequest_AspNetUsers_RequesterId] FOREIGN KEY ([RequesterId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RentalRequest_Property_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Property] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE TABLE [Payment] (
        [Id] int NOT NULL IDENTITY,
        [PropertyId] int NOT NULL,
        [RentalAgreementId] int NOT NULL,
        [TenantId] nvarchar(450) NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Currency] nvarchar(3) NOT NULL,
        [DueDate] date NOT NULL,
        [PaidUtc] datetime2 NULL,
        [Status] int NOT NULL,
        [Reference] nvarchar(80) NULL,
        [Title] nvarchar(240) NULL,
        CONSTRAINT [PK_Payment] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payment_AspNetUsers_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payment_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payment_Property_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Property] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payment_RentalAgreement_RentalAgreementId] FOREIGN KEY ([RentalAgreementId]) REFERENCES [RentalAgreement] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_MaintenanceRequest_PropertyId] ON [MaintenanceRequest] ([PropertyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_MaintenanceRequest_UserId] ON [MaintenanceRequest] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_Payment_PropertyId] ON [Payment] ([PropertyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_Payment_Reference] ON [Payment] ([Reference]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_Payment_RentalAgreementId_Status] ON [Payment] ([RentalAgreementId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_Payment_TenantId] ON [Payment] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_Payment_UserId] ON [Payment] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_Property_ApprovalStatus_IsDeleted] ON [Property] ([ApprovalStatus], [IsDeleted]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_Property_ApprovedByUserId] ON [Property] ([ApprovedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_Property_UserId] ON [Property] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_PropertyImage_PropertyId_SortOrder] ON [PropertyImage] ([PropertyId], [SortOrder]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_RentalAgreement_PropertyId_StartDate_EndDate] ON [RentalAgreement] ([PropertyId], [StartDate], [EndDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_RentalAgreement_TenantId] ON [RentalAgreement] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_RentalAgreement_UserId] ON [RentalAgreement] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_RentalRequest_PropertyId_StartDate_EndDate] ON [RentalRequest] ([PropertyId], [StartDate], [EndDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_RentalRequest_PropertyId_Status] ON [RentalRequest] ([PropertyId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_RentalRequest_RequesterId] ON [RentalRequest] ([RequesterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_RentalRequest_Pending_PerUser] ON [RentalRequest] ([PropertyId], [RequesterId]) WHERE [Status] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    CREATE INDEX [IX_UsersViewModel_UserId] ON [UsersViewModel] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008090714_Init'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251008090714_Init', N'8.0.16');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008092649_Udogodnienia'
)
BEGIN
    ALTER TABLE [Property] ADD [AmenitiesNote] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008092649_Udogodnienia'
)
BEGIN
    ALTER TABLE [Property] ADD [HasBalcony] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008092649_Udogodnienia'
)
BEGIN
    ALTER TABLE [Property] ADD [HasPrivateBathroom] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008092649_Udogodnienia'
)
BEGIN
    ALTER TABLE [Property] ADD [HasWifi] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251008092649_Udogodnienia'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251008092649_Udogodnienia', N'8.0.16');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016164545_Nowa'
)
BEGIN
    CREATE TABLE [UserVerification] (
        [UserId] nvarchar(450) NOT NULL,
        [Status] int NOT NULL,
        [LastRequestId] uniqueidentifier NULL,
        CONSTRAINT [PK_UserVerification] PRIMARY KEY ([UserId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016164545_Nowa'
)
BEGIN
    CREATE TABLE [VerificationRequests] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [FrontPath] nvarchar(max) NOT NULL,
        [BackPath] nvarchar(max) NOT NULL,
        [MimeFront] nvarchar(max) NULL,
        [MimeBack] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [SubmittedUtc] datetime2 NOT NULL,
        [ReviewedUtc] datetime2 NULL,
        [ReviewedById] nvarchar(max) NULL,
        [RejectReason] nvarchar(max) NULL,
        CONSTRAINT [PK_VerificationRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VerificationRequests_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016164545_Nowa'
)
BEGIN
    CREATE INDEX [IX_VerificationRequests_UserId] ON [VerificationRequests] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016164545_Nowa'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251016164545_Nowa', N'8.0.16');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016181127_Zmiany'
)
BEGIN
    CREATE TABLE [Conversations] (
        [Id] uniqueidentifier NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Conversations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016181127_Zmiany'
)
BEGIN
    CREATE TABLE [ConversationMembers] (
        [ConversationId] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [LastReadUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ConversationMembers] PRIMARY KEY ([ConversationId], [UserId]),
        CONSTRAINT [FK_ConversationMembers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ConversationMembers_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016181127_Zmiany'
)
BEGIN
    CREATE TABLE [Messages] (
        [Id] uniqueidentifier NOT NULL,
        [ConversationId] uniqueidentifier NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [SenderId] nvarchar(450) NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Messages_AspNetUsers_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Messages_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016181127_Zmiany'
)
BEGIN
    CREATE INDEX [IX_ConversationMembers_UserId] ON [ConversationMembers] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016181127_Zmiany'
)
BEGIN
    CREATE INDEX [IX_Messages_ConversationId_CreatedUtc] ON [Messages] ([ConversationId], [CreatedUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016181127_Zmiany'
)
BEGIN
    CREATE INDEX [IX_Messages_SenderId] ON [Messages] ([SenderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251016181127_Zmiany'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251016181127_Zmiany', N'8.0.16');
END;
GO

COMMIT;
GO