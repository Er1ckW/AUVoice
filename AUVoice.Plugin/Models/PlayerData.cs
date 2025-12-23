namespace AUVoice.Plugin.Models;

public class PositionData
{
    public float X { get; set; }
    public float Y { get; set; }
}

public class PlayerData
{
    public byte Id { get; set; }
    public long ClientId { get; set; }
    public string Name { get; set; }
    public int ColorId { get; set; }
    public string HatId { get; set; }
    public string PetId { get; set; }
    public string SkinId { get; set; }
    public string VisorId { get; set; }
    public bool IsImpostor { get; set; }
    public bool IsDead { get; set; }
    public bool Disconnected { get; set; }
    public bool IsLocal { get; set; }
    public PositionData Position { get; set; }
    public bool InVent { get; set; }
}
