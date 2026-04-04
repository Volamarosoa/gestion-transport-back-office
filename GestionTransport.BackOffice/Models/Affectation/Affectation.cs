using GestionTransport.BackOffice.Models.Employes;
using GestionTransport.BackOffice.Models.Transport;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace GestionTransport.BackOffice.Models.Affectation;

 [Table("Affectation")]
public class Affectation
{
    public int Id { get; set; }

    // Clés étrangères
    public DateTime? DateTransport { get; set; }
    public int IdEmploye { get; set; }
    public int IdAdresse { get; set; }
    public int IdTypeTransport { get; set; }
    public int IdSite { get; set; }
    public int? IdVehicule { get; set; }
    public int IdHeureTransport { get; set; }
    public int IdType { get; set; }

    public bool? EstValidee { get; set; }
    public string? Commentaire { get; set; }

    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime? DateValidation { get; set; }

    public bool EstArchive { get; set; } = false;

    public Employe? Employe { get; set; }
    public AdresseEmploye? Adresse { get; set; }
    public TypeTransport? TypeTransport { get; set; }
    public Site? Site { get; set; }
    public Vehicule? Vehicule { get; set; }
    public HeureTransport? HeureTransport { get; set; }
    public TypeAffectation? TypeAffectation { get; set; }
    public List<HistoriqueAffectation>? Historique { get; set; }

    public void Valider(string? commentaire = null)
    {
        EstValidee = true;
        DateValidation = DateTime.Now;
        if (!string.IsNullOrEmpty(commentaire))
            Commentaire = commentaire;
    }

    public void Rejeter(string? commentaire = null)
    {
        EstValidee = false;
        DateValidation = DateTime.Now;
        if (!string.IsNullOrEmpty(commentaire))
            Commentaire = commentaire;
    }

    public void Archiver()
    {
        EstArchive = true;
    }

    public void Restaurer()
    {
        EstArchive = false;
    }

    // Vérification
    public bool EstEnAttente() => !EstValidee.HasValue;

    public bool EstActive() => !EstArchive && EstValidee == true;

    public bool PeutEtreModifiee() => !EstArchive && !EstValidee.HasValue;

    public bool EstAutomatique() => TypeAffectation?.EstAutomatique() ?? false;

    public bool EstManuelle() => TypeAffectation?.EstManuel() ?? false;

    public bool VehiculeAffecte() => IdVehicule.HasValue;

    // Informations
    public string GetStatut()
    {
        if (EstArchive) return "Archivée";
        if (EstValidee == true) return "Validée";
        if (EstValidee == false) return "En attente";
        return "En attente";
    }

    public string GetDescription()
    {
        var type = TypeTransport?.Libelle ?? "Transport";
        var heure = HeureTransport?.FormatHeure() ?? "--:--";
        var date = DateTransport?.ToString("dd/MM/yyyy") ?? "--/--/----";
        
        return $"{type} - {date} à {heure}";
    }

    public HistoriqueAffectation ToHistoriqueEntry()
    {
        return new HistoriqueAffectation
        {
            IdAffectation = Id,
            Date = DateTransport,
            IdEmploye = IdEmploye,
            IdAdresse = IdAdresse,
            IdTypeTransport = IdTypeTransport,
            IdSite = IdSite,
            IdVehicule = IdVehicule,
            IdHeureTransport = IdHeureTransport,
            EstValidee = EstValidee,
            Commentaire = Commentaire,
            DateCreation = DateCreation,
            DateValidation = DateValidation,
            IdType = IdType,
            DateModification = DateTime.Now
        };
    }
}
