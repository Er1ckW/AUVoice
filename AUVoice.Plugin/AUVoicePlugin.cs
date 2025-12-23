using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using AUVoice.Plugin.Services;
using Reactor;
using Reactor.Networking.Attributes;

namespace AUVoice.Plugin;

[BepInPlugin(Id, Name, VersionString)]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
[ReactorModFlags(Reactor.Networking.ModFlags.RequireOnAllClients)]
public partial class AUVoicePlugin : BasePlugin
{
    public const string Id = "com.unicorn.auvoiceplugin";
    public const string Name = "auvoiceplugin";
    public const string VersionString = "0.0.1";

    internal static ManualLogSource Logger;
    private readonly Harmony harmony = new(Id);

    public override void Load()
    {
        Logger = Log;
        
        // Initialize Services
        WebSocketService.Instance.Initialize(Logger);
        
        // Apply Patches
        harmony.PatchAll();
        
        Logger.LogInfo("AUVoice Plugin loaded.");
    }

    public override bool Unload()
    {
        WebSocketService.Instance.Stop();
        harmony.UnpatchSelf();
        Logger.LogInfo("AUVoice Plugin unloaded.");
        return base.Unload();
    }
}
