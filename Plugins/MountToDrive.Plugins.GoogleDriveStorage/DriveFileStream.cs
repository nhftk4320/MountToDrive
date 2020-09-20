using Google.Apis.Download;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MountToDrive.Plugins.GoogleDriveStorage
{
    public class GDFileDownloadHandle : IDisposable
    {
        public bool IsDisposed { get; private set; } = false;
        public Task<IDownloadProgress> DownloadTask { get; set; }
        public IDownloadProgress CurrentDownloadStatus { get; set; }
        public long BytesDownloaded => CurrentDownloadStatus?.BytesDownloaded ?? -1;
        public Stream FileStream { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; internal set; }

        public void Dispose()
        {
            CancellationTokenSource.Cancel();
            IsDisposed = true;
            try
            {
                DownloadTask.Wait();
            }
            catch (AggregateException e) when (e.InnerExceptions[0] is TaskCanceledException) { }
            DownloadTask.Dispose();
            FileStream.Dispose();
        }

        public int Read(byte[] buffer, long offset)
        {
            while (CurrentDownloadStatus == null ||
                CurrentDownloadStatus.BytesDownloaded < buffer.Length)
            {
                if (CurrentDownloadStatus != null)
                {
                    if (CurrentDownloadStatus.Status != DownloadStatus.Downloading)
                    {
                        Console.WriteLine("Not expected status: " + CurrentDownloadStatus.Status);
                        return 0;
                    }
                    Console.WriteLine("Bytes downloaded: " + CurrentDownloadStatus.BytesDownloaded);
                }
                Thread.Sleep(300);
            }

            if (FileStream.Position != offset)
            {
                FileStream.Position = offset;
            }

            return FileStream.Read(buffer, 0, buffer.Length);
        }
    }
}
