using GestionTransport.BackOffice.Data;
using GestionTransport.BackOffice.Models.Affectation;
using GestionTransport.BackOffice.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionTransport.BackOffice.Pages.Transport;

public class HistoriqueTransportModel : PageModel
{
    private readonly AppDbContext _db;

    public List<Affectation> _ListeDemandes { get; set; } = new();

    public int PageActuelle { get; set; } = 1;
    public int TotalPages   { get; set; }
    public int PageSize      { get; set; } = 3;

    [BindProperty(SupportsGet = true)]
    public string? Recherche { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateTransportFiltre { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? TypeTransportFiltre { get; set; }

    public HistoriqueTransportModel(AppDbContext db) => _db = db;

    // ===================================================
    // GET — Liste des demandes validées
    // ===================================================
    public async Task OnGetAsync()
    {
        int page = 1;
        if (Request.Query.ContainsKey("page"))
            int.TryParse(Request.Query["page"], out page);

        DateTime debutJournee = DateTime.Today;
        if (DateTransportFiltre.HasValue)
            debutJournee = DateTransportFiltre.Value.Date;

        var query = _db.ListeAffectation
            .Where(a =>
                a.EstValidee == true &&
                a.DateTransport!.Value.Date == debutJournee
            )
            .Include(a => a.Employe)
            .Include(a => a.Adresse)
            .Include(a => a.TypeTransport)
            .Include(a => a.Site)
            .Include(a => a.Vehicule)
            .Include(a => a.HeureTransport)
            .Include(a => a.TypeAffectation)
            .AsQueryable();

        if (TypeTransportFiltre.HasValue)
            query = query.Where(a => a.TypeTransport!.Id == TypeTransportFiltre.Value);

        if (!string.IsNullOrWhiteSpace(Recherche))
        {
            string terme = Recherche.Trim().ToLower();
            query = query.Where(a =>
                a.Employe!.Nom.ToLower().Contains(terme)    ||
                a.Employe!.Prenom.ToLower().Contains(terme) ||
                a.Employe!.Matricule!.ToLower().Contains(terme)
            );
        }

        query = query.OrderByDescending(a => a.DateCreation);

        int total = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(total / (double)PageSize);

        if (page < 1) page = 1;
        if (page > TotalPages && TotalPages > 0) page = TotalPages;
        PageActuelle = page;

        _ListeDemandes = await query
            .Skip((PageActuelle - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    // Affectation automatique des véhicules
    public async Task<IActionResult> OnPostAffectationAutoAsync()
    {
        int.TryParse(Request.Form["pageActuelle"], out int page);
        PageActuelle = page == 0 ? 1 : page;

        DateTime maintenant = DateTime.Now;
        DateTime aujourd_hui = DateTime.Today;

        DateTime? dateFiltre = null;
        if (DateTime.TryParse(Request.Form["dateTransportFiltre"], out DateTime df))
            dateFiltre = df.Date;

        int? typeFiltre = null;
        if (int.TryParse(Request.Form["typeTransportFiltre"], out int tf))
            typeFiltre = tf;

        Console.WriteLine($"dateFiltre: {dateFiltre}");
        Console.WriteLine($"typeFiltre: {typeFiltre}");

        // ── Charger toutes les affectations validées éligibles ──────
        // Retour  : DateTransport >= aujourd'hui
        // Ramassage : DateTransport + HeureTransport >= maintenant
        Console.WriteLine("*******************************************************************************************");
        var demandes = await _db.ListeAffectation
            .Where(a =>
                a.EstValidee == true  
            )
            .Include(a => a.Employe)
            .Include(a => a.Adresse)
            .Include(a => a.TypeTransport)
            .Include(a => a.HeureTransport)
            .Include(a => a.Site)
            .Where(a => !dateFiltre.HasValue
                || a.DateTransport!.Value.Date == dateFiltre.Value)
            .Where(a => !typeFiltre.HasValue
                || a.TypeTransport!.Id == typeFiltre.Value)
            .ToListAsync();
        Console.WriteLine("################################################################################");

        // ── Filtrer selon le type de transport ──────────────────────
        var demandesEligibles = demandes.Where(a =>
        {
            if (a.DateTransport == null) return false;

            bool estRamassage = a.TypeTransport?.Libelle?.ToLower().Contains("Retour") == true
                             || a.TypeTransport?.Libelle?.ToLower().Contains("Aller") == true;

            if (estRamassage)
            {
                // Ramassage : heure de transport doit être >= maintenant
                var heure = a.HeureTransport?.Heure ?? TimeSpan.Zero;
                var dateHeure = a.DateTransport.Value.Date + heure;
                return dateHeure >= maintenant;
            }
            else
            {
                // Retour : date >= aujourd'hui suffit
                return a.DateTransport.Value.Date >= aujourd_hui;
            }
        }).ToList();

        if (!demandesEligibles.Any())
        {
            TempData["Error"] = "Affectation invalide, date de transport déja archivée.";
            return Redirect($"/Transport/HistoriqueTransport?page={PageActuelle}&recherche={Recherche}&dateTransportFiltre={dateFiltre?.ToString("yyyy-MM-dd")}&typeTransportFiltre={typeFiltre}");
        }

        // ── Charger les véhicules actifs ────────────────────────────
        var vehicules = await _db.ListeVehicule
            .Where(v => v.Actif)
            .OrderByDescending(v => v.NombrePlaces)
            .ToListAsync();

        if (!vehicules.Any())
        {
            TempData["Error"] = "Aucun véhicule actif disponible.";
            return Redirect($"/Transport/HistoriqueTransport?page={PageActuelle}&recherche={Recherche}&dateTransportFiltre={dateFiltre?.ToString("yyyy-MM-dd")}&typeTransportFiltre={typeFiltre}");
        }

        // ── Grouper les demandes par (Site, TypeTransport, HeureTransport, Date) ─
        var groupes = demandesEligibles
            .GroupBy(a => new
            {
                a.IdSite,
                a.IdTypeTransport,
                a.IdHeureTransport,
                Date = a.DateTransport!.Value.Date
            })
            .ToList();

        int totalAffectes = 0;
        int totalGroupes  = 0;

        foreach (var groupe in groupes)
        {
            var passagers = groupe.ToList();

            // ── Trier les passagers par distance décroissante (plus loin = ramassé en premier) ──
            passagers = passagers
                .OrderByDescending(a => CalculerDistance(
                    a.Adresse?.Latitude ?? 0, a.Adresse?.Longitude ?? 0,
                    a.Site?.Latitude    ?? 0, a.Site?.Longitude    ?? 0))
                .ToList();

            // ── Affecter les véhicules : algorithme d'insertion par coût ──
            var vehiculesDispos = vehicules.ToList();
            var affectations = AlgorithmeAffectation(passagers, vehiculesDispos);

            foreach (var (affectation, vehicule) in affectations)
            {
                affectation.IdVehicule = vehicule.Id;
                totalAffectes++;
            }

            totalGroupes++;
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Affectation automatique terminée : {totalAffectes} affectation(s) traitée(s) dans {totalGroupes} groupe(s).";
       return Redirect($"/Transport/HistoriqueTransport?page={PageActuelle}&recherche={Recherche}&dateTransportFiltre={dateFiltre?.ToString("yyyy-MM-dd")}&typeTransportFiltre={typeFiltre}");
    }

    // affecter chaque passager au véhicule dont le coût d'insertion est minimal
    private List<(Affectation, Vehicule)> AlgorithmeAffectation(
        List<Affectation> passagers,
        List<Vehicule> vehicules)
    {
        // ── État : capacité restante par véhicule ───────────────────
        var capaciteRestante = vehicules.ToDictionary(v => v.Id, v => v.NombrePlaces);
        var resultats = new List<(Affectation, Vehicule)>();
        var nonAffectes = new List<Affectation>();

        foreach (var passager in passagers)
        {
            Vehicule? meilleurVehicule = null;
            double meilleurCout = double.MaxValue;

            foreach (var vehicule in vehicules)
            {
                // Véhicule plein → ignorer
                if (capaciteRestante[vehicule.Id] <= 0) continue;

                double cout = CalculerCoutInsertion(passager, vehicule, resultats);

                if (cout < meilleurCout)
                {
                    meilleurCout  = cout;
                    meilleurVehicule = vehicule;
                }
            }

            if (meilleurVehicule != null)
            {
                resultats.Add((passager, meilleurVehicule));
                capaciteRestante[meilleurVehicule.Id]--;
            }
            else
            {
                nonAffectes.Add(passager);
            }
        }

        // ── Passagers non affectés : forcer dans le véhicule le moins plein ──
        foreach (var passager in nonAffectes)
        {
            var vehiculeMoinsPlein = vehicules
                .OrderByDescending(v => capaciteRestante[v.Id])
                .FirstOrDefault();

            if (vehiculeMoinsPlein != null)
            {
                resultats.Add((passager, vehiculeMoinsPlein));
                // On autorise le dépassement en dernier recours
            }
        }

        return resultats;
    }

    // ── Coût d'insertion d'un passager dans un véhicule ─────────────
    // On pénalise la distance détour + le sous-remplissage
    private double CalculerCoutInsertion(
        Affectation passager,
        Vehicule vehicule,
        List<(Affectation, Vehicule)> affectationsActuelles)
    {
        // Passagers déjà dans ce véhicule
        var dejaDans = affectationsActuelles
            .Where(x => x.Item2.Id == vehicule.Id)
            .Select(x => x.Item1)
            .ToList();

        int nbDans = dejaDans.Count;
        int capacite = vehicule.NombrePlaces;

        if (nbDans >= capacite) return double.MaxValue;

        // Distance du nouveau passager vers le site
        double distPassager = CalculerDistance(
            passager.Adresse?.Latitude  ?? 0, passager.Adresse?.Longitude  ?? 0,
            passager.Site?.Latitude     ?? 0, passager.Site?.Longitude     ?? 0);

        // Pénalité de sous-remplissage (après ajout)
        double ratio = (double)(nbDans + 1) / capacite; 
        double penalite = ratio < 0.34 ? 20.0
                        : ratio < 0.50 ? 10.0
                        : ratio < 0.75 ? 3.0
                        : 0.0;

        // Coût = distance + pénalité sous-remplissage
        return 4.0 * distPassager + penalite;
    }

    // ── Haversine : distance en km entre deux coordonnées GPS ────────
    private static double CalculerDistance(
        decimal lat1d, decimal lon1d,
        decimal lat2d, decimal lon2d)
    {
        double lat1 = (double)lat1d;
        double lon1 = (double)lon1d;
        double lat2 = (double)lat2d;
        double lon2 = (double)lon2d;

        const double R = 6371.0;
        double p1 = Math.PI * lat1 / 180;
        double p2 = Math.PI * lat2 / 180;
        double dp = Math.PI * (lat2 - lat1) / 180;
        double dl = Math.PI * (lon2 - lon1) / 180;
        double a  = Math.Sin(dp / 2) * Math.Sin(dp / 2)
                  + Math.Cos(p1) * Math.Cos(p2)
                  * Math.Sin(dl / 2) * Math.Sin(dl / 2);
        return 2 * R * Math.Asin(Math.Sqrt(a));
    }

    // ===================================================
    // POST — Valider une demande
    // ===================================================
    public async Task<IActionResult> OnPostValiderAsync(int id, string? commentaire)
    {
        int.TryParse(Request.Form["pageActuelle"], out int page);
        PageActuelle = page == 0 ? 1 : page;

        var demande = await _db.ListeAffectation.FindAsync(id);
        if (demande == null)
        {
            TempData["Error"] = $"Demande #{id} introuvable !";
            return Redirect($"/Transport/HistoriqueTransport?page={PageActuelle}");
        }

        demande.Valider(commentaire);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Demande #{id} validée avec succès !";
        return Redirect($"/Transport/HistoriqueTransport?page={PageActuelle}");
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
            return Redirect($"/Transport/HistoriqueTransport?page={PageActuelle}");
        }

        demande.Rejeter(commentaire);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Demande #{id} rejetée.";
        return Redirect($"/Transport/HistoriqueTransport?page={PageActuelle}");
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

        return Redirect($"/Transport/HistoriqueTransport?page={PageActuelle}");
    }
}