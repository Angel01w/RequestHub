using Microsoft.EntityFrameworkCore;
using RequestHub.Domain.Entities;
using RequestHub.Domain.Enums;
using System.Collections.Generic;
using System.Reflection.Emit;

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

        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Username = "admin", FullName = "Administrador", Role = UserRole.Admin, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123*") },
            new User { Id = 2, Username = "solicitante", FullName = "Usuario Solicitante", Role = UserRole.Solicitante, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Solicitante123*") },
            new User { Id = 3, Username = "gestor-ti", FullName = "Gestor TI", Role = UserRole.Gestor, AreaId = 1, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Gestor123*") }
        );

        modelBuilder.Entity<ServiceRequest>()
            .HasIndex(x => x.Number)
            .IsUnique();
    }
}
