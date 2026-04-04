using GestionTransport.BackOffice.Models;
using GestionTransport.BackOffice.Models.Employes;
using GestionTransport.BackOffice.Models.Affectation;
using Microsoft.EntityFrameworkCore;

namespace GestionTransport.BackOffice.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Vehicule> ListeVehicule { get; set; }
        public DbSet<Affectation> ListeAffectation { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ===== AFFECTATION =====
    modelBuilder.Entity<Affectation>(entity =>
    {
        entity.ToTable("Affectation");

        entity.HasOne(a => a.Employe)
              .WithMany()
              .HasForeignKey(a => a.IdEmploye)
              .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(a => a.Adresse)
              .WithMany()
              .HasForeignKey(a => a.IdAdresse)
              .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(a => a.TypeTransport)
              .WithMany()
              .HasForeignKey(a => a.IdTypeTransport)
              .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(a => a.Site)
              .WithMany()
              .HasForeignKey(a => a.IdSite)
              .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(a => a.Vehicule)
              .WithMany()
              .HasForeignKey(a => a.IdVehicule)
              .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(a => a.HeureTransport)
              .WithMany()
              .HasForeignKey(a => a.IdHeureTransport)
              .OnDelete(DeleteBehavior.NoAction);

        entity.HasOne(a => a.TypeAffectation)
              .WithMany()
              .HasForeignKey(a => a.IdType)
              .OnDelete(DeleteBehavior.NoAction);
    });

    // ===== EMPLOYE =====
    modelBuilder.Entity<Employe>(entity =>
    {
        entity.HasOne(e => e.Departement)
              .WithMany()
              .HasForeignKey(e => e.IdDepartement)
              .OnDelete(DeleteBehavior.NoAction);
    });

    // ===== ADRESSE EMPLOYE =====
    modelBuilder.Entity<AdresseEmploye>(entity =>
    {
        entity.HasOne(a => a.Employe)
              .WithMany(e => e.Adresses)
              .HasForeignKey(a => a.IdEmploye)
              .OnDelete(DeleteBehavior.NoAction);

        entity.Property(a => a.Latitude).HasPrecision(10, 7);
        entity.Property(a => a.Longitude).HasPrecision(10, 7);
    });

    // ===== SITE =====
    modelBuilder.Entity<Site>(entity =>
    {
        entity.Property(s => s.Latitude).HasPrecision(10, 7);
        entity.Property(s => s.Longitude).HasPrecision(10, 7);
    });
}
    }
}