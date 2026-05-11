namespace OpenChat.API.Models;

public class AllowedDomainRequest
{
    public string Domain { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Description { get; set; } = string.Empty;
    public bool AllowSubdomains { get; set; }
    public bool Enabled { get; set; } = true;
}

public class AllowedDomainResponse
{
    public string Id { get; set; } = default!;
    public string Domain { get; set; } = default!;
    public bool Enabled { get; set; }
    public string Category { get; set; } = default!;
    public string Description { get; set; } = string.Empty;
    public bool AllowSubdomains { get; set; }
    public string AddedBy { get; set; } = default!;
    public bool IsSystemDefault { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static AllowedDomainResponse From(AllowedDomain domain) => new()
    {
        Id = domain.Id,
        Domain = domain.Domain,
        Enabled = domain.Enabled,
        Category = domain.Category,
        Description = domain.Description,
        AllowSubdomains = domain.AllowSubdomains,
        AddedBy = domain.AddedBy,
        IsSystemDefault = domain.AddedBy == "system",
        AddedAt = domain.AddedAt,
        UpdatedAt = domain.UpdatedAt
    };
}
