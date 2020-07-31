using MountToDrive.MemoryStoragePlugin;
using MountToDrive.SharedContract;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MountToDrive.MemoryStoragePlugin
{
    
    public class MemoryContainer : IDisposable
    {
        public enum ContainerOperationResult
        {
            Success,
            PathNotFound,
            AlreadyExists,
            NotADirectory,
            DirectoryNotEmpty
        }

        public DirectoryInternal RootDirectory { get; set; }

        public MemoryContainer()
        {
            this.RootDirectory = new DirectoryInternal()
            {
                Files = new Dictionary<string, FileInternal>(),
                FileInformation = new SharedContract.FileInformation()
                {
                    Attributes = FileAttributes.Device | FileAttributes.Directory
                }
            };
        }

        /// <summary>
        /// Tries to get the given file. 
        /// <para>fails if the file or its path doesn't exist</para>
        /// </summary>
        public bool TryGetFile(string[] fileHierarchy, out FileInternal fileInternal)
        {
            return TryGetFile(fileHierarchy, fileHierarchy.Length, out fileInternal);
        }

        /// <summary>
        /// Tries to get the given directory. 
        /// <para>Fail reasons: PathNotFound, NotADirectory</para>
        /// </summary>
        public ContainerOperationResult TryGetDirectory(string[] fileHierarchy, out DirectoryInternal directoryInternal)
        {
            return TryGetDirectory(fileHierarchy, fileHierarchy.Length, out directoryInternal);
        }

        /// <summary>
        /// Tries to get the given directory. 
        /// <para>Fail reasons: PathNotFound, NotADirectory</para>
        /// </summary>
        public ContainerOperationResult TryGetDirectory(string[] fileHierarchy, int arrLenToUse, out DirectoryInternal directoryInternal)
        {
            if (arrLenToUse == 0)
            {
                directoryInternal = RootDirectory;
                return ContainerOperationResult.Success;
            }

            if (TryGetFile(fileHierarchy, arrLenToUse, out FileInternal fileInternal) == false)
            {
                directoryInternal = null;
                return ContainerOperationResult.PathNotFound;
            }

            if (fileInternal is DirectoryInternal == false)
            {
                directoryInternal = null;
                return ContainerOperationResult.NotADirectory;
            }

            directoryInternal = (DirectoryInternal)fileInternal;
            return ContainerOperationResult.Success;
        }

        /// <summary>
        /// Tries to add a blank file, returns it to be altered
        /// <para>Fail reasons: PathNotFound, AlreadyExists</para>
        /// </summary>
        public ContainerOperationResult TryAddFile(string[] fileHierarchy, out FileInternal fileInternal)
        {
            int fileDepth = fileHierarchy.Length;

            ContainerOperationResult operationResult = TryGetDirectory(fileHierarchy, fileDepth - 1, out DirectoryInternal containerOfFile);
            if (operationResult != ContainerOperationResult.Success)
            {
                fileInternal = null;
                return ContainerOperationResult.PathNotFound;
            }

            string fileName = fileHierarchy[fileDepth - 1];
            if (containerOfFile.Files.ContainsKey(fileName))
            {
                fileInternal = null;
                return ContainerOperationResult.AlreadyExists;
            }



            fileInternal = FileInternal.GenerateFileInternal(fileName, containerOfFile);
            containerOfFile.Files[fileName] = fileInternal;
            return ContainerOperationResult.Success;
        }

        /// <summary>
        /// Tries to add a blank directory, returns it to be altered
        /// <para>Fail reasons: PathNotFound, AlreadyExists</para>
        /// </summary>
        public ContainerOperationResult TryAddDirectory(string[] fileHierarchy, out DirectoryInternal directoryInternal)
        {
            int fileDepth = fileHierarchy.Length;

            return TryAddDirectory(fileHierarchy, fileDepth, out directoryInternal);
        }

        /// <summary>
        /// Tries to add a blank directory, returns it to be altered
        /// <para>Fail reasons: PathNotFound, AlreadyExists</para>
        /// </summary>
        public ContainerOperationResult TryAddDirectory(string[] fileHierarchy, int arrLenToUse, out DirectoryInternal directoryInternal)
        {

            ContainerOperationResult operationResult = TryGetDirectory(fileHierarchy, arrLenToUse - 1, out DirectoryInternal containerOfFile);
            if (operationResult != ContainerOperationResult.Success)
            {
                directoryInternal = null;
                return ContainerOperationResult.PathNotFound;
            }

            string directoryName = fileHierarchy[arrLenToUse - 1];
            if (containerOfFile.Files.ContainsKey(directoryName))
            {
                directoryInternal = null;
                return ContainerOperationResult.AlreadyExists;
            }

            directoryInternal = DirectoryInternal.GenerateDirectoryInternal(directoryName, containerOfFile);
            containerOfFile.Files[directoryName] = directoryInternal;
            return ContainerOperationResult.Success;
        }

        /// <summary>
        /// Tries to create given file. on pre-existing file, can be chosen whether to override or keep.
        /// <para>fails if the file or its path doesn't exist</para>
        /// </summary>
        public bool TryGetOrAddFile(string[] fileHierarchy, bool toOverride, out FileInternal fileInternal)
        {
            int fileDepth = fileHierarchy.Length;

            ContainerOperationResult operationResult = TryGetDirectory(fileHierarchy, fileDepth - 1, out DirectoryInternal containerOfFile);
            if (operationResult != ContainerOperationResult.Success)
            {
                fileInternal = null;
                return false;
            }

            string fileName = fileHierarchy[fileDepth - 1];

            bool prevValueExists = containerOfFile.Files.TryGetValue(fileName, out FileInternal prevFile);
            if (toOverride == false && prevValueExists)
            {
                fileInternal = prevFile;
                return true;

            }
            if (prevValueExists)
            {
                if (toOverride == false)
                {
                    fileInternal = prevFile;
                    return true;
                }
                else
                {
                    prevFile.Parent.Files.Remove(fileName);
                    prevFile.Dispose();
                }
            }
            fileInternal = FileInternal.GenerateFileInternal(fileName, containerOfFile);
            containerOfFile.Files[fileName] = fileInternal;

            return true;
        }

        /// <summary>
        /// Tries to delete the given file. This applies to directories too.
        /// <pre>Fail reasons: PathNotFound, DirectoryNotEmpty</pre>
        /// </summary>
        public ContainerOperationResult TryDeleteFile(string[] fileHierarchy)
        {
            int fileDepth = fileHierarchy.Length;

            ContainerOperationResult operationResult = TryGetDirectory(fileHierarchy, fileDepth - 1, out DirectoryInternal parentDirectory);
            if (operationResult != ContainerOperationResult.Success)
            {
                return ContainerOperationResult.PathNotFound;
            }

            string fileName = fileHierarchy[fileDepth - 1];
            if (!parentDirectory.Files.TryGetValue(fileName, out FileInternal fileInternal))
            {
                return ContainerOperationResult.PathNotFound;
            }

            if (fileInternal is DirectoryInternal directoryInternal)
            {
                if (directoryInternal.Files.Count > 0)
                {
                    return ContainerOperationResult.DirectoryNotEmpty;
                }
            }
            
            fileInternal.Dispose();

            parentDirectory.Files.Remove(fileName);
            return ContainerOperationResult.Success;
        }

        public long GetTotalSpaceUsed()
        {
            long spaceSum = 0;
            Stack<DirectoryInternal> streamStack = new Stack<DirectoryInternal>();
            streamStack.Push(RootDirectory);
            DirectoryInternal curDirectory;

            ParallelOptions parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 4
            };

            while (streamStack.Count > 0)
            {
                curDirectory = streamStack.Pop();
                Parallel.ForEach(curDirectory.Files.Values, parallelOptions, fileInternal =>
                {
                    if (fileInternal is DirectoryInternal directoryInternal)
                    {
                        streamStack.Push(directoryInternal);
                    }
                    else
                    {
                        if (fileInternal.FileData == null)
                        {

                        }
                        Interlocked.Add(ref spaceSum, fileInternal.FileData.Capacity);
                        spaceSum += fileInternal.FileData.Capacity;
                    }
                });
            }

            return spaceSum;
        }

        private bool TryGetFile(string[] fileHierarchy, int arrLenToUse, out FileInternal fileInternal)
        {
            int fileDepth = arrLenToUse;

            if (fileDepth == 0)
            {
                fileInternal = RootDirectory;
                return true;
            }

            DirectoryInternal curDirectory = RootDirectory;

            for (int i = 0; i < fileDepth - 1; i++)
            {
                if (curDirectory.Files.TryGetValue(fileHierarchy[i], out FileInternal curFile) == false)
                {
                    fileInternal = null;
                    return false;
                }
                if (curFile is DirectoryInternal curFileAsDirectory)
                {
                    curDirectory = curFileAsDirectory;
                }
                else
                {
                    fileInternal = null;
                    return false;
                }
            }

            return curDirectory.Files.TryGetValue(fileHierarchy[fileDepth - 1], out fileInternal);
        }

        public void Dispose()
        {
            Stack<DirectoryInternal> cleanupStack = new Stack<DirectoryInternal>();
            cleanupStack.Push(RootDirectory);
            DirectoryInternal curDirectory;

            while (cleanupStack.Count > 0)
            {
                curDirectory = cleanupStack.Pop();
                foreach(FileInternal fileInternal in curDirectory.Files.Values)
                {
                    if (fileInternal is DirectoryInternal directoryInternal)
                    {
                        cleanupStack.Push(directoryInternal);
                    }
                    else
                    {
                        fileInternal.Dispose();
                    }
                }
            }
        }
    }
}
