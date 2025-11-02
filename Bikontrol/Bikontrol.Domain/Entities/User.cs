using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikontrol.Persistence.Entities
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public string FullName { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;


        private User() { }

        public User(string email, string fullName, string passwordHash)
        {
            Id = Guid.NewGuid();
            Email = email;
            FullName = fullName;
            PasswordHash = passwordHash;
            CreatedAt = DateTime.UtcNow;
        }

        // Simple domain validation
        public void UpdatePassword(string newHash)
        {
            PasswordHash = newHash ?? throw new ArgumentNullException(nameof(newHash));
        }
    }
}
