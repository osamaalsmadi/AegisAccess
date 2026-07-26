namespace AegisAccess.Domain.Entities;
public class Resource {
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string SensitivityLevel { get; set; } = "Low";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}