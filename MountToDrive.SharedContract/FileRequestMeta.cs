using System.IO;

namespace MountToDrive.SharedContract
{
    public class FileRequestMeta
    {
        public string[] PathHierarchy { get; set; }
        public FileType FileType { get; set; }
        public string FileName => PathHierarchy[^1];

        public object FileHandle { get; set; }
    }
}