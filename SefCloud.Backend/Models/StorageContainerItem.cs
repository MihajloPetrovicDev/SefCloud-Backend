namespace SefCloud.Backend.Models
{
    public class StorageContainerItem
    {
        public int Id { get; set; }
        public int ContainerId { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
