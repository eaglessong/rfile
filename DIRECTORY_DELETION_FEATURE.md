# Directory Deletion Feature

## Overview
This feature adds the ability to delete directories (folders) and all their contents from the Remote File Viewer application.

## Implementation Details

### Backend Changes

#### 1. IFileService Interface Update
Added new method to the service interface:
```csharp
Task<bool> DeleteDirectoryAsync(string directoryPath);
```

#### 2. FileService Implementation (Azure Blob Storage)
- **Location**: `backend/FileViewer.Api/Services/FileService.cs`
- **Method**: `DeleteDirectoryAsync(string directoryPath)`
- **Functionality**:
  - Recursively finds all blobs with the directory prefix
  - Deletes all files within the directory and subdirectories
  - Also removes the `.placeholder` file that represents empty directories
  - Returns `true` if any blobs were deleted, `false` otherwise

#### 3. MockFileService Implementation (Development)
- **Location**: `backend/FileViewer.Api/Services/MockFileService.cs`
- **Method**: `DeleteDirectoryAsync(string directoryPath)`
- **Functionality**:
  - Removes the target directory from the mock directories list
  - Recursively removes all subdirectories that start with the target path
  - Removes all files from the directory and subdirectories
  - Cleans up file contents from the mock storage
  - Persists changes to the mock data file

#### 4. API Endpoint
- **Route**: `DELETE /api/files/directory/{*directoryPath}`
- **Controller**: `FilesController`
- **Method**: `DeleteDirectory(string directoryPath)`
- **Security**: Requires authorization
- **Response**: 
  - `200 OK` with success message if deletion succeeds
  - `404 Not Found` if directory doesn't exist
  - `500 Internal Server Error` if deletion fails

### Frontend Changes

#### 1. FileService Update
- **Location**: `frontend/src/services/fileService.ts`
- **Method**: `deleteDirectory(directoryPath: string)`
- **Functionality**: Makes DELETE request to the API endpoint with URL-encoded directory path

#### 2. Dashboard Component Update
- **Location**: `frontend/src/components/Dashboard.tsx`
- **New Method**: `handleDeleteDirectory(directoryPath: string, directoryName: string)`
- **UI Changes**:
  - Added delete button (trash icon) to each directory item
  - Added confirmation dialog before deletion
  - Added "Open Folder" button alongside the delete button
  - Buttons appear in the file-actions overlay on hover

#### 3. User Experience
- **Confirmation**: Shows a confirmation dialog with the folder name before deletion
- **Visual Feedback**: Success/error messages are displayed to the user
- **Auto-refresh**: Directory listing refreshes automatically after deletion
- **Hover Actions**: Delete and open buttons appear when hovering over a directory

## Security Considerations

1. **Authorization Required**: All directory operations require valid JWT authentication
2. **Confirmation Dialog**: Frontend requires user confirmation before deletion
3. **Recursive Deletion Warning**: User is warned that all contents will be deleted
4. **Error Handling**: Proper error messages are displayed for failed operations

## Testing the Feature

### Manual Testing Steps
1. **Login** to the application
2. **Create a test directory** using the "New Folder" button
3. **Upload some files** to the directory
4. **Create subdirectories** within the test directory
5. **Hover over the directory** to see the action buttons
6. **Click the delete button** (trash icon)
7. **Confirm deletion** in the dialog
8. **Verify** the directory and all contents are removed

### API Testing
```bash
# Create a directory
curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer <valid_token>" \
  -d '{"directoryPath": "test-folder"}' \
  https://your-api-url/api/files/directory

# Delete a directory
curl -X DELETE -H "Authorization: Bearer <valid_token>" \
  https://your-api-url/api/files/directory/test-folder
```

## Deployment

The feature has been deployed to Azure and is available at:
- **Frontend**: https://wonderful-mud-0a20d140f.2.azurestaticapps.net
- **API**: https://remotefile-cpgnd2b8bjc6hhga.westus-01.azurewebsites.net

## Future Enhancements

1. **Bulk Operations**: Allow selection and deletion of multiple directories
2. **Recycle Bin**: Implement soft delete with recovery option
3. **Progress Indicator**: Show deletion progress for large directories
4. **Permissions**: Add role-based permissions for directory deletion
5. **Audit Log**: Track directory deletion operations for compliance
