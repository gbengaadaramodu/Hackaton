using SimuLearn.Domain.Common;

namespace SimuLearn.Domain.Identity;

public sealed class Institution : Entity
{
    public required string Name { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code (for example "NG").</summary>
    public required string CountryCode { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Opaque configuration blob for institutional policies (retention, publishing rules).</summary>
    public string ConfigurationJson { get; set; } = "{}";

    public List<User> Users { get; set; } = [];
}
