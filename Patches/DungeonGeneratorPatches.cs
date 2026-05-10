using HarmonyLib;
using DunGen;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using System.Reflection.Emit;

namespace FairerFireExits.Patches;

[HarmonyPatch(typeof(DungeonGenerator))]
internal class DungeonGeneratorPatches
{
  [HarmonyPatch(nameof(DungeonGenerator.Generate))]
  [HarmonyPrefix]
  private static void PreGenerate(DungeonGenerator __instance)
  {
    if (!(Networking.FFENetworkManager.Instance.IsServer || Networking.FFENetworkManager.Instance.IsHost))
      return;

    if (FairerFireExits.FireConfig.ApplyFireExitChangePerInterior.TryGetValue(__instance.DungeonFlow, out ConfigEntry<bool> config) && !config.Value)
    {
      FairerFireExits.Logger.LogInfo($"Found current DungeonFlow ({__instance.DungeonFlow.name}) config but patch is set to false");
      Networking.FFENetworkManager.Instance.shouldUseFireExitPatch.Value = false;

      return;
    }

    Networking.FFENetworkManager.Instance.shouldUseFireExitPatch.Value = true;
  }

  [HarmonyPatch(nameof(DungeonGenerator.ProcessGlobalProps))]
  [HarmonyPrefix]
  private static void PreProcessGlobalProps(DungeonGenerator __instance)
  {
    if (!Networking.FFENetworkManager.Instance.shouldUseFireExitPatch.Value)
      return;

    DungeonGeneratorHelper.allFireProps.Clear();
    DungeonGeneratorHelper.allExitPlacements.Clear();

    foreach (Tile tile in __instance.CurrentDungeon.AllTiles)
    {
      GlobalProp[] allProps = tile.GetComponentsInChildren<GlobalProp>();
      foreach (GlobalProp prop in allProps)
      {
        if (prop.PropGroupID == DungeonGeneratorHelper.fireExitGroupID)
        {
          DungeonGeneratorHelper.allFireProps.Add(prop);
        }
      }

      SpawnSyncedObject exitSpawn = tile.GetComponentInChildren<SpawnSyncedObject>();
      if (exitSpawn == null)
        continue;

      if (exitSpawn.spawnPrefab.name == "EntranceTeleportA")
      {
        DungeonGeneratorHelper.allExitPlacements.Add(tile.Placement.NormalizedPathDepth);
      }
    }
  }

  [HarmonyPatch(nameof(DungeonGenerator.ProcessGlobalProps))]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> TranspileProcessGlobalProps(IEnumerable<CodeInstruction> codes, ILGenerator ilgen)
  {
    CodeMatcher matcher = new(codes, ilgen);

    Label loopContinue = ilgen.DefineLabel();
    matcher.End().MatchBack(false, new CodeMatch(OpCodes.Ldloc_1)).Advance(-7).Instruction.labels.Add(loopContinue);

    Label targetIfNotFire = ilgen.DefineLabel();
    CodeInstruction[] fireExitLogic =
    {
      new CodeInstruction(OpCodes.Ldloc_S, 13),
      new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DunGen.Graph.DungeonFlow.GlobalPropSettings), nameof(DunGen.Graph.DungeonFlow.GlobalPropSettings.ID))),
      new CodeInstruction(OpCodes.Ldc_I4, DungeonGeneratorHelper.fireExitGroupID),
      new CodeInstruction(OpCodes.Bne_Un, targetIfNotFire),
      new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DungeonGeneratorHelper), nameof(DungeonGeneratorHelper.PlaceFireOptimally))),
      new CodeInstruction(OpCodes.Br, loopContinue)
    };

    matcher.MatchBack(false, new CodeMatch(OpCodes.Br));
    CodeInstruction loopStart = matcher.Advance(1).Instruction;
    fireExitLogic[0].labels.AddRange(loopStart.labels);
    loopStart.labels.Clear();
    loopStart.labels.Add(targetIfNotFire);

    return matcher.Insert(fireExitLogic).InstructionEnumeration();
  }
}

internal static class DungeonGeneratorHelper
{
  public const int fireExitGroupID = 1231;
  public static List<float> allExitPlacements = new();
  public static List<GlobalProp> allFireProps = new();

  public static void PlaceFireOptimally()
  {
    float bestDistance = 0f;
    float bestPlacement = 1f;
    int bestPlacementIdx = -1;
    for (int i = 0; i < allFireProps.Count; i++)
    {
      float minDist = 100f;
      float currPlacement = allFireProps[i].gameObject.GetComponentInParent<Tile>(includeInactive: false).Placement.NormalizedPathDepth;
      foreach (float placement in allExitPlacements)
        minDist = Mathf.Min(minDist, Mathf.Abs(currPlacement - placement));
      
      if (minDist > bestDistance)
      {
        bestDistance = minDist;
        bestPlacement = currPlacement;
        bestPlacementIdx = i;
      }
    }

    if (bestPlacementIdx != -1)
    {
      FairerFireExits.Logger.LogInfo($"Found best fire exit placement: {bestPlacement}");
      allExitPlacements.Add(bestPlacement);
      allFireProps[bestPlacementIdx].gameObject.SetActive(true);
    }
  }
}
