using GestionTransport.BackOffice.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionTransport.BackOffice.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Vehicule> ListeVehicule { get; set; }
    }
}