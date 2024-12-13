using System.ComponentModel.DataAnnotations.Schema;

namespace SefCloud.Backend.Models
{
    public class StorageContainer
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string EncryptionKey { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
