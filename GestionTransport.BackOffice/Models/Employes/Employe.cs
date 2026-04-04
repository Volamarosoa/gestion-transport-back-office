namespace GestionTransport.BackOffice.Models.Employes;
using System.ComponentModel.DataAnnotations;

public class Employe
{
    [Key]
    public int Id { get; set; }

    public string Nom { get; set; } = string.Empty;

    public string Prenom { get; set; } = string.Empty;

    public string? Matricule { get; set; }
    
    public string? Email { get; set; }

    public string? Telephone { get; set; }

    public int IdDepartement { get; set; }

    public bool Actif { get; set; } = true;

    public bool EstBeneficiaire { get; set; } = false;

    public DateTime DateInsertion { get; set; } = DateTime.Now;

    public DateTime? DateDesactivation { get; set; }

    public Departement? Departement { get; set; }
    
    public List<AdresseEmploye>? Adresses { get; set; }

    public string NomComplet() => $"{Prenom} {Nom}";

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
