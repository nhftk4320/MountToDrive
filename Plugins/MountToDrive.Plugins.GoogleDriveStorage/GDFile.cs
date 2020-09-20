using MountToDrive.SharedContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MountToDrive.Plugins.GoogleDriveStorage
{
    public class GDFile
    {
        public string ID { get; set; }
        public string ParentID { get; set; }
        public FileInformation FileInformation { get; set; }
        public GDFileDownloadHandle FileStream { get; set; }
    }
}
