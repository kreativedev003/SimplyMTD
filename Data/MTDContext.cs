using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

using SimplyMTD.Models.MTD;
using Microsoft.AspNetCore.Identity;
using System.Reflection.Emit;

namespace SimplyMTD.Data
{
    public partial class MTDContext : DbContext
    {
        public MTDContext()
        {
        }

        public MTDContext(DbContextOptions<MTDContext> options) : base(options)
        {
        }

        partial void OnModelBuilding(ModelBuilder builder);

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<IdentityUser>().ToTable("Accountant", t => t.ExcludeFromMigrations());
        }

		public DbSet<SimplyMTD.Models.MTD.Planing> Planings { get; set; }

		public DbSet<SimplyMTD.Models.MTD.Billing> Billings { get; set; }

		public DbSet<SimplyMTD.Models.MTD.UserDetail> UserDetails { get; set; }

		public DbSet<SimplyMTD.Models.MTD.W8> W8 { get; set; }

        public DbSet<SimplyMTD.Models.MTD.Attachment> Attachment { get; set; }
	}
}