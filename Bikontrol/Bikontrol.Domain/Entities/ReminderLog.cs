using Bikontrol.Domain.Entities;
using System;

namespace Bikontrol.Domain.Entities
{
    /// <summary>
    /// Registro de un recordatorio de mantenimiento generado para un usuario.
    /// Sirve de dedupe: un mismo mantenimiento no vuelve a generar el mismo tipo
    /// de recordatorio dentro de la ventana configurada (evita spam), y deja
    /// traza de qué se envió y cuándo.
    /// </summary>
    public class ReminderLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }
        public Guid UserMaintenanceId { get; set; }
        public Guid MotorcycleId { get; set; }

        /// <summary>
        /// Tipo de recordatorio. Hoy solo se usa "Due" (por vencer o vencido);
        /// queda abierto a "Final" para un aviso posterior.
        /// </summary>
        public string Kind { get; set; } = ReminderKind.Due;

        /// <summary>Estado del item al generarse (para diagnosticar el envío).</summary>
        public bool IsOverdue { get; set; }
        public int LifePercent { get; set; }
        public int RemainingKm { get; set; }
        public int RemainingDays { get; set; }

        /// <summary>
        /// Primer canal por el que se entregó ("Pending" mientras ninguno lo
        /// haya hecho). Un recordatorio puede salir por varios canales: la
        /// entrega por canal se lee de <see cref="EmailDeliveredAt"/> y
        /// <see cref="PushDeliveredAt"/>.
        /// </summary>
        public string Channel { get; set; } = ReminderChannel.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Primera entrega por cualquier canal.</summary>
        public DateTime? DeliveredAt { get; set; }

        public DateTime? EmailDeliveredAt { get; set; }
        public DateTime? PushDeliveredAt { get; set; }

        public User? User { get; set; }
        public UserMaintenance? UserMaintenance { get; set; }
    }

    public static class ReminderKind
    {
        public const string Due = "Due";
    }

    public static class ReminderChannel
    {
        public const string Pending = "Pending";
        public const string Email = "Email";
        public const string Push = "Push";
    }
}
