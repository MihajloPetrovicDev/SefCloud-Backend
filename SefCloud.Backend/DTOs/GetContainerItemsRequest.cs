namespace SefCloud.Backend.DTOs
{
    public class GetContainerItemsRequest
    {
        public string Authorization {  get; set; }
        public int StorageContainerId {  get; set; }
    }
}
