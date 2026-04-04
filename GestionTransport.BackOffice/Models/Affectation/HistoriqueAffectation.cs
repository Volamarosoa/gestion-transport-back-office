namespace GestionTransport.BackOffice.Models.Affectation;
using System.ComponentModel.DataAnnotations;

public class HistoriqueAffectation
{
    [Key]
    public int IdHistorique { get; set; }
    public int IdAffectation { get; set; }

    public DateTime? Date { get; set; }
    public int IdEmploye { get; set; }
    public int IdAdresse { get; set; }
    public int IdTypeTransport { get; set; }
    public int IdSite { get; set; }
    public int? IdVehicule { get; set; }
    public int IdHeureTransport { get; set; }

    public bool? EstValidee { get; set; }
    public string? Commentaire { get; set; }

    public DateTime DateCreation { get; set; }
    public DateTime? DateValidation { get; set; }
    public int IdType { get; set; }

    public DateTime DateModification { get; set; } = DateTime.Now;

    public Affectation? Affectation { get; set; }
}
