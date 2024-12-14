namespace SefCloud.Backend.DTOs
{
    public class CreateStorageContainerRequest
    {
        public string Authorization { get; set; }
        public string StorageContainerName { get; set; }
    }
}