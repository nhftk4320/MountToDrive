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

        FileOperationResult CanDeleteDirectory(FileRequestMeta fileMeta);

        FileOperationResult CanDeleteFile(FileRequestMeta fileMeta);

        FileOperationResult CreateFile(FileRequestMeta fileMeta, FileMode mode, FileOptions options, FileAttributes attributes, out object fileHandle);

        void CloseFile(FileRequestMeta fileMeta);

        FileOperationResult GetFileList(FileRequestMeta fileMeta, out IList<FileInformation> files);

        FileOperationResult FlushFileBuffers(FileRequestMeta fileMeta);

        long GetFreeSpace();

        long GetTotalSpace();

        FileOperationResult GetFileInfo(FileRequestMeta fileMeta, out FileInformation fileInfo);

        FileOperationResult MoveFile(FileRequestMeta oldFileMeta, FileRequestMeta newFileMeta, bool replace);

        FileOperationResult ReadFile(FileRequestMeta fileMeta, byte[] buffer, out int bytesRead, long offset);

        FileOperationResult SetFileSize(FileRequestMeta fileMeta, long length);

        FileOperationResult SetFileAttributes(FileRequestMeta fileMeta, FileAttributes attributes);

        void Cleanup(FileRequestMeta fileMeta, bool deleteOnClose);

        FileOperationResult WriteFile(FileRequestMeta fileMeta, byte[] buffer, out int bytesWritten, long offset);
    }
}