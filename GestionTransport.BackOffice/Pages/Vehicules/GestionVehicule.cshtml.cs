using GestionTransport.BackOffice.Data;
using GestionTransport.BackOffice.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionTransport.BackOffice.Pages.Vehicules;

public class GestionVehiculeModel : PageModel
{
    private readonly AppDbContext _db;

    public List<Vehicule> _ListeVehicule { get; set; } = new();

    public int PageActuelle { get; set; } = 1;
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 3; // ← nombre de lignes par page

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
}