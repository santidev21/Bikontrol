using Bikontrol.Persistence.Entities;
using Bikontrol.Shared.Exceptions;
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

        protected Motorcycle() { }

        public Motorcycle(string name, string brand, int year, string nickname, int km, int displacement, string plate, Guid userId)
        {
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
            Validate();
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new ValidationException("El nombre es obligatorio.");

            if (string.IsNullOrWhiteSpace(Brand))
                throw new ValidationException("La marca es obligatoria.");

            if (string.IsNullOrWhiteSpace(Nickname))
                throw new ValidationException("El apodo es obligatorio.");

            if (string.IsNullOrWhiteSpace(Plate))
                throw new ValidationException("La placa es obligatoria.");

            if (Km < 0)
                throw new ValidationException("El kilometraje no puede ser negativo.");

            int currentYear = DateTime.UtcNow.Year;
            if (Year < 1950 || Year > currentYear + 1)
                throw new ValidationException($"El modelo debe estar entre 1950 y {currentYear + 1}.");

            if (Displacement < 0)
                throw new ValidationException("El cilindraje no puede ser negativo.");
        }

        public void UpdateKm(int newKm)
        {
            if (newKm < Km)
                throw new ValidationException("El nuevo kilometraje no puede ser menor al actual.");
            Km = newKm;
        }
    }
}
