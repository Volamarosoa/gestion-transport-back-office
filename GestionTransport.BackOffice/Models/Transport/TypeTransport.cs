namespace GestionTransport.BackOffice.Models.Transport;
using System.ComponentModel.DataAnnotations;

public class TypeTransport
{
    [Key]
    public int Id { get; set; }

    public string? Libelle { get; set; } // Aller, Retour

    public bool Actif { get; set; } = true;

    public bool EstAller() => Libelle?.ToLower().Contains("aller") ?? false;

    public bool EstRetour() => Libelle?.ToLower().Contains("retour") ?? false;
}