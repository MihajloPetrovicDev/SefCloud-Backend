namespace SefCloud.Backend.DTOs
{
    public class CreateStorageContainerRequest
    {
        public string AuthToken { get; set; }
        public string StorageContainerName { get; set; }
    }
}
