using KSEB.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<FormSummary> Forms { get; set; }

    // Remove DynamicFormFields DbSet - it's not needed as a DbSet
    // public DbSet<DynamicFormField> DynamicFormFields { get; set; } // REMOVE THIS

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure FormSummary
        modelBuilder.Entity<FormSummary>(entity =>
        {
            entity.ToTable("forms");
            entity.HasKey(e => e.FormId);
            entity.Property(e => e.FormId).HasColumnName("form_id");
            entity.Property(e => e.FormTitle).HasColumnName("form_title").HasMaxLength(200);
            entity.Property(e => e.FormDescription).HasColumnName("form_description").HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            //entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.TableName).HasColumnName("table_name").HasMaxLength(200);
        });

        // Remove DynamicFormField configuration - it's not a table
        // modelBuilder.Entity<DynamicFormField>().HasNoKey(); // REMOVE THIS
    }
}