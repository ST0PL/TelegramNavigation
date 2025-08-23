using Microsoft.EntityFrameworkCore;

namespace ExampleBot.Database
{
    internal class MediaContext : DbContext
    {
        public DbSet<MediaType>? Types { get; set; }
        public DbSet<Media>? Media { get; set; }

        public MediaContext()
            => Database.EnsureCreated();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlite("Data Source=media.db");
    }
}
