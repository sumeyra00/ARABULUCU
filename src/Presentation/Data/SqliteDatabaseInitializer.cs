using System.Data.Common;
using ARABULUCU.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace ARABULUCU.Web.Data;

public static class SqliteDatabaseInitializer
{
    private static readonly string[] IdentitySchema = [
        """
        CREATE TABLE IF NOT EXISTS "AspNetRoles" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetRoles" PRIMARY KEY,
            "Name" TEXT NULL,
            "NormalizedName" TEXT NULL,
            "ConcurrencyStamp" TEXT NULL
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS "AspNetUsers" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetUsers" PRIMARY KEY,
            "UserName" TEXT NULL,
            "NormalizedUserName" TEXT NULL,
            "Email" TEXT NULL,
            "NormalizedEmail" TEXT NULL,
            "EmailConfirmed" INTEGER NOT NULL,
            "PasswordHash" TEXT NULL,
            "SecurityStamp" TEXT NULL,
            "ConcurrencyStamp" TEXT NULL,
            "PhoneNumber" TEXT NULL,
            "PhoneNumberConfirmed" INTEGER NOT NULL,
            "TwoFactorEnabled" INTEGER NOT NULL,
            "LockoutEnd" TEXT NULL,
            "LockoutEnabled" INTEGER NOT NULL,
            "AccessFailedCount" INTEGER NOT NULL
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS "AspNetRoleClaims" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY AUTOINCREMENT,
            "RoleId" TEXT NOT NULL,
            "ClaimType" TEXT NULL,
            "ClaimValue" TEXT NULL,
            CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId"
                FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS "AspNetUserClaims" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY AUTOINCREMENT,
            "UserId" TEXT NOT NULL,
            "ClaimType" TEXT NULL,
            "ClaimValue" TEXT NULL,
            CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId"
                FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS "AspNetUserLogins" (
            "LoginProvider" TEXT NOT NULL,
            "ProviderKey" TEXT NOT NULL,
            "ProviderDisplayName" TEXT NULL,
            "UserId" TEXT NOT NULL,
            CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
            CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId"
                FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS "AspNetUserRoles" (
            "UserId" TEXT NOT NULL,
            "RoleId" TEXT NOT NULL,
            CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
            CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId"
                FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
            CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId"
                FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS "AspNetUserTokens" (
            "UserId" TEXT NOT NULL,
            "LoginProvider" TEXT NOT NULL,
            "Name" TEXT NOT NULL,
            "Value" TEXT NULL,
            CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
            CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId"
                FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
        )
        """,
        """CREATE INDEX IF NOT EXISTS "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId")""",
        """CREATE UNIQUE INDEX IF NOT EXISTS "RoleNameIndex" ON "AspNetRoles" ("NormalizedName")""",
        """CREATE INDEX IF NOT EXISTS "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId")""",
        """CREATE INDEX IF NOT EXISTS "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId")""",
        """CREATE INDEX IF NOT EXISTS "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId")""",
        """CREATE INDEX IF NOT EXISTS "EmailIndex" ON "AspNetUsers" ("NormalizedEmail")""",
        """CREATE UNIQUE INDEX IF NOT EXISTS "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName")"""
    ];

