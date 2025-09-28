using DunGen.Graph;
using UnityEngine;
using HarmonyLib;

namespace FairerFireExits.Patches;

[HarmonyPatch(typeof(GameNetworkManager))]
internal class GameNetworkManagerPatches
{
    [HarmonyPatch(nameof(GameNetworkManager.Start))]
    [HarmonyPostfix]
    private static void PostStart(GameNetworkManager __instance)
    {
        Networking.FFENetworkManager.CreateAndRegisterPrefab();

        DungeonFlow[] allInteriors = Resources.FindObjectsOfTypeAll<DungeonFlow>();
        FairerFireExits.FireConfig = new(FairerFireExits.Instance.Config, allInteriors);
    }

    [HarmonyPatch(nameof(GameNetworkManager.Disconnect))]
    [HarmonyPrefix]
    private static void PreDisconnect(GameNetworkManager __instance)
    {
        Networking.FFENetworkManager.DespawnNetworkHandler();
    }
}
