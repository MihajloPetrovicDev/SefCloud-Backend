namespace SefCloud.Backend.DTOs
{
    public class DeleteStorageContainerItemRequest
    {
        public string Authorization { get; set; }
        public int StorageContainerItemId { get; set; }
    }
}
