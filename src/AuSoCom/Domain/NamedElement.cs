namespace AuSoCom.Domain;

public abstract class NamedElement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ShortName { get; set; } = string.Empty;
}
