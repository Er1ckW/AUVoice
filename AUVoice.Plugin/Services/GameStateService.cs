using AUVoice.Plugin.Models;
using System.Collections.Generic;
using InnerNet;

namespace AUVoice.Plugin.Services;

public class GameStateService
{
    private static GameStateService _instance;
    public static GameStateService Instance => _instance ??= new GameStateService();

    // Reusable DTO instance
    private readonly GameStateDto _cachedDto = new()
    {
        Self = new SelfData(),
        Players = new List<PlayerData>(),
        GameData = new GameSpecificData()
    };

    // Pool of PlayerData objects to avoid new allocations
    private readonly List<PlayerData> _playerPool = new();

    public GameStateDto CollectGameState()
    {
        UpdateGameState();
        return _cachedDto;
    }

    private void UpdateGameState()
    {
        if (AmongUsClient.Instance == null)
        {
            _cachedDto.GameState = "MENU";
            _cachedDto.Players.Clear();
            return;
        }

        _cachedDto.GameState = AmongUsClient.Instance.GameState switch
        {
            InnerNetClient.GameStates.Joined => "LOBBY",
            InnerNetClient.GameStates.Started => MeetingHud.Instance != null ? "DISCUSSION" : "TASKS",
            _ => "MENU"
        };

        _cachedDto.GameCode = GameCode.IntToGameName(AmongUsClient.Instance.GameId);
        
        if (GameOptionsManager.Instance != null && GameOptionsManager.Instance.CurrentGameOptions != null)
        {
             _cachedDto.MapId = GameOptionsManager.Instance.CurrentGameOptions.MapId;
        }

        // Update Self
        _cachedDto.Self.ClientId = AmongUsClient.Instance.ClientId;
        _cachedDto.Self.HostId = AmongUsClient.Instance.HostId;

        // Update Players
        _cachedDto.Players.Clear();
        
        if (GameData.Instance != null && GameData.Instance.AllPlayers != null)
        {
            int pIndex = 0;
            foreach (var playerInfo in GameData.Instance.AllPlayers)
            {
                if (playerInfo == null) continue;

                // Ensure pool has enough objects
                if (pIndex >= _playerPool.Count)
                {
                    _playerPool.Add(new PlayerData { Position = new PositionData() });
                }

                var pData = _playerPool[pIndex];
                pIndex++;

                // Update pData fields
                pData.Id = playerInfo.PlayerId;
                pData.ClientId = playerInfo.Object != null ? playerInfo.Object.OwnerId : -1;
                pData.Name = playerInfo.PlayerName;
                pData.ColorId = playerInfo.DefaultOutfit.ColorId;
                pData.HatId = playerInfo.DefaultOutfit.HatId;
                pData.PetId = playerInfo.DefaultOutfit.PetId;
                pData.SkinId = playerInfo.DefaultOutfit.SkinId;
                pData.VisorId = playerInfo.DefaultOutfit.VisorId;
                pData.IsImpostor = playerInfo.Role != null && playerInfo.Role.TeamType == RoleTeamTypes.Impostor;
                pData.IsDead = playerInfo.IsDead;
                pData.Disconnected = playerInfo.Disconnected;
                pData.IsLocal = playerInfo.Object != null && playerInfo.Object.AmOwner;
                pData.InVent = playerInfo.Object != null && playerInfo.Object.inVent;

                if (playerInfo.Object != null)
                {
                    var pos = playerInfo.Object.GetTruePosition();
                    pData.Position.X = pos.x;
                    pData.Position.Y = pos.y;
                }
                else
                {
                    pData.Position.X = 0;
                    pData.Position.Y = 0;
                }

                _cachedDto.Players.Add(pData);
            }
        }

        // Update GameData
        _cachedDto.GameData.CommsSabotaged = IsCommsSabotaged();
        _cachedDto.GameData.MeetingHudState = MeetingHud.Instance ? (int)MeetingHud.Instance.state : 4;
    }

    private static bool IsCommsSabotaged()
    {
        if (ShipStatus.Instance == null || !ShipStatus.Instance.Systems.ContainsKey(SystemTypes.Comms))
        {
            return false;
        }

        var system = ShipStatus.Instance.Systems[SystemTypes.Comms];
        if (system == null) return false;

        // Try casting to HqHudSystemType safely
        var hqSystem = system.TryCast<HqHudSystemType>();
        if (hqSystem != null)
        {
            return hqSystem.IsActive;
        }

        // Potential future support for HudOverrideSystemType or others could go here.
        // For now, return false to prevent crashing.
        return false;
    }
}
