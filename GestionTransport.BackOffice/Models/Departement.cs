namespace GestionTransport.BackOffice.Models;

public class Departement
{
    public int Id { get; set; }

    public string Nom { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool Actif { get; set; } = true;

    public DateTime DateInsertion { get; set; } = DateTime.Now;

    public DateTime? DateDesactivation { get; set; }

    public void Desactiver()
    {
        Actif = false;
        DateDesactivation = DateTime.Now;
    }

    public void Activer()
    {
        Actif = true;
        DateDesactivation = null;
    }

    public bool EstActif() => Actif && DateDesactivation == null;
}