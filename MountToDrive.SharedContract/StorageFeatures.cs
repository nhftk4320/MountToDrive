namespace MountToDrive.SharedContract
{
    public enum StorageFeatures
    {
        None = 0,
        CaseSensitiveSearch = 1,
        CasePreservedNames = 2,
        UnicodeOnDisk = 4,
        PersistentAcls = 8,
        VolumeQuotas = 32,
        SupportsSparseFiles = 64,
        SupportsReparsePoints = 128,
        SupportsRemoteStorage = 256,
        VolumeIsCompressed = 32768,
        SupportsObjectIDs = 65536,
        SupportsEncryption = 131072,
        NamedStreams = 262144,
        ReadOnlyVolume = 524288,
        SequentialWriteOnce = 1048576,
        SupportsTransactions = 2097152
    }
}
