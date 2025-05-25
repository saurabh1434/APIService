using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace KrishivVideoUploader.Modal
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
    }
}
