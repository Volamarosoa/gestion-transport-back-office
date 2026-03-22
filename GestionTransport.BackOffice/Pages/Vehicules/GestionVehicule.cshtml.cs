using GestionTransport.BackOffice.Data;
using GestionTransport.BackOffice.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace GestionTransport.BackOffice.Pages.Vehicules;

public class GestionVehiculeModel : PageModel
{
    private readonly AppDbContext _db;

    public List<Vehicule> _ListeVehicule { get; set; } = new();

    [BindProperty]
    public IFormFile? FichierExcel { get; set; }

    public int PageActuelle { get; set; } = 1;
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 5; // ← nombre de lignes par page

    [BindProperty]
    public Vehicule Vehicule { get; set; } = new();

    public GestionVehiculeModel(AppDbContext db) => _db = db;

    // ===== LISTE =====
    // public async Task OnGetAsync()
    // {
    //     _ListeVehicule = await _db.ListeVehicule
    //         .Where(v => v.Actif)
    //         .OrderBy(v => v.Matricule)
    //         .ToListAsync();
    // }

    public async Task OnGetAsync()
    {
        // Lit manuellement le paramètre depuis l'URL
        int page = 1;
        if (Request.Query.ContainsKey("page"))
        {
            int.TryParse(Request.Query["page"], out page);
        }
        
        PageActuelle = page;
        Console.WriteLine("page reçue: " + page);
        Console.WriteLine("********************page: " + page);

        var query = _db.ListeVehicule
            .Where(v => v.Actif)
            .OrderBy(v => v.Matricule);

        int total = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        Console.WriteLine("TotalPages: " + TotalPages);

        // ← Ajuste la page si elle dépasse le max
        if (page > TotalPages && TotalPages > 0) page = TotalPages;
        if (page < 1) page = 1;
        PageActuelle = page;

        _ListeVehicule = await query
            .Skip((PageActuelle - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    // ===== CREATE & UPDATE (même handler) =====
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        // ← Lis directement depuis le formulaire
        int.TryParse(Request.Form["pageActuelle"], out int page);
        PageActuelle = page == 0 ? 1 : page;
        Console.WriteLine("****************************PageActuelle reçue : " + PageActuelle);

        if(Vehicule.NombrePlaces < 1 )
            TempData["Error"] =$"Nombre de places ({Vehicule.NombrePlaces}) invalide.";

        Vehicule.Matricule = Vehicule.Matricule?.ToUpper();
        if (Vehicule.Id == 0)
        {
            var existant = await _db.ListeVehicule
                .FirstOrDefaultAsync(v => v.Matricule == Vehicule.Matricule);

        
            if (existant != null && existant.Actif)
            {
                // Matricule déjà utilisé et actif → erreur
                TempData["Error"] = $"Le matricule '{Vehicule.Matricule}' est déjà utilisé par un véhicule actif !";
                return Redirect($"/Vehicules/GestionVehicule?page={PageActuelle}");
            }
            // CREATE
            Vehicule.DateInsertion = DateTime.Now;
            _db.ListeVehicule.Add(Vehicule);
            TempData["Success"] = "Véhicule ajouté avec succès !";
        }
        else
        {
            // UPDATE
            // Vérifie si le nouveau matricule est déjà pris par un AUTRE véhicule actif
            var doublon = await _db.ListeVehicule
                .FirstOrDefaultAsync(v => v.Matricule == Vehicule.Matricule && v.Id != Vehicule.Id && v.Actif);

            if (doublon != null)
            {
                TempData["Error"] = $"Le matricule '{Vehicule.Matricule}' est déjà utilisé par un autre véhicule actif !";
                return Redirect($"/Vehicules/GestionVehicule?page={PageActuelle}");
            }
            var existing = await _db.ListeVehicule.FindAsync(Vehicule.Id);
            if (existing == null) {
                TempData["Error"] = $"L'ID vehicule '{Vehicule.Id}' n'existe pas !";
                return Redirect($"/Vehicules/GestionVehicule?page={PageActuelle}");
            }
            Console.WriteLine(Vehicule.Id + " ==> Status: " + Vehicule.Actif);
            existing.Matricule = Vehicule.Matricule;
            existing.NombrePlaces = Vehicule.NombrePlaces;
            existing.Actif = Vehicule.Actif;
            TempData["Success"] = "Véhicule modifié avec succès !";
        }

        await _db.SaveChangesAsync();
        return Redirect($"/Vehicules/GestionVehicule?page={PageActuelle}");
    }

    // ===== DELETE (soft delete) =====
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        int.TryParse(Request.Form["pageActuelle"], out int page);
        PageActuelle = page == 0 ? 1 : page;
        Console.WriteLine("****************************PageActuelle Delete reçue: " + PageActuelle);
        var v = await _db.ListeVehicule.FindAsync(id);
        if (v != null)
        {
            v.Actif = false;
            v.DateDesactivation = DateTime.Now;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Véhicule supprimé avec succès !";
        }
        return Redirect($"/Vehicules/GestionVehicule?page={PageActuelle}");
    }

    // Import d'un fichier liste de vehicule
    public async Task<IActionResult> OnPostImportAsync()
    {
        int.TryParse(Request.Form["pageActuelle"], out int page);
        PageActuelle = page == 0 ? 1 : page;

        if (FichierExcel == null || FichierExcel.Length == 0)
        {
            TempData["Error"] = "Veuillez sélectionner un fichier Excel !";
            return Redirect($"/Vehicules/GestionVehicule?page={PageActuelle}");
        }

        if (!FichierExcel.FileName.EndsWith(".xlsx"))
        {
            TempData["Error"] = "Le fichier doit être au format .xlsx !";
            return Redirect($"/Vehicules/GestionVehicule?page={PageActuelle}");
        }

        int ajoutes = 0;
        int reactives = 0;
        int modifies = 0;
        int ignores = 0;
        List<string> erreurs = new();

        using var stream = new MemoryStream();
        await FichierExcel.CopyToAsync(stream);

        ExcelPackage.License.SetNonCommercialPersonal("GestionTransport"); // ← fix ici
        using var package = new ExcelPackage(stream);

        var feuille = package.Workbook.Worksheets[0]; // première feuille
        int lignes = feuille.Dimension?.Rows ?? 0;

        for (int i = 2; i <= lignes; i++) // i=2 pour sauter l'en-tête
        {
            string? matricule = feuille.Cells[i, 1].Value?.ToString()?.Trim().ToUpper();
            string? placesStr = feuille.Cells[i, 2].Value?.ToString()?.Trim();

            if (string.IsNullOrEmpty(matricule))
            {
                erreurs.Add($"Ligne {i} : matricule vide, ignorée.");
                ignores++;
                continue;
            }

            if (!int.TryParse(placesStr, out int places) || places < 1)
            {
                erreurs.Add($"Ligne {i} : nombre de places ({places}) invalide pour '{matricule}'.");
                ignores++;
                continue;
            }

            var existant = await _db.ListeVehicule
                .FirstOrDefaultAsync(v => v.Matricule == matricule);

            if (existant != null && existant.Actif)
            {
                if(existant.NombrePlaces != places) {
                    // Déjà actif mais Nb de places differentes → on modifie
                    existant.NombrePlaces = places;
                    modifies++;
                } else {
                    // Déjà actif → on ignore
                    ignores++;
                    Console.WriteLine("************Ignorer : " + matricule);
                }
            }
            else if (existant != null && !existant.Actif)
            {
                // SCD2 → réactivation
                existant.Actif = true;
                existant.NombrePlaces = places;
                existant.DateInsertion = DateTime.Now;
                existant.DateDesactivation = null;
                reactives++;
                Console.WriteLine("************Réactivation : " + matricule);
            }
            else
            {
                // Nouveau
                _db.ListeVehicule.Add(new Vehicule
                {
                    Matricule = matricule,
                    NombrePlaces = places,
                    Actif = true,
                    DateInsertion = DateTime.Now
                });
                ajoutes++;
                Console.WriteLine("************Nouveau : " + matricule);
            }
        }

        await _db.SaveChangesAsync();

        if (erreurs.Any())
            TempData["Error"] = string.Join("\n", erreurs);
        else
            TempData["Success"] = $"Import terminé : {ajoutes} ajouté(s), {modifies} modifié(s), {reactives} réactivé(s), {ignores} ignoré(s).";

        int total = await _db.ListeVehicule.CountAsync();
        int lastPage = (int)Math.Ceiling(total / (double)PageSize);
        return Redirect($"/Vehicules/GestionVehicule?page={lastPage}");
    }
}