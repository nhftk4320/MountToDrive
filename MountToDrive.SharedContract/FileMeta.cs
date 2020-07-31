using System.IO;

namespace MountToDrive.SharedContract
{
    public class FileMeta
    {
        public string[] PathHierarchy { get; set; }
        public FileType FileType { get; set; }
        public string FileName => PathHierarchy[PathHierarchy.Length - 1];

        public object FileHandle { get; set; }
    }
}