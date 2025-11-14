# Migrating ASP.NET Identity from String to Guid IDs

This document explains how to apply the migration that converts ASP.NET Identity from string-based IDs to Guid-based IDs.

## Overview

The codebase has been updated to use `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>` which changes all Identity table primary keys from `text` to `uuid` in PostgreSQL.

## Migrations Created

1. **20251114204752_MigrateIdentityToGuid.cs** - Converts Identity tables (AspNetUsers, AspNetRoles, etc.)
2. **20251114204752_MigrateAccessProfileToGuid.cs** - Converts AccessControl tables (RoleAccessProfiles, UserAccessProfiles)

## Important Warnings

⚠️ **These migrations will fail if you have existing data with non-UUID string IDs.**

PostgreSQL cannot automatically cast arbitrary strings to UUIDs. You have three options:

### Option 1: Fresh Database (Recommended for Development)

If you don't need to preserve existing data:

```bash
# Drop and recreate the database
psql -U postgres -c "DROP DATABASE photobank;"
psql -U postgres -c "CREATE DATABASE photobank;"

# Apply all migrations from scratch
cd backend/PhotoBank.Api
dotnet ef database update --context PhotoBankDbContext
dotnet ef database update --context AccessControlDbContext
```

### Option 2: Existing Data with Valid UUID IDs

If your existing user/role IDs are already valid UUIDs (e.g., `"123e4567-e89b-12d3-a456-426614174000"`):

```bash
cd backend/PhotoBank.Api

# Apply Identity migration
dotnet ef database update --context PhotoBankDbContext

# Apply AccessControl migration
dotnet ef database update --context AccessControlDbContext
```

PostgreSQL will automatically cast the text UUIDs to the uuid type.

### Option 3: Existing Data with Non-UUID String IDs

If you have existing data with non-UUID IDs (e.g., arbitrary strings), you need a data migration:

#### Step 1: Create a mapping of old IDs to new UUIDs

```sql
-- Create temporary mapping tables
CREATE TABLE _user_id_mapping (
    old_id text PRIMARY KEY,
    new_id uuid NOT NULL DEFAULT gen_random_uuid()
);

CREATE TABLE _role_id_mapping (
    old_id text PRIMARY KEY,
    new_id uuid NOT NULL DEFAULT gen_random_uuid()
);

-- Populate mappings
INSERT INTO _user_id_mapping (old_id)
SELECT "Id" FROM "AspNetUsers";

INSERT INTO _role_id_mapping (old_id)
SELECT "Id" FROM "AspNetRoles";
```

#### Step 2: Create custom migration SQL

Instead of using the generated migrations, create custom SQL:

```sql
-- Start transaction
BEGIN;

-- Update AspNetUsers
ALTER TABLE "AspNetUsers" ADD COLUMN "NewId" uuid;
UPDATE "AspNetUsers" u SET "NewId" = m.new_id
FROM _user_id_mapping m WHERE u."Id" = m.old_id;

ALTER TABLE "AspNetUsers" DROP CONSTRAINT "PK_AspNetUsers";
ALTER TABLE "AspNetUsers" DROP COLUMN "Id";
ALTER TABLE "AspNetUsers" RENAME COLUMN "NewId" TO "Id";
ALTER TABLE "AspNetUsers" ADD PRIMARY KEY ("Id");

-- Update AspNetRoles (similar pattern)
ALTER TABLE "AspNetRoles" ADD COLUMN "NewId" uuid;
UPDATE "AspNetRoles" r SET "NewId" = m.new_id
FROM _role_id_mapping m WHERE r."Id" = m.old_id;

ALTER TABLE "AspNetRoles" DROP CONSTRAINT "PK_AspNetRoles";
ALTER TABLE "AspNetRoles" DROP COLUMN "Id";
ALTER TABLE "AspNetRoles" RENAME COLUMN "NewId" TO "Id";
ALTER TABLE "AspNetRoles" ADD PRIMARY KEY ("Id");

-- Update all foreign key references (AspNetUserRoles, AspNetUserClaims, etc.)
-- ... (repeat for each FK table)

-- Update AccessControl tables
ALTER TABLE "UserAccessProfiles" ADD COLUMN "NewUserId" uuid;
UPDATE "UserAccessProfiles" u SET "NewUserId" = m.new_id
FROM _user_id_mapping m WHERE u."UserId" = m.old_id;

ALTER TABLE "UserAccessProfiles" DROP CONSTRAINT "PK_UserAccessProfiles";
ALTER TABLE "UserAccessProfiles" DROP COLUMN "UserId";
ALTER TABLE "UserAccessProfiles" RENAME COLUMN "NewUserId" TO "UserId";
ALTER TABLE "UserAccessProfiles" ADD PRIMARY KEY ("UserId", "ProfileId");

-- Similar for RoleAccessProfiles
-- ...

-- Clean up mapping tables
DROP TABLE _user_id_mapping;
DROP TABLE _role_id_mapping;

-- Mark migrations as applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251114204752_MigrateIdentityToGuid', '9.0.9');

COMMIT;
```

#### Step 3: Update JWT tokens

After migration, all existing JWT tokens will be invalid because they contain the old string IDs. Users will need to log in again.

## Verifying the Migration

After applying migrations, verify the schema:

```sql
-- Check AspNetUsers.Id is uuid
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'AspNetUsers' AND column_name = 'Id';
-- Expected: uuid

-- Check AspNetRoles.Id is uuid
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'AspNetRoles' AND column_name = 'Id';
-- Expected: uuid

-- Check UserAccessProfiles.UserId is uuid
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'UserAccessProfiles' AND column_name = 'UserId';
-- Expected: uuid

-- Check RoleAccessProfiles.RoleId is uuid
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'RoleAccessProfiles' AND column_name = 'RoleId';
-- Expected: uuid
```

## Troubleshooting

### Error: "cannot cast type text to uuid"

This means you have existing data with non-UUID string IDs. Follow Option 3 above.

### Error: "duplicate key value violates unique constraint"

This can happen if UUID generation creates duplicates (extremely unlikely) or if your mapping logic is incorrect.

### Foreign Key Constraint Violations

Ensure you update all foreign keys in the correct order:
1. AspNetUsers.Id and AspNetRoles.Id (primary keys)
2. All tables referencing these keys (foreign keys)
3. AccessControl tables last

## Rolling Back

If something goes wrong, you can roll back:

```bash
cd backend/PhotoBank.Api

# Roll back both contexts
dotnet ef database update 20251103124346_Empty --context PhotoBankDbContext
dotnet ef database update 20251103124432_Empty --context AccessControlDbContext
```

Or restore from your database backup (you did create a backup first, right? 😅)
