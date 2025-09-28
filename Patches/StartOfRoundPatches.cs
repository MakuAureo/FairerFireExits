using HarmonyLib;

namespace FairerFireExits.Patches;

[HarmonyPatch(typeof(StartOfRound))]
internal class StartOfRoundPatches
{
    [HarmonyPatch(nameof(StartOfRound.Awake))]
    [HarmonyPrefix]
    private static void PreAwake(StartOfRound __instance)
    {
        Networking.FFENetworkManager.SpawnNetworkHandler();
    }
}
