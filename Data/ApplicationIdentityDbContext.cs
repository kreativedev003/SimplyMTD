using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SimplyMTD.Models;

namespace SimplyMTD.Data
{
    public partial class ApplicationIdentityDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationIdentityDbContext(DbContextOptions<ApplicationIdentityDbContext> options) : base(options)
        {
        }

        public ApplicationIdentityDbContext()
        {
        }

        partial void OnModelBuilding(ModelBuilder builder);

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>()
                   .HasMany(u => u.Roles)
                   .WithMany(r => r.Users)
                   .UsingEntity<IdentityUserRole<string>>();

            this.OnModelBuilding(builder);
        }


        public DbSet<SimplyMTD.Models.ApplicationUser> AspNetUsers { get; set; }

        public DbSet<SimplyMTD.Models.MTD.Accounting> Accountings { get; set; }

        public DbSet<SimplyMTD.Models.MTD.Accountant> Accountants { get; set; }
    }
}