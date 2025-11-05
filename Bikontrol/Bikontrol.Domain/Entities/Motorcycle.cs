using Bikontrol.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Domain.Entities
{
    public class Motorcycle
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public int Km { get; set; }
        public string Image { get; set; } = "default.png";
        public int Displacement { get; set; }
        public string Plate { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;


        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public Motorcycle(string name, string brand, int year, string nickname, int km, int displacement, string plate, Guid userId)
        {
            if (km < 0)
                throw new ArgumentException("El kilometraje no puede ser negativo.", nameof(km));

            int currentYear = DateTime.UtcNow.Year;
            if (year < 1950 || year > currentYear + 1)
                throw new ArgumentException($"El modelo debe estar entre 1950 y {currentYear + 1}.", nameof(year));

            Name = name;
            Brand = brand;
            Year = year;
            Nickname = nickname;
            Km = km;
            Displacement = displacement;
            Plate = plate;
            UserId = userId;
            IsEnabled = true;
            Image = "default.png";
        }

        public void UpdateKm(int newKm)
        {
            if (newKm < Km)
                throw new ArgumentException("El nuevo kilometraje no puede ser menor al actual.");
            Km = newKm;
        }
    }
}
