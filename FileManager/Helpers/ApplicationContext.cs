using FileManager.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileManager.Helpers
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {}

        public DbSet<User> Users { get; set; }
        public DbSet<Objects> Objects { get; set; }
        public DbSet<Permissions> Permissions { get; set; }
    }
}