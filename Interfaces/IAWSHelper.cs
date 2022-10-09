namespace WebFileLoader.Interfaces
{
    public interface IAWSHelper
    {
        Task<byte[]> DownloadFileAsync(string file);

        Task<bool> UploadFileAsync(byte[] file, string fileName, string contentType);

        //Task<bool> DeleteFileAsync(string fileName, string versionId = "");
    }
}
