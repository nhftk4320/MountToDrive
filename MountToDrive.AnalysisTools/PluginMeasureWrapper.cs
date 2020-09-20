using MountToDrive.SharedContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MountToDrive.AnalysisTools
{
    public class PluginMeasureWrapper : IFileSystemStorage
    {
        private readonly IFileSystemStorage _pluginToMeasure;

        public Dictionary<string, StrongBox<int>> CountersByMethod { get; }

        public PluginMeasureWrapper(IFileSystemStorage pluginToMeasure)
        {
            _pluginToMeasure = pluginToMeasure;
            CountersByMethod = new Dictionary<string, StrongBox<int>>();
            InitiateDictionary();
        }

        public void ResetCounter()
        {
            foreach (var counterByMethod in CountersByMethod)
            {
                Interlocked.Exchange(ref counterByMethod.Value.Value, 0);
            }
        }

        private void InitiateDictionary()
        {
            Type interfaceType = typeof(IFileSystemStorage);
            foreach (var method in interfaceType.GetMethods().Where(method => !method.IsSpecialName))
            {
                // will not work on similiar names with different signatures
                CountersByMethod.Add(method.Name, new StrongBox<int>(0));
            }

            foreach (var property in interfaceType.GetProperties())
            {
                CountersByMethod.Add(property.Name, new StrongBox<int>(0));
            }
        }

        private void IncrementCalled(string calledObjName)
        {
            Interlocked.Increment(ref CountersByMethod[calledObjName].Value);

        }

        private T MonitoredPropertyGet<T>(T propVal, string propertyName)
        {
            IncrementCalled(propertyName);
            return propVal;
        }

        #region IFileSystemStorage Implementation

        public string VolumeLabel => MonitoredPropertyGet(_pluginToMeasure.VolumeLabel, nameof(VolumeLabel));

        public StorageFeatures Features => MonitoredPropertyGet(_pluginToMeasure.Features, nameof(Features));

        public uint MaxPathLength => MonitoredPropertyGet(_pluginToMeasure.MaxPathLength, nameof(MaxPathLength));

        public string FileSystemName => MonitoredPropertyGet(_pluginToMeasure.FileSystemName, nameof(FileSystemName));

        public FileOperationResult CanDeleteDirectory(FileRequestMeta fileMeta)
        {
            IncrementCalled(nameof(CanDeleteDirectory));
            return _pluginToMeasure.CanDeleteDirectory(fileMeta);
        }

        public FileOperationResult CanDeleteFile(FileRequestMeta fileMeta)
        {
            IncrementCalled(nameof(CanDeleteFile));
            return _pluginToMeasure.CanDeleteFile(fileMeta);
        }

        public void Cleanup(FileRequestMeta fileMeta, bool deleteOnClose)
        {
            IncrementCalled(nameof(Cleanup));
            _pluginToMeasure.Cleanup(fileMeta, deleteOnClose);
        }

        public void CloseFile(FileRequestMeta fileMeta)
        {
            IncrementCalled(nameof(CloseFile));
            _pluginToMeasure.CloseFile(fileMeta);
        }

        public FileOperationResult CreateFile(FileRequestMeta fileMeta, FileMode mode, FileOptions options, FileAttributes attributes, out object fileHandle)
        {
            IncrementCalled(nameof(CreateFile));
            return _pluginToMeasure.CreateFile(fileMeta, mode, options, attributes, out fileHandle);
        }

        public FileOperationResult FlushFileBuffers(FileRequestMeta fileMeta)
        {
            IncrementCalled(nameof(FlushFileBuffers));
            return _pluginToMeasure.FlushFileBuffers(fileMeta);
        }

        public FileOperationResult GetFileInfo(FileRequestMeta fileMeta, out FileInformation fileInfo)
        {
            IncrementCalled(nameof(GetFileInfo));
            return _pluginToMeasure.GetFileInfo(fileMeta, out fileInfo);
        }

        public FileOperationResult GetFileList(FileRequestMeta fileMeta, out IList<FileInformation> files)
        {
            IncrementCalled(nameof(GetFileList));
            return _pluginToMeasure.GetFileList(fileMeta, out files);
        }

        public long GetFreeSpace()
        {
            IncrementCalled(nameof(GetFreeSpace));
            return _pluginToMeasure.GetFreeSpace();
        }

        public long GetTotalSpace()
        {
            IncrementCalled(nameof(GetTotalSpace));
            return _pluginToMeasure.GetTotalSpace();
        }

        public FileOperationResult MoveFile(FileRequestMeta oldFileMeta, FileRequestMeta newFileMeta, bool replace)
        {
            IncrementCalled(nameof(MoveFile));
            return _pluginToMeasure.MoveFile(oldFileMeta, newFileMeta, replace);
        }

        public FileOperationResult ReadFile(FileRequestMeta fileMeta, byte[] buffer, out int bytesRead, long offset)
        {
            IncrementCalled(nameof(ReadFile));
            return _pluginToMeasure.ReadFile(fileMeta, buffer, out bytesRead, offset);
        }

        public FileOperationResult SetFileAttributes(FileRequestMeta fileMeta, FileAttributes attributes)
        {
            IncrementCalled(nameof(SetFileAttributes));
            return _pluginToMeasure.SetFileAttributes(fileMeta, attributes);
        }

        public FileOperationResult SetFileSize(FileRequestMeta fileMeta, long length)
        {
            IncrementCalled(nameof(SetFileSize));
            return _pluginToMeasure.SetFileSize(fileMeta, length);
        }

        public FileOperationResult WriteFile(FileRequestMeta fileMeta, byte[] buffer, out int bytesWritten, long offset)
        {
            IncrementCalled(nameof(WriteFile));
            return _pluginToMeasure.WriteFile(fileMeta, buffer, out bytesWritten, offset);
        }

        #endregion
    }
}
