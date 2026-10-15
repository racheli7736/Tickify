namespace Tickify.Core.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
}
