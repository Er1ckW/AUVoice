using HarmonyLib;
using AUVoice.Plugin.Services;
using System.Text.Json;
using System;

namespace AUVoice.Plugin.Patches;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class GameUpdatePatch
{
    private static int _logCounter = 0;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void Postfix(PlayerControl __instance)
    {
        if (__instance != PlayerControl.LocalPlayer) return;

        try
        {
            // Debug log every ~5 seconds (60fps * 5 = 300)
            if (_logCounter++ % 300 == 0)
            {
                AUVoicePlugin.Logger?.LogInfo($"[AUVoice] GameUpdatePatch running. LocalPlayer: {__instance.name}");
            }

            // Collect state
            var state = GameStateService.Instance.CollectGameState();
            
            // Serialize
            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(state, JsonOptions);
            
            // Broadcast
            WebSocketService.Instance.Broadcast(jsonBytes);
        }
        catch (Exception ex)
        {
            if (_logCounter % 300 == 0) // Don't spam errors
            {
                AUVoicePlugin.Logger?.LogError($"[AUVoice] Error in GameUpdatePatch: {ex}");
            }
        }
    }
}
