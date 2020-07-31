using System.Collections.Generic;
using System.IO;

namespace MountToDrive.SharedContract
{
    public interface IFileSystemStorage
    {
        string VolumeLabel { get; }
        StorageFeatures Features { get; }
        uint MaxPathLength { get; }
        string FileSystemName { get; }

        FileOperationResult CanDeleteDirectory(FileMeta fileMeta);

        FileOperationResult CanDeleteFile(FileMeta fileMeta);

        FileOperationResult CreateFile(FileMeta fileMeta, FileMode mode, FileOptions options, FileAttributes attributes, out object fileHandle);

        void CloseFile(FileMeta fileMeta);

        FileOperationResult GetFileList(FileMeta fileMeta, out IList<FileInformation> files);

        FileOperationResult FlushFileBuffers(FileMeta fileMeta);

        long GetFreeSpace();

        long GetTotalSpace();

        FileOperationResult GetFileInfo(FileMeta fileMeta, out FileInformation fileInfo);

        FileOperationResult MoveFile(FileMeta oldFileMeta, FileMeta newFileMeta, bool replace);

        FileOperationResult ReadFile(FileMeta fileMeta, byte[] buffer, out int bytesRead, long offset);

        FileOperationResult SetFileSize(FileMeta fileMeta, long length);

        FileOperationResult SetFileAttributes(FileMeta fileMeta, FileAttributes attributes);

        void Cleanup(FileMeta fileMeta, bool deleteOnClose);

        FileOperationResult WriteFile(FileMeta fileMeta, byte[] buffer, out int bytesWritten, long offset);
    }
}