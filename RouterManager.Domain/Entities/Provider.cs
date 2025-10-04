namespace RouterManager.Domain.Entities;

public class Provider
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<RouterModel> RouterModels { get; set; } = new List<RouterModel>();
}