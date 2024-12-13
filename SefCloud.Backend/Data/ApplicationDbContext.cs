using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SefCloud.Backend.Models;

namespace SefCloud.Backend.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        // Define your DbSets for the tables here
        public DbSet<ApplicationUser> AspNetUsers { get; set; }
        public DbSet<StorageContainer> StorageContainers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure other entities
            modelBuilder.Entity<StorageContainer>(entity =>
            {
                entity.ToTable("Containers");
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("getdate()");
            });
        }
    }
}
