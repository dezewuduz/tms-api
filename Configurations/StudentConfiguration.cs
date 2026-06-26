using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Configurations;
public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.RegistrationNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.RegistrationNumber).IsUnique();
        builder.Property(s => s.GPA).HasPrecision(4, 2);

        // Exercise 8 - Shadow property audit
        builder.Property<DateTime>("LastUpdated");

        // Exercise 8 - Concurrency token (PostgreSQL xmin system column)
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Exercise 9 - Soft delete filter
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}