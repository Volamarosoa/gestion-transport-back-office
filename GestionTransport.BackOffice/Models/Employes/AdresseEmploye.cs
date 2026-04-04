namespace GestionTransport.BackOffice.Models.Employes;
using System.ComponentModel.DataAnnotations;

public class AdresseEmploye
{
    [Key]
    public int Id { get; set; }

    public int IdEmploye { get; set; }

    public string? Adresse { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public bool EstPrincipale { get; set; } = false;

    public bool Actif { get; set; } = true;

    public DateTime DateInsertion { get; set; } = DateTime.Now;

    public Employe? Employe { get; set; }

    public void DefinirCommePrincipale()
    {
        EstPrincipale = true;
        Actif = true;
    }

    public void Desactiver()
    {
        Actif = false;
    }

    public void Activer()
    {
        Actif = true;
    }

    public bool EstActif() => Actif;

    public bool ADesCoordonnees() => Latitude.HasValue && Longitude.HasValue;
}
