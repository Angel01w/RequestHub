using Microsoft.EntityFrameworkCore;
using RequestHub.Domain.Entities;
using RequestHub.Domain.Enums;

namespace RequestHub.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<RequestType> RequestTypes => Set<RequestType>();
    public DbSet<Priority> Priorities => Set<Priority>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<RequestComment> RequestComments => Set<RequestComment>();
    public DbSet<RequestHistory> RequestHistories => Set<RequestHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.Property(x => x.Username)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Email)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.PasswordHash)
                .IsRequired();

            entity.Property(x => x.FullName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Role)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Area>(entity =>
        {
            entity.ToTable("Areas");
        });

        modelBuilder.Entity<RequestType>(entity =>
        {
            entity.ToTable("RequestTypes");
        });

        modelBuilder.Entity<Priority>(entity =>
        {
            entity.ToTable("Priorities");
        });

        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.ToTable("ServiceRequests", tb => tb.UseSqlOutputClause(false));

            entity.HasIndex(x => x.Number)
                .IsUnique();
        });

        modelBuilder.Entity<RequestComment>(entity =>
        {
            entity.ToTable("RequestComments", tb => tb.UseSqlOutputClause(false));
        });

        modelBuilder.Entity<RequestHistory>(entity =>
        {
            entity.ToTable("RequestHistory", tb => tb.UseSqlOutputClause(false));
        });

        modelBuilder.Entity<Area>().HasData(
            new Area { Id = 1, Name = "TI" },
            new Area { Id = 2, Name = "Mantenimiento" },
            new Area { Id = 3, Name = "Transporte" },
            new Area { Id = 4, Name = "Compras" }
        );

        modelBuilder.Entity<Priority>().HasData(
            new Priority { Id = 1, Name = "Baja" },
            new Priority { Id = 2, Name = "Media" },
            new Priority { Id = 3, Name = "Alta" }
        );

        modelBuilder.Entity<RequestType>().HasData(
            new RequestType { Id = 1, Name = "Soporte PC", AreaId = 1 },
            new RequestType { Id = 2, Name = "Acceso a sistema", AreaId = 1 },
            new RequestType { Id = 3, Name = "Reparación", AreaId = 2 },
            new RequestType { Id = 4, Name = "Asignación vehículo", AreaId = 3 },
            new RequestType { Id = 5, Name = "Compra insumos", AreaId = 4 }
        );
    }
}