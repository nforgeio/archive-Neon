namespace TinyURL
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class TinyUrlContext : DbContext
    {
        public TinyUrlContext()
            : base("name=TinyUrlContext")
        {
        }

        public virtual DbSet<Hit> Hits { get; set; }
        public virtual DbSet<Link> Links { get; set; }
        public virtual DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Link>()
                .Property(e => e.Id)
                .IsUnicode(false);

            modelBuilder.Entity<Link>()
                .Property(e => e.Source)
                .IsUnicode(false);

            modelBuilder.Entity<Hit>()
                .Property(e => e.IP)
                .IsUnicode(false);

            modelBuilder.Entity<Hit>()
                .Property(e => e.TimeUtc)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);
        }
    }
}
