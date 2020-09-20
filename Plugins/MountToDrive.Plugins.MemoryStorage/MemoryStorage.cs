using MountToDrive.SharedContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace MountToDrive.Plugins.MemoryStorage
{
    public class MemoryStorage : IFileSystemStorage, IDisposable
    {
        private readonly long _totalSpaceBytes;

        private readonly MemoryContainer _memoryContainer;
        public string VolumeLabel { get; } 
        public StorageFeatures Features { get; } = StorageFeatures.CaseSensitiveSearch | StorageFeatures.CasePreservedNames | StorageFeatures.SupportsObjectIDs;
        public uint MaxPathLength { get; } = 30;
        public string FileSystemName { get; } = "MemoryStorage";
        public long TotalSpaceMB => _totalSpaceBytes * (long)1e-6;

        public MemoryStorage(string volumeLabel, int volumeSizeMB)
        {
            this._totalSpaceBytes = volumeSizeMB * (long)1e6;
            this.VolumeLabel = volumeLabel;
            this._memoryContainer = new MemoryContainer()
            {
                RootDirectory = new DirectoryInternal()
                {
                    FileInformation = new FileInformation()
                    {
                        Attributes = FileAttributes.Device | FileAttributes.Directory,
                    },
                    Files = new Dictionary<string, FileInternal>()
                }
            };
        }

        public FileOperationResult CanDeleteDirectory(FileRequestMeta fileMeta)
        {
            if (_memoryContainer.TryGetFile(fileMeta.PathHierarchy, out FileInternal fileInternal) == false)
            {
                return FileOperationResult.DirectoryNotFound;
            }

            if (!(fileInternal is DirectoryInternal directory))
            {
                return FileOperationResult.NotADirectory;
            }

            if (directory.Files.Count > 0)
            {
                return FileOperationResult.DirectoryNotEmpty;
            }

            return FileOperationResult.Success;
        }

        public FileOperationResult CanDeleteFile(FileRequestMeta fileMeta)
        {
            if(_memoryContainer.TryGetFile(fileMeta.PathHierarchy, out FileInternal fileInternal) == false)
            {
                return FileOperationResult.FileNotFound;
            }

            if (fileInternal is DirectoryInternal)
            {
                return FileOperationResult.FileNotFound;
            }

            return FileOperationResult.Success;
        }

        public FileOperationResult CreateFile(FileRequestMeta fileMeta, FileMode mode, FileOptions options, FileAttributes attributes, out object fileHandle)
        {
            if (fileMeta.FileType == FileType.File)
            {
                return CreateFile_File(fileMeta, mode, out fileHandle);
            }

            fileHandle = null;
            return CreateFile_Directory(fileMeta, mode);

        }

        private FileOperationResult CreateFile_File(FileRequestMeta fileMeta, FileMode mode, out object fileHandle)
        {
            FileInternal fileInternal;

            switch (mode)
            {
                case FileMode.Append:
                case FileMode.Open:
                case FileMode.Truncate:
                    if (_memoryContainer.TryGetFile(fileMeta.PathHierarchy, out fileInternal) == false)
                    {
                        fileHandle = null;
                        return FileOperationResult.FileNotFound;
                    }

                    if (fileInternal is DirectoryInternal)
                    {
                        fileHandle = null;
                        return FileOperationResult.IsDirectoryNotFile;
                    }

                    fileHandle = fileInternal;
                    if (mode == FileMode.Append)
                    {
                        fileInternal.FileData.Seek(0, SeekOrigin.End);
                    }
                    else if (mode == FileMode.Truncate)
                    {
                        fileInternal.TruncateStream();
                    }

                    return FileOperationResult.Success;
                case FileMode.Create:
                case FileMode.OpenOrCreate:
                    bool toOverride = mode == FileMode.Create;

                    if (_memoryContainer.TryGetOrAddFile(fileMeta.PathHierarchy, toOverride, out fileInternal) == false)
                    {
                        fileHandle = null;
                        return FileOperationResult.FileNotFound;
                    }

                    fileHandle = fileInternal;
                    return FileOperationResult.Success;
                case FileMode.CreateNew:
                    MemoryContainer.ContainerOperationResult result = _memoryContainer.TryAddFile(fileMeta.PathHierarchy, out fileInternal);
                    if (result == MemoryContainer.ContainerOperationResult.Success)
                    {
                        fileHandle = fileInternal.FileData;
                        return FileOperationResult.Success;
                    }
                    else
                    {
                        fileHandle = null;
                        if (result == MemoryContainer.ContainerOperationResult.PathNotFound)
                        {
                            return FileOperationResult.FileNotFound;
                        }
                        return FileOperationResult.AlreadyExists;
                    }
                default: // will never happen
                    fileHandle = null;
                    return FileOperationResult.FileNotFound;
            }
        }

        private FileOperationResult CreateFile_Directory(FileRequestMeta fileMeta, FileMode mode)
        {
            MemoryContainer.ContainerOperationResult result;
            var pathHierarchy = fileMeta.PathHierarchy;

            if (mode == FileMode.Open)
            {
                result = _memoryContainer.TryGetDirectory(pathHierarchy, out _);
                if (result == MemoryContainer.ContainerOperationResult.NotADirectory)
                {
                    return FileOperationResult.NotADirectory;
                }
                if (result == MemoryContainer.ContainerOperationResult.PathNotFound)
                {
                    return FileOperationResult.DirectoryNotFound;
                }
            }
            else // FileMode.Create
            {
                result = _memoryContainer.TryAddDirectory(pathHierarchy, out _);
                if (result == MemoryContainer.ContainerOperationResult.AlreadyExists)
                {
                    return FileOperationResult.AlreadyExists;
                }
                else if (result == MemoryContainer.ContainerOperationResult.PathNotFound)
                {
                    return FileOperationResult.DirectoryNotFound;
                }
            }
            return FileOperationResult.Success;
        }

        public void CloseFile(FileRequestMeta fileMeta) { }

        public FileOperationResult GetFileList(FileRequestMeta fileMeta, out IList<SharedContract.FileInformation> files)
        {
            MemoryContainer.ContainerOperationResult result = _memoryContainer.TryGetDirectory(fileMeta.PathHierarchy, out DirectoryInternal parentDirectory);
            if (result == MemoryContainer.ContainerOperationResult.NotADirectory)
            {
                files = null;
                return FileOperationResult.NotADirectory;
            }
            if (result == MemoryContainer.ContainerOperationResult.PathNotFound)
            {
                files = null;
                return FileOperationResult.DirectoryNotFound;
            }

            files = parentDirectory.Files.Values.Select(file => file.FileInformation).ToList();
            return FileOperationResult.Success;
        }

        public FileOperationResult FlushFileBuffers(FileRequestMeta fileMeta) => FileOperationResult.Success;

        public long GetFreeSpace()
        {
            return _totalSpaceBytes - _memoryContainer.GetTotalSpaceUsed();
        }

        public long GetTotalSpace() => _totalSpaceBytes;

        public FileOperationResult GetFileInfo(FileRequestMeta fileMeta, out SharedContract.FileInformation fileInfo)
        {
            if(_memoryContainer.TryGetFile(fileMeta.PathHierarchy, out FileInternal fileInternal) == false)
            {
                fileInfo = default;
                return FileOperationResult.FileNotFound;
            }

            fileInfo = fileInternal.FileInformation;
            fileInfo.Length = fileInternal.FileData?.Length ?? 0;
            return FileOperationResult.Success;
        }

        public FileOperationResult MoveFile(FileRequestMeta oldFileMeta, FileRequestMeta newFileMeta, bool replace)
        {

            if (_memoryContainer.TryGetFile(oldFileMeta.PathHierarchy, out FileInternal fileToMove) == false)
            {
                return FileOperationResult.FileNotFound;
            }


            MemoryContainer.ContainerOperationResult directoryGetResult = _memoryContainer.TryGetDirectory(newFileMeta.PathHierarchy,
                                                                                                           newFileMeta.PathHierarchy.Length - 1,
                                                                                                           out DirectoryInternal destinationDirectory);

            if (directoryGetResult == MemoryContainer.ContainerOperationResult.NotADirectory)
            {
                return FileOperationResult.NotADirectory;
            }
            else if (directoryGetResult == MemoryContainer.ContainerOperationResult.PathNotFound)
            {
                return FileOperationResult.DirectoryNotFound;
            }

            
            if (destinationDirectory.Files.TryGetValue(newFileMeta.FileName, out FileInternal collidingFile))
            {
                if (collidingFile is DirectoryInternal || fileToMove is DirectoryInternal)
                {
                    return FileOperationResult.ObjectNameCollision; // cant do this with directories
                }

                if (replace == false)
                {
                    return FileOperationResult.ObjectNameCollision;
                }

                collidingFile.Parent.Files.Remove(collidingFile.FileInformation.FileName);
                collidingFile.Dispose();
            }



            fileToMove.Parent.Files.Remove(fileToMove.FileInformation.FileName);
            destinationDirectory.Files.Add(fileToMove.FileInformation.FileName, fileToMove);

            return FileOperationResult.Success;
        }

        public FileOperationResult ReadFile(FileRequestMeta fileMeta, byte[] buffer, out int bytesRead, long offset)
        {
            Stream fileStream;
            if (fileMeta.FileHandle is FileInternal metaAsFileInternal)
            {
                fileStream = metaAsFileInternal.FileData;
            }
            else
            {
                if (_memoryContainer.TryGetFile(fileMeta.PathHierarchy, out FileInternal file) == false)
                {
                    bytesRead = 0;
                    return FileOperationResult.FileNotFound;
                }
                fileStream = file.FileData;
            }


            lock (fileStream)
            {
                fileStream.Seek(offset, SeekOrigin.Begin);
                bytesRead = fileStream.Read(buffer, 0, buffer.Length);
            }
            return FileOperationResult.Success;
        }

        public FileOperationResult SetFileSize(FileRequestMeta fileMeta, long length)
        {
            if (_memoryContainer.TryGetFile(fileMeta.PathHierarchy, out FileInternal file) == false)
            {
                return FileOperationResult.FileNotFound;
            }

            Stream fileStream = file.FileData;
            lock (fileStream)
            {
                file.FileData.SetLength(length);
            }
            return FileOperationResult.Success;
        }

        public FileOperationResult SetFileAttributes(FileRequestMeta fileMeta, FileAttributes attributes)
        {
            return FileOperationResult.Success;
        }

        public void Cleanup(FileRequestMeta fileMeta, bool deleteOnClose)
        {
            if (!deleteOnClose) { return; }

            MemoryContainer.ContainerOperationResult result = _memoryContainer.TryDeleteFile(fileMeta.PathHierarchy);
            if (result != MemoryContainer.ContainerOperationResult.Success)
            {
                Console.WriteLine("Unexptected error: " + result.ToString());
            }
        }

        public FileOperationResult WriteFile(FileRequestMeta fileMeta, byte[] buffer, out int bytesWritten, long offset)
        {
            FileInternal fileHandle;
            if (fileMeta.FileHandle is FileInternal metaAsFileInternal)
            {
                fileHandle = metaAsFileInternal;
            }
            else
            {
                if (_memoryContainer.TryGetFile(fileMeta.PathHierarchy, out FileInternal file) == false)
                {
                    bytesWritten = 0;
                    return FileOperationResult.FileNotFound;
                }
                fileHandle = file;
            }

            Stream fileData = fileHandle.FileData;
            lock (fileData)
            {
                fileHandle.FileData.Seek(offset, SeekOrigin.Begin);
                fileHandle.AddBytes(buffer);
            }
            bytesWritten = buffer.Length;

            return FileOperationResult.Success;
        }

        public void Dispose()
        {
            _memoryContainer.Dispose();
        }
    }
}
