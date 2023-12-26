using BepInEx;
using BepInEx.Logging;
using JetBrains.Annotations;
using SpaceWarp;
using SpaceWarp.API.Mods;
using KSP.Messages;
using KSP.Game;
using KSP.Modules;
using KerbalHeadlamp.Modules;
using Logger = BepInEx.Logging.Logger;

namespace KerbalHeadlamp;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
public class KerbalHeadlampPlugin : BaseSpaceWarpPlugin
{
    // Useful in case some other mod wants to use this mod a dependency
    [PublicAPI] public const string ModGuid = MyPluginInfo.PLUGIN_GUID;
    [PublicAPI] public const string ModName = MyPluginInfo.PLUGIN_NAME;
    [PublicAPI] public const string ModVer = MyPluginInfo.PLUGIN_VERSION;

    // Singleton instance of the plugin class
    [PublicAPI] public static KerbalHeadlampPlugin Instance { get; set; }


    /// <summary>
    /// Runs when the mod is first initialized.
    /// </summary>
    public override void OnInitialized()
    {
        base.OnInitialized();

        Instance = this;

        SubscribeToMessages();

        Logger.LogInfo($"KerbalHeadlampPlugin Initialized");
    }

    public void SubscribeToMessages() => _ = Subscribe();

    private async Task Subscribe()
    {
        await Task.Delay(100);

        MessageCenter.PersistentSubscribe<KerbalRigLoadComplete>(OnKerbalRigLoadComplete);
        _LOGGER.LogInfo("Subscribed to KerbalRigLoadComplete");
    }

    private void OnKerbalRigLoadComplete(MessageCenterMessage msg)
    {
        var message = msg as KerbalRigLoadComplete;
        Module_Kerbal module_kerbal = message.ownerModule;
        Module_Headlamp module_Headlamp = module_kerbal.GetComponent<Module_Headlamp>();
        if (module_Headlamp != null)
        {
            module_Headlamp.AddHeadlamp(module_kerbal.gameObject);
        }
    }
}

