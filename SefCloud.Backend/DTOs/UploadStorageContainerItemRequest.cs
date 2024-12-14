namespace SefCloud.Backend.DTOs
{
    public class UploadStorageContainerItemRequest
    {
        public string Authorization { get; set; }
        public int StorageContainerId { get; set; }
        public List<IFormFile> Files { get; set; }
    }
}
