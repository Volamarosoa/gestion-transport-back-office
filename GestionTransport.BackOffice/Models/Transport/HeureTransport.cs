namespace GestionTransport.BackOffice.Models.Transport;
using System.ComponentModel.DataAnnotations;

public class HeureTransport
{
    [Key]
    public int Id { get; set; }

    public TimeSpan? Heure { get; set; }

    public string? Libelle { get; set; } // Matin, Soir, Nuit

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

    public string GetPeriodeJournee()
    {
        if (!Heure.HasValue) return "Non définie";

        return Heure.Value.Hours switch
        {
            >= 5 and < 12 => "Matin",
            >= 12 and < 18 => "Après-midi",
            >= 18 and < 22 => "Soir",
            _ => "Nuit"
        };
    }

    public string FormatHeure() => Heure?.ToString(@"hh\:mm") ?? "--:--";
}
