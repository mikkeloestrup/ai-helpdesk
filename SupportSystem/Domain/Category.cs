namespace SupportSystem.Domain;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
