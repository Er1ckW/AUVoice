using System.Collections.Generic;

namespace AUVoice.Plugin.Models;

public class GameStateDto
{
    public string GameState { get; set; }
    public string GameCode { get; set; }
    public int MapId { get; set; }
    public SelfData Self { get; set; }
    public List<PlayerData> Players { get; set; }
    public GameSpecificData GameData { get; set; }
}
