using InventoryManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace InventoryManager.Infrastructure.Persistence;

public class InventoryManagerDbContext : DbContext
{
    public InventoryManagerDbContext(DbContextOptions<InventoryManagerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemLike> ItemLikes => Set<ItemLike>();
    public DbSet<Discussion> Discussions => Set<Discussion>();
    public DbSet<User> Users => Set<User>();
    public DbSet<InventoryWriteAccess> InventoryWriteAccesses => Set<InventoryWriteAccess>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<InventoryTag> InventoryTags => Set<InventoryTag>();
    public DbSet<ItemFieldDefinition> ItemFieldDefinitions => Set<ItemFieldDefinition>();
    public DbSet<ItemFieldValue> ItemFieldValues => Set<ItemFieldValue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Inventory>()
            .Property(x => x.Version)
            .HasColumnName("xmin")
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate();

        modelBuilder.Entity<Item>()
            .Property(x => x.Version)
            .HasColumnName("xmin")
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate();

        modelBuilder.Entity<Inventory>()
            .HasIndex(x => x.Title);

        modelBuilder.Entity<Inventory>()
            .Property(x => x.OwnerUserId)
            .IsRequired(false);

        modelBuilder.Entity<Inventory>()
            .Property(x => x.CustomIdFormat)
            .HasDefaultValue("[]");

        modelBuilder.Entity<Item>()
            .HasIndex(x => x.InventoryId);

        modelBuilder.Entity<Item>()
            .HasIndex(x => new { x.InventoryId, x.CustomId })
            .IsUnique();

        modelBuilder.Entity<Item>()
            .HasIndex(x => new { x.InventoryId, x.SequenceNumber });

        modelBuilder.Entity<Inventory>()
            .Property<NpgsqlTsVector>("SearchVector")
            .HasColumnType("tsvector")
            .HasComputedColumnSql(
                "to_tsvector('simple', coalesce(\"Title\", '') || ' ' || coalesce(\"Description\", ''))",
                stored: true
            );

        modelBuilder.Entity<Item>()
            .Property<NpgsqlTsVector>("SearchVector")
            .HasColumnType("tsvector")
            .HasComputedColumnSql(
                "to_tsvector('simple', coalesce(\"CustomId\", '') || ' ' || coalesce(\"Name\", ''))",
                stored: true
            );

        modelBuilder.Entity<ItemLike>()
            .HasIndex(x => new { x.ItemId, x.UserId })
            .IsUnique();

        modelBuilder.Entity<Discussion>()
            .ToTable("Discussions");

        modelBuilder.Entity<Discussion>()
            .HasIndex(x => x.InventoryId);

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(x => new { x.Provider, x.ProviderId })
            .IsUnique();

        modelBuilder.Entity<InventoryWriteAccess>()
            .ToTable("InventoryWriteAccesses");

        modelBuilder.Entity<InventoryWriteAccess>()
            .HasIndex(x => new { x.InventoryId, x.UserId })
            .IsUnique();

        modelBuilder.Entity<InventoryWriteAccess>()
            .HasIndex(x => x.UserId);

        modelBuilder.Entity<InventoryWriteAccess>()
            .HasIndex(x => x.InventoryId);

        modelBuilder.Entity<Category>()
            .HasIndex(x => x.Name)
            .IsUnique();

        modelBuilder.Entity<Tag>()
            .HasIndex(x => x.Name)
            .IsUnique();

        modelBuilder.Entity<InventoryTag>()
            .HasIndex(x => new { x.InventoryId, x.TagId })
            .IsUnique();

        modelBuilder.Entity<InventoryTag>()
            .HasIndex(x => x.InventoryId);

        modelBuilder.Entity<InventoryTag>()
            .HasIndex(x => x.TagId);

        modelBuilder.Entity<ItemFieldDefinition>()
            .ToTable("ItemFieldDefinitions");

        modelBuilder.Entity<ItemFieldDefinition>()
            .HasIndex(x => x.InventoryId);

        modelBuilder.Entity<ItemFieldDefinition>()
            .HasIndex(x => new { x.InventoryId, x.DisplayOrder })
            .IsUnique();

        modelBuilder.Entity<ItemFieldValue>()
            .ToTable("ItemFieldValues");

        modelBuilder.Entity<ItemFieldValue>()
            .HasIndex(x => x.ItemId);

        modelBuilder.Entity<ItemFieldValue>()
            .HasIndex(x => new { x.ItemId, x.FieldDefinitionId })
            .IsUnique();
    }
}