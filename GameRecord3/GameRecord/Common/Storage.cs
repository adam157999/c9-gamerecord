using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage;
using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace GameRecord
{
    public static class Storage
    {
        public static bool UploadFile(string AzureConnectionString, string containerName, string fileName, Stream fileStream, ref string Msg)
        {
            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 |
                                   SecurityProtocolType.Tls11 |
                                   SecurityProtocolType.Tls |
                                   SecurityProtocolType.Ssl3 |
                                   SecurityProtocolType.Tls13;

                CloudStorageAccount storage = CloudStorageAccount.Parse(AzureConnectionString);
                CloudBlobClient client = storage.CreateCloudBlobClient();
                CloudBlobContainer container = client.GetContainerReference(containerName);
                container.CreateIfNotExists(BlobContainerPublicAccessType.Container);

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);
                blockBlob.UploadFromStream(fileStream);

                Msg = blockBlob.Uri.AbsoluteUri;
                return true;
            }
            catch (Exception ex)
            {
                Msg = ex.Message;
                return false;
            }
        }

        public static bool UploadFile(string AzureConnectionString, string containerName, string fileName, string sourceUrl, ref string Msg)
        {
            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 |
                                   SecurityProtocolType.Tls11 |
                                   SecurityProtocolType.Tls |
                                   SecurityProtocolType.Ssl3 |
                                   SecurityProtocolType.Tls13;

                CloudStorageAccount storage = CloudStorageAccount.Parse(AzureConnectionString);
                CloudBlobClient client = storage.CreateCloudBlobClient();
                CloudBlobContainer container = client.GetContainerReference(containerName);
                container.CreateIfNotExists(BlobContainerPublicAccessType.Container);

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);

                Uri referrerUri = new Uri(sourceUrl);
                string refererUrl = referrerUri.Scheme + "://" + referrerUri.Host + "/";

                Stream stream = null;
                if (DownloadCFImages(sourceUrl, refererUrl, out stream))
                {
                    blockBlob.UploadFromStream(stream);
                }

                Msg = blockBlob.Uri.AbsoluteUri;
                return true;
            }
            catch (Exception ex)
            {
                Msg = ex.Message;
                return false;
            }
        }

        public static string[] OpenFile(string AzureConnectionString, string containerName, string fileName, ref string Msg)
        {
            try
            {
                CloudStorageAccount storage = CloudStorageAccount.Parse(AzureConnectionString);
                CloudBlobClient client = storage.CreateCloudBlobClient();
                CloudBlobContainer container = client.GetContainerReference(containerName);
                container.CreateIfNotExists(BlobContainerPublicAccessType.Container);

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);

                string[] messages = null;
                using (MemoryStream stream = new MemoryStream())
                {
                    using (StreamReader sr = new StreamReader(blockBlob.OpenRead()))
                    {
                        messages = sr.ReadToEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    }
                }

                return messages;
            }
            catch (Exception ex)
            {
                Msg = ex.Message;
                return null;
            }
        }

        public static bool DeleteFile(string AzureConnectionString, string containerName, string fileName, ref string Msg)
        {
            try
            {
                CloudStorageAccount storage = CloudStorageAccount.Parse(AzureConnectionString);
                CloudBlobClient client = storage.CreateCloudBlobClient();
                CloudBlobContainer container = client.GetContainerReference(containerName);

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);
                blockBlob.DeleteIfExists();

                return true;
            }
            catch (Exception ex)
            {
                Msg = ex.Message;
                return false;
            }
        }

        public static bool DownloadCFImages(string sourceUrl, string refererUrl, out Stream stream)
        {
            stream = null; // Ensure the out parameter is assigned

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Referrer = new Uri(refererUrl);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");

                    // Execute the request synchronously
                    HttpResponseMessage response = client.GetAsync(sourceUrl).Result;
                    response.EnsureSuccessStatusCode();

                    // Read the stream synchronously
                    stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();

                    return true;
                }
            }
            catch
            {
                stream?.Dispose(); // Dispose stream in case of an error
                stream = null;
                return false;
            }
        }

    }
}