using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MountToDrive.SharedContract;

namespace MountToDrive.Plugins.GoogleDriveStorage
{
    public class GoogleDriveStorage : IFileSystemStorage, IDisposable
    {
        private readonly GoogleDriveApiHandler _apiHandler;
        private readonly List<GDFile> _fileList;
        private readonly Action<string, string[]> _loggerAction;

        public GoogleDriveStorage(GoogleDriveApiHandler apiHandler, Action<string, string[]> loggerAction)
        {
            _apiHandler = apiHandler;
            _fileList = _apiHandler.GetFileList();
            _loggerAction = loggerAction;
        }

        public void Log(string message, params string[] args) => _loggerAction(message, args);

        public string VolumeLabel => "GDrive";

        public StorageFeatures Features => StorageFeatures.CasePreservedNames
            | StorageFeatures.ReadOnlyVolume
            | StorageFeatures.SupportsObjectIDs;

        public uint MaxPathLength => 256;

        public string FileSystemName => "FSNAME";

        public FileOperationResult CanDeleteDirectory(FileRequestMeta fileMeta)
        {
            return FileOperationResult.Success;
        }

        public FileOperationResult CanDeleteFile(FileRequestMeta fileMeta)
        {
            return FileOperationResult.Success;
        }

        public void Cleanup(FileRequestMeta fileMeta, bool deleteOnClose)
        {

        }

        public void CloseFile(FileRequestMeta fileMeta)
        {
            if (fileMeta.PathHierarchy.Length == 0) { return; }

            try
            {
                var file = _fileList.First(file => file.FileInformation.FileName == fileMeta.FileName);


                if (Monitor.TryEnter(file))
                {
                    if (file.FileStream != null)
                    {
                        file.FileStream?.Dispose();
                        file.FileStream = null;
                    }
                    Monitor.Exit(file);
                }
            }
            catch (Exception e) {
                Log(e.ToString()); 
            }
        }

        public FileOperationResult CreateFile(FileRequestMeta fileMeta, FileMode mode, FileOptions options, FileAttributes attributes, out object fileHandle)
        {
            fileHandle = null;
            if (fileMeta.PathHierarchy.Length == 0)
            {
                return FileOperationResult.Success;
            }

            if (mode == FileMode.Open)
            {
                var file = _fileList.FirstOrDefault(file => file.FileInformation.FileName == fileMeta.FileName);
                if (file != null)
                {
                    fileHandle = file;
                    return FileOperationResult.Success;
                }
            }

            return FileOperationResult.FileNotFound;
        }

        public FileOperationResult FlushFileBuffers(FileRequestMeta fileMeta)
        {
            return FileOperationResult.Success;
        }

        public FileOperationResult GetFileInfo(FileRequestMeta fileMeta, out FileInformation fileInfo)
        {
            if (fileMeta.PathHierarchy.Length == 0)
            {
                fileInfo = new FileInformation()
                {
                    Attributes = FileAttributes.Directory,
                    FileName = "Root"
                };
                return FileOperationResult.Success;
            }

            var file = _fileList.First(file => file.FileInformation.FileName == fileMeta.FileName);
            fileInfo = file.FileInformation;
            return FileOperationResult.Success;
        }

        public FileOperationResult GetFileList(FileRequestMeta fileMeta, out IList<FileInformation> files)
        {
            files = _fileList.Select(file => file.FileInformation).ToList();
            return FileOperationResult.Success;
        }

        public long GetFreeSpace() => _apiHandler.GetFreeSpace();

        public long GetTotalSpace() => _apiHandler.GetTotalSpace();

        public FileOperationResult MoveFile(FileRequestMeta oldFileMeta, FileRequestMeta newFileMeta, bool replace)
        {
            throw new NotImplementedException();
        }

        public FileOperationResult ReadFile(FileRequestMeta fileMeta, byte[] buffer, out int bytesRead, long offset)
        {
            if (fileMeta.PathHierarchy.Length == 0)
            {
                bytesRead = 0;
                return FileOperationResult.IsDirectoryNotFile;
            }

            var file = _fileList.First(file => file.FileInformation.FileName == fileMeta.FileName);
            if (file.FileStream != null
                && file.FileStream.CurrentDownloadStatus != null
                && file.FileStream.CurrentDownloadStatus.Status != Google.Apis.Download.DownloadStatus.Failed)
            {
                Log("Continues reading file[{0}:{1}]: {2}", offset.ToString(), buffer.Length.ToString(), fileMeta.FileName);
                if (file.FileStream.BytesDownloaded >= buffer.Length + offset)
                {
                    Log("-- Stream buffer used");
                    bytesRead = file.FileStream.Read(buffer, offset);
                }
                else
                {
                    Log("-- Segment download used ({0} bytes streamed)", file.FileStream.BytesDownloaded.ToString());
                    bytesRead = (int)_apiHandler.DownloadFileSegment(file.ID, buffer, offset);
                }
            }
            else
            {
                Log("Opening file: {0}", fileMeta.FileName);
                file.FileStream = _apiHandler.StartFileDownload(file.ID);

                Log("Reading file[{0}:{1}]: {2}", offset.ToString(), buffer.Length.ToString(), fileMeta.FileName);
                bytesRead = (int)_apiHandler.DownloadFileSegment(file.ID, buffer, offset);
            }

            Log("Read {0} bytes.", bytesRead.ToString());
            Log("Done");
            return FileOperationResult.Success;
        }

        public FileOperationResult SetFileAttributes(FileRequestMeta fileMeta, FileAttributes attributes)
        {
            throw new NotImplementedException();
        }

        public FileOperationResult SetFileSize(FileRequestMeta fileMeta, long length)
        {
            throw new NotImplementedException();
        }

        public FileOperationResult WriteFile(FileRequestMeta fileMeta, byte[] buffer, out int bytesWritten, long offset)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            foreach (var file in _fileList)
            {
                file.FileStream?.Dispose();
            }
        }
    }
}
