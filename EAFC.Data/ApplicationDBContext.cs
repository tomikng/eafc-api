using Microsoft.EntityFrameworkCore;
using EAFC.Core.Models;

namespace EAFC.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Player> Players { get; set; }
    }
}