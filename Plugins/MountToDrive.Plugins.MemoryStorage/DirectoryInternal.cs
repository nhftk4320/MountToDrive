using System;
using System.Collections.Generic;
using System.IO;

namespace MountToDrive.Plugins.MemoryStorage
{
    public class DirectoryInternal : FileInternal
    {

        /// <summary>
        /// Represents all inner files and folders
        /// </summary>
        public Dictionary<string, FileInternal> Files { get; set; }

        public static DirectoryInternal GenerateDirectoryInternal(string directoryName, DirectoryInternal parent)
        {
            return new DirectoryInternal()
            {
                Files = new Dictionary<string, FileInternal>(),
                FileInformation = new SharedContract.FileInformation()
                {
                    FileName = directoryName,
                    Attributes = FileAttributes.Directory,
                    CreationTime = DateTime.Now,
                    LastAccessTime = DateTime.Now,
                    LastWriteTime = DateTime.Now,
                },
                Parent = parent
            };
        }
    } 
}
