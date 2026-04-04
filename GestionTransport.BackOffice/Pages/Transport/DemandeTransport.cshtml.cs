using GestionTransport.BackOffice.Data;
using GestionTransport.BackOffice.Models.Affectation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionTransport.BackOffice.Pages.Transport;

public class DemandeTransportModel : PageModel
{
    private readonly AppDbContext _db;

    public List<Affectation> _ListeDemandes { get; set; } = new();

    public int PageActuelle { get; set; } = 1;
    public int TotalPages   { get; set; }
    public int PageSize      { get; set; } = 3;

    // ← Filtres optionnels via l'URL (?recherche=...&dateTransport=...)
    [BindProperty(SupportsGet = true)]
    public string? Recherche { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateTransportFiltre { get; set; }

    public DemandeTransportModel(AppDbContext db) => _db = db;

    // ===================================================
    // GET — Liste des demandes EN ATTENTE du jour
    // ===================================================
    public async Task OnGetAsync()
    {
        int page = 1;
        if (Request.Query.ContainsKey("page"))
            int.TryParse(Request.Query["page"], out page);

        // Lit manuellement le paramètre depuis l'URL
        DateTime debutJournee = DateTime.Today;
        // ← Filtre par date de transport si renseignée
        Console.WriteLine(DateTransportFiltre);
        if (DateTransportFiltre.HasValue)
            debutJournee = DateTransportFiltre.Value.Date;

        // ← EF : base query — EN ATTENTE + créées aujourd'hui ou après
        var query = _db.ListeAffectation
            .Where(a =>
                a.EstValidee == false &&
                a.DateTransport >= debutJournee        // créée aujourd'hui ou après
            )
            // ← EF : Include → JOIN automatique sur les tables liées
            .Include(a => a.Employe)
            .Include(a => a.Adresse)
            .Include(a => a.TypeTransport)
            .Include(a => a.Site)
            .Include(a => a.Vehicule)
            .Include(a => a.HeureTransport)
            .Include(a => a.TypeAffectation)
            .AsQueryable();

        // ← Filtre par nom d'employé si renseigné
        if (!string.IsNullOrWhiteSpace(Recherche))
        {
            string terme = Recherche.Trim().ToLower();
            query = query.Where(a =>
                a.Employe!.Nom.ToLower().Contains(terme)    ||
                a.Employe!.Prenom.ToLower().Contains(terme) ||
                a.Employe!.Matricule!.ToLower().Contains(terme)
            );
        }

        // ← Tri : les plus récentes en premier
        query = query.OrderByDescending(a => a.DateCreation);

        // ← EF : COUNT pour la pagination
        int total = await query.CountAsync();
        TotalPages   = (int)Math.Ceiling(total / (double)PageSize);

        if (page < 1) page = 1;
        if (page > TotalPages && TotalPages > 0) page = TotalPages;
        PageActuelle = page;

        // ← EF : SKIP / TAKE → OFFSET / FETCH SQL Server
        _ListeDemandes = await query
            .Skip((PageActuelle - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    // ===================================================
    // POST — Valider une demande
    // ===================================================
    public async Task<IActionResult> OnPostValiderAsync(int id, string? commentaire)
    {
        int.TryParse(Request.Form["pageActuelle"], out int page);
        PageActuelle = page == 0 ? 1 : page;

        // ← EF : FindAsync → SELECT WHERE PK = id
        var demande = await _db.ListeAffectation.FindAsync(id);

        if (demande == null)
        {
            TempData["Error"] = $"Demande #{id} introuvable !";
            return Redirect($"/Transport/DemandeTransport?page={PageActuelle}");
        }

        if (!demande.PeutEtreModifiee())
        {
            TempData["Error"] = "Cette demande ne peut plus être modifiée.";
            return Redirect($"/Transport/DemandeTransport?page={PageActuelle}");
        }

        // ← Utilise la méthode métier de votre modèle
        demande.Valider(commentaire);

        // ← EF : tracking auto → génère UPDATE
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Demande #{id} validée avec succès !";
        return Redirect($"/Transport/DemandeTransport?page={PageActuelle}");
    }

    // ===================================================
    // POST — Rejeter une demande
    // ===================================================
    public async Task<IActionResult> OnPostRejeterAsync(int id, string? commentaire)
    {
        int.TryParse(Request.Form["pageActuelle"], out int page);
        PageActuelle = page == 0 ? 1 : page;

        var demande = await _db.ListeAffectation.FindAsync(id);

        if (demande == null)
        {
            TempData["Error"] = $"Demande #{id} introuvable !";
            return Redirect($"/Transport/DemandeTransport?page={PageActuelle}");
        }

        if (!demande.PeutEtreModifiee())
        {
            TempData["Error"] = "Cette demande ne peut plus être modifiée.";
            return Redirect($"/Transport/DemandeTransport?page={PageActuelle}");
        }

        demande.Rejeter(commentaire);

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Demande #{id} rejetée.";
        return Redirect($"/Transport/DemandeTransport?page={PageActuelle}");
    }

    // ===================================================
    // POST — Archiver une demande
    // ===================================================
    public async Task<IActionResult> OnPostArchiverAsync(int id)
    {
        int.TryParse(Request.Form["pageActuelle"], out int page);
        PageActuelle = page == 0 ? 1 : page;

        var demande = await _db.ListeAffectation.FindAsync(id);

        if (demande != null)
        {
            demande.Archiver();
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Demande #{id} archivée.";
        }

        return Redirect($"/Transport/DemandeTransport?page={PageActuelle}");
    }
}