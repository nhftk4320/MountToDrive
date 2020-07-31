using DokanNet;
using Contract = MountToDrive.SharedContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;

namespace MountToDrive.Core
{
    public class DokanImplementer : IDokanOperations
    {
        private readonly Contract.IFileSystemStorage _storage;

        public DokanImplementer(Contract.IFileSystemStorage storage)
        {
            _storage = storage;
        }

        public void Cleanup(string fileName, IDokanFileInfo info)
        {
            Contract.FileMeta fileMeta = GetFileMeta(fileName, info);
            _storage.Cleanup(fileMeta, info.DeleteOnClose);
            return;
        }

        public void CloseFile(string fileName, IDokanFileInfo info)
        {
            _storage.CloseFile(GetFileMeta(fileName, info));
            info.Context = null;
            return;
        }

        public NtStatus CreateFile(string fileName, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, IDokanFileInfo info)
        {
            if (mode == FileMode.CreateNew && fileName == "\\")
            {
                return NtStatus.Success;
            }
            var fileMeta = GetFileMeta(fileName, info);

            Contract.FileOperationResult fileOperationResult = _storage.CreateFile(fileMeta, mode, options, attributes, out object fileHandle);


            if (fileOperationResult == Contract.FileOperationResult.Success
                && !info.IsDirectory)
            {
                info.Context = fileHandle ?? new object();
            }
            if (fileOperationResult == Contract.FileOperationResult.Success && mode == FileMode.OpenOrCreate)
            {
                return NtStatus.ObjectNameCollision;
            }
            if (fileOperationResult == Contract.FileOperationResult.IsDirectoryNotFile)
            {
                info.IsDirectory = true;
                return NtStatus.Success;
            }


            return (NtStatus)fileOperationResult;
        }

        public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
        {
            var fileMeta = GetFileMeta(fileName, info);
            Contract.FileOperationResult result = _storage.CanDeleteDirectory(fileMeta);
            return (NtStatus)result;
        }

        public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
        {
            var fileMeta = GetFileMeta(fileName, info);
            Contract.FileOperationResult result = _storage.CanDeleteFile(fileMeta);
            return (NtStatus)result;
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
        {
            var fileMeta = GetFileMeta(fileName, info);
            Contract.FileOperationResult result = _storage.GetFileList(fileMeta, out IList<Contract.FileInformation> filesAsContractType);

            files = filesAsContractType.Select(file => GenericFileInfoToDokan(file)).ToList();
            return (NtStatus)result;
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, IDokanFileInfo info)
        {
            files = null;
            return NtStatus.NotImplemented;
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
        {
            streams = null;
            return NtStatus.NotImplemented;
        }

        public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info)
        {
            var fileMeta = GetFileMeta(fileName, info);
            Contract.FileOperationResult result = _storage.FlushFileBuffers(fileMeta);
            return (NtStatus)result;
        }

        public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes, IDokanFileInfo info)
        {
            totalNumberOfBytes = _storage.GetTotalSpace();
            long bytesFree = _storage.GetFreeSpace();
            freeBytesAvailable = bytesFree;
            totalNumberOfFreeBytes = bytesFree;
            return NtStatus.Success;
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInformation, IDokanFileInfo info)
        {
            var fileMeta = GetFileMeta(fileName, info);

            Contract.FileOperationResult result = _storage.GetFileInfo(fileMeta, out Contract.FileInformation fileInfo);

            fileInformation = GenericFileInfoToDokan(fileInfo);
            return (NtStatus)result;
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
        {
            security = null;
            return NtStatus.NotImplemented;
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
        {
            volumeLabel = _storage.VolumeLabel;
            features = (FileSystemFeatures)_storage.Features;
            fileSystemName = _storage.FileSystemName;
            maximumComponentLength = _storage.MaxPathLength;
            return NtStatus.Success;           
        }

        public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus Mounted(IDokanFileInfo info)
        {
            return NtStatus.Success;
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info)
        {
            Contract.FileMeta oldFileMeta = GetFileMeta(oldName, info);
            Contract.FileMeta newFileMeta = GetFileMeta(newName, info);

            Contract.FileOperationResult result = _storage.MoveFile(oldFileMeta, newFileMeta, replace);
            return (NtStatus)result;
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
        {
            Contract.FileMeta fileMeta = GetFileMeta(fileName, info);

            Contract.FileOperationResult result = _storage.ReadFile(fileMeta, buffer, out bytesRead, offset);
            return (NtStatus)result;
        }

        public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
        {
            return SetEndOfFile(fileName, length, info);
        }

        public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
        {
            Contract.FileMeta fileMeta = GetFileMeta(fileName, info);
            Contract.FileOperationResult result = _storage.SetFileSize(fileMeta, length);
            return (NtStatus)result;
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info)
        {
            Contract.FileMeta fileMeta = GetFileMeta(fileName, info);

            Contract.FileOperationResult result = _storage.SetFileAttributes(fileMeta, attributes);
            return (NtStatus)result;
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
        {
            return NtStatus.Success;
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, IDokanFileInfo info)
        {
            return NtStatus.Success;
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus Unmounted(IDokanFileInfo info)
        {
            return NtStatus.Success;
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, IDokanFileInfo info)
        {
            Contract.FileMeta fileMeta = GetFileMeta(fileName, info);
            Contract.FileOperationResult result = _storage.WriteFile(fileMeta, buffer, out bytesWritten, offset);
            return (NtStatus)result;
        }

        public Contract.FileMeta GetFileMeta(string fileName, IDokanFileInfo fileInfo)
        {
            string[] pathHierarchy = fileName.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            Contract.FileType fileType = Contract.FileType.File;

            if (fileInfo.IsDirectory)
            {
                fileType = Contract.FileType.Directory;
            }
            else if (pathHierarchy.Length == 0)
            {
                fileType = Contract.FileType.Device;
            }


            return new Contract.FileMeta()
            {
                FileType = fileType,
                PathHierarchy = pathHierarchy,
                FileHandle = fileInfo.Context,
            };
        }

        public FileInformation GenericFileInfoToDokan(Contract.FileInformation genericFileInformation)
        {
            return new FileInformation()
            {
                FileName = genericFileInformation.FileName,
                Attributes = genericFileInformation.Attributes,
                CreationTime = genericFileInformation.CreationTime,
                LastAccessTime = genericFileInformation.LastAccessTime,
                LastWriteTime = genericFileInformation.LastWriteTime,
                Length = genericFileInformation.Length
            };
        }
    }
}
