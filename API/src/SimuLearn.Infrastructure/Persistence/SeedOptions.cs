namespace SimuLearn.Infrastructure.Persistence;

/// <summary>
/// Demo data for local development and the pilot demo. Disabled by default in production; the demo
/// password must come from configuration, not the repository.
/// </summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; }

    public string InstitutionName { get; set; } = "Demo Secondary School";

    public string Password { get; set; } = string.Empty;

    public string TeacherEmail { get; set; } = "teacher@simulearn.demo";

    public string StudentEmail { get; set; } = "student@simulearn.demo";

    public string ReviewerEmail { get; set; } = "reviewer@simulearn.demo";

    public string AdminEmail { get; set; } = "admin@simulearn.demo";
}
