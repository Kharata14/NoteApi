using Microsoft.EntityFrameworkCore;
using NoteApi.Common.Models;

namespace NoteApi.Infrastructure.Database
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<NoteTag> NoteTags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.CreatedAt).ValueGeneratedOnAdd().HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).ValueGeneratedOnAddOrUpdate().HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<Note>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).ValueGeneratedOnAdd().HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).ValueGeneratedOnAddOrUpdate().HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(n => n.User)
                   .WithMany(u => u.Notes)
                   .HasForeignKey(n => n.UserId);
            });

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            });

            modelBuilder.Entity<NoteTag>(entity =>
            {
                entity.HasKey(nt => new { nt.NoteId, nt.TagId });

                entity.HasOne(nt => nt.Note)
                   .WithMany(n => n.NoteTags)
                   .HasForeignKey(nt => nt.NoteId);

                entity.HasOne(nt => nt.Tag)
                   .WithMany(t => t.NoteTags)
                   .HasForeignKey(nt => nt.TagId);
            });
        }

    }
}
