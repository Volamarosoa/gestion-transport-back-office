using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionTransport.BackOffice.Models
{   
    [Table("Vehicule")]
    public class Vehicule
    {
        public int Id { get; set; }

        [Display(Name = "Matricule")]
        public string? Matricule { get; set; }

        [Display(Name = "Nombre de places")]
        public int NombrePlaces { get; set; }

        public bool Actif { get; set; } = true;

        public DateTime DateInsertion { get; set; } = DateTime.Now;

        public DateTime? DateDesactivation { get; set; }
    }
}