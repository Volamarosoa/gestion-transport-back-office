namespace GestionTransport.BackOffice.Models;
using System.ComponentModel.DataAnnotations;

public class Site
{
    [Key]
    public int Id { get; set; }

    public string? Nom { get; set; }

    public string? Adresse { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public bool Actif { get; set; } = true;

    public DateTime DateInsertion { get; set; } = DateTime.Now;

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

    public string GetTypeSite()
    {
        if (string.IsNullOrEmpty(Nom)) return "Non défini";
        
        return Nom.ToLower() switch
        {
            var n when n.Contains("siège") || n.Contains("siege") => "Siège",
            var n when n.Contains("usine") => "Usine",
            var n when n.Contains("agence") => "Agence",
            var n when n.Contains("entrepôt") || n.Contains("entrepot") => "Entrepôt",
            _ => Nom
        };
    }
}
