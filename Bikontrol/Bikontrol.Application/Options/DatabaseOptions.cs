namespace Bikontrol.Application.Options
{
    /// <summary>Database startup behaviour (<c>Database</c> section).</summary>
    public class DatabaseOptions
    {
        public const string Section = "Database";

        /// <summary>Production applies migrations as a dedicated deploy step and turns this off.</summary>
        public bool RunMigrationsOnStartup { get; set; } = true;
    }
}
