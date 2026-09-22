namespace DanchiManager.Models;

public sealed class FloorDef
{
    public int Level { get; set; }
    public List<string> Rooms { get; set; } = [];
}
