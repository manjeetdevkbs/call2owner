using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Utilities
{
    public class AzureBlobUploader
    {
        private readonly string _connectionString;
        private readonly string _containerName;

        public AzureBlobUploader(string connectionString, string containerName)
        {
            _connectionString = connectionString;
            _containerName = containerName;
        }

        public async Task<Dictionary<string, string>> UploadFilesAsync(List<IFormFile> files, string fileNamePrefix)
        {
            var result = new Dictionary<string, string>();
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();

            foreach (var file in files)
            {
                string containerName = string.Concat(fileNamePrefix) + Path.GetExtension(file.FileName);

                var blobClient = containerClient.GetBlobClient(containerName);

                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, overwrite: true);

                result[file.FileName] = blobClient.Uri.ToString();
            }

            return result;
        }

        public async Task<(string BlobName, string BlobUrl)> UploadFileAsync(IFormFile file, string fileNamePrefix)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();

            // Construct blob name using prefix and file extension
            string blobName = $"{fileNamePrefix}{Path.GetExtension(file.FileName)}";

            var blobClient = containerClient.GetBlobClient(blobName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);

            return (blobName, blobClient.Uri.ToString());
        }


        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string fileNamePrefix)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();

            // Construct the blob name with the prefix
            var blobName = $"{fileNamePrefix}_{fileName}";

            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(fileStream, overwrite: true);

            return blobClient.Uri.ToString();
        }


        public async Task<Dictionary<string, string>> ListAllDocumentsAsync()
        {
            var result = new Dictionary<string, string>();
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                result[blobItem.Name] = blobClient.Uri.ToString();
            }

            return result;
        }

        public async Task<string?> GetDocumentUrlByNameAsync(string fileName)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

            var blobClient = containerClient.GetBlobClient(fileName);

            if (await blobClient.ExistsAsync())
            {
                return blobClient.Uri.ToString();
            }

            return null;
        }


        public async Task<string> DownloadFileFromBlobAsync(string blobName, string downloadDirectory)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            Directory.CreateDirectory(downloadDirectory);
            var localFilePath = Path.Combine(downloadDirectory, blobName);
            using (var downloadFileStream = File.OpenWrite(localFilePath))
            {
                await blobClient.DownloadToAsync(downloadFileStream);
            }

            return localFilePath;
        }

    }
}


