using System.ComponentModel.DataAnnotations;
namespace GestionTransport.BackOffice.Models.Transport
{
    public class DateTransport
    {
        [Key]
        public int Id { get; set; }

        public DateTime DateJour { get; set; }

        public bool Actif { get; set; } = true;

        public bool EstAujourdhui() => DateJour.Date == DateTime.Today;

        public bool EstDansLeFutur() => DateJour.Date > DateTime.Today;

        public bool EstDansLePasse() => DateJour.Date < DateTime.Today;

        public string FormatDate() => DateJour.ToString("dd/MM/yyyy");

        public string FormatDateLongue() => DateJour.ToString("dddd dd MMMM yyyy");

        public DayOfWeek JourSemaine() => DateJour.DayOfWeek;

        public bool EstWeekend() => DateJour.DayOfWeek == DayOfWeek.Saturday 
                                    || DateJour.DayOfWeek == DayOfWeek.Sunday;
    }
}