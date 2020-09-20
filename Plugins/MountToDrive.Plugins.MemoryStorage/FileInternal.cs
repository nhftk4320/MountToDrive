using MountToDrive.SharedContract;
using System;
using System.IO;

namespace MountToDrive.Plugins.MemoryStorage
{
    public class FileInternal : IDisposable
    {
        private FileInformation _fileInformation;
        public FileInformation FileInformation
        {
            get => _fileInformation;
            set => _fileInformation = value;
        }
        public MemoryStream FileData { get; set; }
        public DirectoryInternal Parent { get; set; }
        public void TruncateStream()
        {
            FileData?.Close();
            FileData?.Dispose();
            FileData = new MemoryStream();
        }

        public void AddBytes(byte[] bytes)
        {
            FileData.Write(bytes, 0, bytes.Length);
            _fileInformation.Length = FileData.Length;
        }

        public void Dispose()
        {
            FileData?.Close();
            FileData?.Dispose();
        }

        public static FileInternal GenerateFileInternal(string fileName, DirectoryInternal parent)
        {
            return new FileInternal()
            {
                FileData = new MemoryStream(),
                FileInformation = new FileInformation()
                {
                    FileName = fileName,
                    Attributes = FileAttributes.Normal,
                    CreationTime = DateTime.Now,
                    LastAccessTime = DateTime.Now,
                    LastWriteTime = DateTime.Now,
                },
                Parent = parent
            };
        }
    }
}
