# Photo Deletion Command

The `delete` command allows you to delete a photo by ID, including all related database records and S3 objects (preview and thumbnail images).

## Usage

```bash
# Interactive deletion (prompts for confirmation)
dotnet run --project PhotoBank.Console -- delete --photo-id 123

# Auto-confirm deletion (no prompt)
dotnet run --project PhotoBank.Console -- delete --photo-id 123 --confirm
```

## Command Options

| Option | Alias | Description | Required |
|--------|-------|-------------|----------|
| `--photo-id` | `-p` | Photo ID to delete | Yes |
| `--confirm` | `-y` | Skip confirmation prompt | No |

## What Gets Deleted

When you delete a photo, the command removes:

### Database Records (in order to avoid FK violations):
1. **Captions** - Image descriptions/captions
2. **PhotoTags** - Photo-to-Tag relationships
3. **PhotoCategories** - Photo-to-Category relationships
4. **ObjectProperties** - Detected objects in the photo
5. **Faces** - Detected faces in the photo
6. **Files** - Physical file references
7. **Photos** - The photo record itself

### S3 Objects:
- **Preview Image** - Preview version stored in S3 (if exists)
- **Thumbnail Image** - Thumbnail version stored in S3 (if exists)

## Example Output

### Interactive Mode (with confirmation):
```bash
$ dotnet run --project PhotoBank.Console -- delete --photo-id 123

Preparing to delete photo ID: 123

Are you sure you want to delete this photo? This action cannot be undone. (yes/no): yes

Deleting photo...

Photo ID: 123
Status: SUCCESS

S3 Objects:
  Preview: Deleted
  Thumbnail: Deleted

Database Records:
  Captions: 2
  PhotoTags: 5
  PhotoCategories: 2
  ObjectProperties: 8
  Faces: 3
  Files: 1
  Photo: Deleted
```

### Auto-confirm Mode:
```bash
$ dotnet run --project PhotoBank.Console -- delete --photo-id 123 --confirm

Preparing to delete photo ID: 123

Deleting photo...

Photo ID: 123
Status: SUCCESS

S3 Objects:
  Preview: Deleted
  Thumbnail: Deleted

Database Records:
  Captions: 2
  PhotoTags: 5
  PhotoCategories: 2
  ObjectProperties: 8
  Faces: 3
  Files: 1
  Photo: Deleted
```

### Photo Not Found:
```bash
$ dotnet run --project PhotoBank.Console -- delete --photo-id 999 --confirm

Preparing to delete photo ID: 999

Deleting photo...

Photo ID: 999
Status: FAILED
Error: Photo with ID 999 not found
```

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success or user cancelled |
| `1` | Deletion failed |
| `130` | Cancelled by Ctrl+C |

## Safety Features

- **Confirmation Prompt**: By default, the command asks for confirmation before deletion
- **Transaction Safety**: Database deletions are executed in correct order to avoid FK violations
- **Graceful Error Handling**: If S3 deletion fails, database deletion still proceeds (with warning logged)
- **Photo Validation**: Checks if photo exists before attempting deletion

## Implementation Details

### Service Layer

The deletion is implemented in `PhotoDeletionService` which:
1. Retrieves photo metadata (including S3 keys)
2. Deletes S3 objects (preview and thumbnail)
3. Deletes database records using raw SQL for efficiency
4. Returns detailed result with counts of deleted records

### S3 Deletion

- Uses `MinioObjectService.DeleteObjectAsync()` to remove objects
- Checks if object exists before deletion attempt
- Non-fatal: If S3 deletion fails, the operation continues (with logged warning)

### Database Deletion

Uses raw SQL (`ExecuteSqlRawAsync`) for efficiency:
- Deletes records in correct order to avoid FK constraint violations
- Returns row counts for each table
- All deletions are executed within the DbContext transaction

## Error Handling

If deletion fails:
- Database transaction is rolled back (if using transactions)
- Error message is displayed to console
- Exit code `1` is returned
- Full error is logged to log file

## Related Files

- **Service**: `PhotoBank.Services/Photos/PhotoDeletionService.cs`
- **S3 Service**: `PhotoBank.Services/MinioObjectService.cs`
- **Command Handler**: `PhotoBank.Console/Program.cs` (BuildDeleteCommand, RunDeleteAsync)
- **DI Registration**: `PhotoBank.DependencyInjection/AddPhotobankCoreExtensions.cs`

## Logging

All operations are logged to:
- Console (INFO level and above)
- Log file: `logs/photobank-{date}.log`

Example log entries:
```
[INF] Deleting photo 123
[DBG] Deleted preview: preview/123.jpg
[DBG] Deleted thumbnail: thumbnail/123.jpg
[DBG] Deleted database records for photo 123: Captions=2, Tags=5, Categories=2, Properties=8, Faces=3, Files=1
[INF] Successfully deleted photo 123
```

## Alternative: SQL Scripts

For bulk deletions or database-only operations, see:
- `delete_photo_by_id.sql` - One-time SQL script
- `create_delete_photo_procedure.sql` - Reusable PostgreSQL function
- `DELETE_PHOTO_README.md` - SQL scripts documentation

**Note**: SQL scripts do NOT delete S3 objects - use this command for complete deletion.
