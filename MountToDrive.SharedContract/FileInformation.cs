using System;
using System.IO;

namespace MountToDrive.SharedContract
{
    public class FileInformation
    {
        public string FileName { get; set; }
        public FileAttributes Attributes { get; set; }
        public DateTime? CreationTime { get; set; }
        public DateTime? LastAccessTime { get; set; }
        public DateTime? LastWriteTime { get; set; }
        public long Length { get; set; }
    }
}
