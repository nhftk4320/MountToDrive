using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using DriveData = Google.Apis.Drive.v3.Data;
using MountToDrive.SharedContract;
using System.Linq;
using Google.Apis.Download;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace MountToDrive.Plugins.GoogleDriveStorage
{
    public class GoogleDriveApiHandler : IDisposable
    {
        static readonly string[] _scopes = { DriveService.Scope.Drive };
        static readonly string _applicationName = "Drive API .NET Quickstart";
        private readonly DriveService _service;


        public GoogleDriveApiHandler(string clientSecretFilePath)
        {
            UserCredential credential;


            using (var stream =
                new FileStream(clientSecretFilePath, FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    _scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            // Create Drive API service.
            _service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = _applicationName,
            });
        }

        public List<GDFile> GetFileList()
        {
            var listRequest = _service.Files.List();
            listRequest.Fields = "nextPageToken, files(id, name, parents, size, createdTime, modifiedTime, viewedByMeTime)";
            listRequest.Q = "'me' in owners";

            FileList fileRequestResult = listRequest.Execute();
            IList<DriveData.File> files = fileRequestResult.Files;

            var l =  files.Select(file => new GDFile()
            {
                FileInformation = new FileInformation()
                {
                    FileName = file.Name,
                    Attributes = GetFileAttributes(file),
                    CreationTime = file.CreatedTime,
                    LastAccessTime = file.ViewedByMeTime,
                    LastWriteTime = file.ModifiedTime,
                    Length = file.Size ?? 0
                },
                ID = file.Id,
                ParentID = file.Parents[0]
            }).ToList();
            return l;
        }

        public long GetTotalSpace()
        {
            var aboutRequest = _service.About.Get();
            aboutRequest.Fields = "*";
            var storageQuota = aboutRequest.Execute().StorageQuota;

            return storageQuota.Limit.Value;
        }

        public long GetFreeSpace()
        {
            var aboutRequest = _service.About.Get();
            aboutRequest.Fields = "*";
            var storageQuota = aboutRequest.Execute().StorageQuota;

            return storageQuota.Limit.Value - storageQuota.Usage.Value;
        }

        public GDFileDownloadHandle StartFileDownload(string fileId)
        {
            Stream fileStream = new MemoryStream();
            var cts = new CancellationTokenSource();
            var driveFileStream = new GDFileDownloadHandle()
            {
                FileStream = fileStream,
                CancellationTokenSource = cts
            };
            var downloadRequest = _service.Files.Get(fileId);
            downloadRequest.MediaDownloader.ProgressChanged +=
                (progress) => driveFileStream.CurrentDownloadStatus = progress;

            driveFileStream.DownloadTask = downloadRequest.DownloadAsync(fileStream);
            
            return driveFileStream;
        }

        public long DownloadFileSegment(string fileId, byte[] buffer, long offset)
        {
            using Stream fileStream = new MemoryStream(buffer);
            var downloadRequest = _service.Files.Get(fileId);

            var range = new RangeHeaderValue(offset, offset + buffer.Length - 1);
            var downloadStatus = downloadRequest.DownloadRange(fileStream, range);

            return downloadStatus.BytesDownloaded;
        }

        private FileAttributes GetFileAttributes(DriveData.File file)
        {
            return file.MimeType switch
            {
                "application/vnd.google-apps.folder" => FileAttributes.Directory,
                _ => FileAttributes.Normal,
            };
        }

        public void Dispose()
        {
            _service.Dispose();
        }
    }
}