    private static readonly string[] ApplicationSchema = [
        """
        CREATE TABLE IF NOT EXISTS "Categories" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_Categories" PRIMARY KEY AUTOINCREMENT,
            "Name" TEXT NOT NULL
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS "ContactMessages" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_ContactMessages" PRIMARY KEY AUTOINCREMENT,
            "LostItemId" INTEGER NOT NULL,
            "FromUserId" TEXT NOT NULL,
            "Message" TEXT NOT NULL,
            "SentAt" TEXT NOT NULL
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS "LostItemFeatures" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_LostItemFeatures" PRIMARY KEY AUTOINCREMENT,
            "LostItemId" INTEGER NOT NULL,
            "Key" TEXT NOT NULL,
            "Value" TEXT NOT NULL,
            "IsHidden" INTEGER NOT NULL
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS "UserProfiles" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_UserProfiles" PRIMARY KEY,
            "DisplayName" TEXT NOT NULL,
            "Email" TEXT NOT NULL
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS "LostItems" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_LostItems" PRIMARY KEY AUTOINCREMENT,
            "Title" TEXT NOT NULL,
            "Description" TEXT NOT NULL,
            "ImagePath" TEXT NULL,
            "Location" TEXT NOT NULL,
            "CreatedAt" TEXT NOT NULL,
            "IsFound" INTEGER NOT NULL,
            "UserId" TEXT NOT NULL,
            "CategoryId" INTEGER NOT NULL,
            CONSTRAINT "FK_LostItems_Categories_CategoryId"
                FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE RESTRICT,
            CONSTRAINT "FK_LostItems_UserProfiles_UserId"
                FOREIGN KEY ("UserId") REFERENCES "UserProfiles" ("Id") ON DELETE RESTRICT
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS "Notifications" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_Notifications" PRIMARY KEY AUTOINCREMENT,
            "UserId" TEXT NOT NULL,
            "Message" TEXT NOT NULL,
            "IsRead" INTEGER NOT NULL,
            "CreatedAt" TEXT NOT NULL,
            "LostItemId" INTEGER NULL,
            CONSTRAINT "FK_Notifications_LostItems_LostItemId"
                FOREIGN KEY ("LostItemId") REFERENCES "LostItems" ("Id"),
            CONSTRAINT "FK_Notifications_UserProfiles_UserId"
                FOREIGN KEY ("UserId") REFERENCES "UserProfiles" ("Id") ON DELETE CASCADE
        )
        """,
        """CREATE INDEX IF NOT EXISTS "IX_LostItems_CategoryId" ON "LostItems" ("CategoryId")""",
        """CREATE INDEX IF NOT EXISTS "IX_LostItems_UserId" ON "LostItems" ("UserId")""",
        """CREATE INDEX IF NOT EXISTS "IX_Notifications_LostItemId" ON "Notifications" ("LostItemId")""",
        """CREATE INDEX IF NOT EXISTS "IX_Notifications_UserId" ON "Notifications" ("UserId")""",
        """INSERT OR IGNORE INTO "Categories" ("Id", "Name") VALUES (1, 'Cüzdan')""",
        """INSERT OR IGNORE INTO "Categories" ("Id", "Name") VALUES (2, 'Anahtar')""",
        """INSERT OR IGNORE INTO "Categories" ("Id", "Name") VALUES (3, 'Telefon')""",
        """INSERT OR IGNORE INTO "Categories" ("Id", "Name") VALUES (4, 'Kulaklık')"""
    ];

    public static async Task InitializeAsync(ApplicationDbContext dbContext)
    {
        var migrations = dbContext.Database.GetMigrations().ToArray();
        await dbContext.Database.OpenConnectionAsync();

        try
        {
            var connection = dbContext.Database.GetDbConnection();
            var tableNames = await GetTableNamesAsync(connection);
            var hasLegacyApplicationSchema = tableNames.Contains("Categories")
                || tableNames.Contains("LostItems")
                || tableNames.Contains("UserProfiles")
                || tableNames.Contains("Notifications");

            if (hasLegacyApplicationSchema && migrations.Length > 0)
            {
                var migrationHistory = await GetAppliedMigrationIdsAsync(connection, tableNames);
                var initialMigration = migrations[0];

                if (!migrationHistory.Contains(initialMigration))
                {
                    await using var transaction = await dbContext.Database.BeginTransactionAsync();
                    foreach (var statement in IdentitySchema)
                    {
                        await dbContext.Database.ExecuteSqlRawAsync(statement);
                    }

                    await dbContext.Database.ExecuteSqlRawAsync(
                        """
                        CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                            "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                            "ProductVersion" TEXT NOT NULL
                        )
                        """);

                    var productVersion = typeof(DbContext).Assembly.GetName().Version?.ToString(3)
                        ?? throw new InvalidOperationException("Could not determine the EF Core product version.");
                    await dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({initialMigration}, {productVersion})");
                    await transaction.CommitAsync();
                }
            }
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }

        await dbContext.Database.MigrateAsync();
        await EnsureApplicationSchemaAsync(dbContext);
    }

    private static async Task EnsureApplicationSchemaAsync(ApplicationDbContext dbContext)
    {
        foreach (var statement in ApplicationSchema)
        {
            await dbContext.Database.ExecuteSqlRawAsync(statement);
        }
    }

    private static async Task<HashSet<string>> GetTableNamesAsync(DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """SELECT "name" FROM "sqlite_master" WHERE "type" = 'table'""";
        await using var reader = await command.ExecuteReaderAsync();
        var names = new HashSet<string>(StringComparer.Ordinal);
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private static async Task<HashSet<string>> GetAppliedMigrationIdsAsync(
        DbConnection connection,
        HashSet<string> tableNames)
    {
        var migrationIds = new HashSet<string>(StringComparer.Ordinal);
        if (!tableNames.Contains("__EFMigrationsHistory"))
        {
            return migrationIds;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """SELECT "MigrationId" FROM "__EFMigrationsHistory" """;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            migrationIds.Add(reader.GetString(0));
        }

        return migrationIds;
    }
}
