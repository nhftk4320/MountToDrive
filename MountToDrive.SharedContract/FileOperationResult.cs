namespace MountToDrive.SharedContract
{
    public enum FileOperationResult : long
    {
        Success = 0,
        IsDirectoryNotFile = 11101,
        DirectoryNotFound = 3221225530,
        FileNotFound = 3221225524,
        NotADirectory = 3221225731,
        AlreadyExists = 1073741824,
        DirectoryNotEmpty = 3221225729,
        ObjectNameCollision = 3221225525,
    }
}
