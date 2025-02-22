using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ShipmentScan.Models;
namespace ShipmentScan.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<ShipmentModel> Shipments { get; set; }
        public DbSet<DestinationModel> Destinations { get; set; }
        public DbSet<ModelCodeModel> GlobalModelCodes { get; set; }
        public DbSet<UserModel> AspNetUsers { get; set; }
        public DbSet<SerialNumModel> ShipmentSerialNums { get; set; }
        public DbSet<RoleModel> AspNetRoles { get; set; }
        public DbSet<UserRoleModel> AspNetUserRole { get; set; }
        public DbSet<ContainerSizeModel> ContainerSizes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserModel>()
            .HasKey(u => u.NIK);

            modelBuilder.Entity<UserRoleModel>()
                .HasOne(s => s.Role)
                .WithMany()
                .HasForeignKey(s => s.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRoleModel>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserNik)
                .OnDelete(DeleteBehavior.Restrict);


            // Konfigurasi relasi ShipmentModel ke ModelCodeModel
            modelBuilder.Entity<ShipmentModel>()
                .HasOne(s => s.Model)
                .WithMany()
                .HasForeignKey(s => s.ModelCode)
                .OnDelete(DeleteBehavior.Restrict);

            // Konfigurasi relasi ShipmentModel ke DestinationModel
            modelBuilder.Entity<ShipmentModel>()     .HasOne(s => s.Destination)
                .WithMany()
                .HasForeignKey(s => s.DestinationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShipmentModel>()
    .HasOne(s => s.ContainerSizeModel)
    .WithMany()
    .HasForeignKey(s => s.ContainerSizeId)
    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SerialNumModel>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserNIK)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
