using UnityEngine;
using HarmonyLib;
using DunGen;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace FairerFireExits.Patches;

[HarmonyPatch(typeof(DungeonGenerator))]
internal class DungeonGeneratorPatches
{
    [HarmonyPatch(nameof(DungeonGenerator.ProcessGlobalProps))]
    [HarmonyPrefix]
    private static void PreProcessGlobalProps(DungeonGenerator __instance)
    {
        foreach (Tile tile in __instance.CurrentDungeon.AllTiles)
        {
            GlobalProp[] allProps = tile.GetComponentsInChildren<GlobalProp>();
            foreach (GlobalProp prop in allProps)
            {
                if (prop.PropGroupID == DungeonGeneratorHelper.fireExitGroupID)
                {
                    prop.DepthWeightScale = new AnimationCurve(DungeonGeneratorHelper.fireExitKeyframes);
                }
            }
        }
    }

    [HarmonyPatch(nameof(DungeonGenerator.ProcessGlobalProps))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TranspileProcessGlobalProps(IEnumerable<CodeInstruction> codes)
    {
        CodeMatcher matcher = new(codes);

        matcher.MatchForward(true, new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(TilePlacementData), nameof(TilePlacementData.NormalizedDepth))));
        if (matcher.IsInvalid)
        {
            FairerFireExits.Logger.LogError($"Could not the ending pattern. Aborting {nameof(DungeonGeneratorPatches.TranspileProcessGlobalProps)} transpiler.");
            return codes;
        }
        int endIndex = matcher.Pos;

        matcher.MatchBack(false, new CodeMatch(OpCodes.Ldloc_3));
        if (matcher.IsInvalid)
        {
            FairerFireExits.Logger.LogError($"Could not the starting pattern. Aborting {nameof(DungeonGeneratorPatches.TranspileProcessGlobalProps)} transpiler.");
            return codes;
        }
        matcher.Advance(1);
        int startIndex = matcher.Pos;

        matcher.RemoveInstructionsInRange(startIndex, endIndex);
        matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, 6),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DungeonGeneratorHelper), nameof(DungeonGeneratorHelper.GetNormalizedPathDepthForFireExit)))
        );

        return matcher.InstructionEnumeration();
    }
}

internal static class DungeonGeneratorHelper
{
    public const int fireExitGroupID = 1231;

    public readonly static Keyframe[] fireExitKeyframes =
    {
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.2f, 0f, 0.000007952512f, 0.04168295f),
            new Keyframe(0.5f, 0.3f, 0.000007952512f, 0.04168295f),
            new Keyframe(0.8f, 1f, 0.02613646f, 0.02613646f),
            new Keyframe(1f, 1f, 0.02613646f, 0.02613646f)
    };

    public static float GetNormalizedPathDepthForFireExit(Tile currTile, GlobalProp currProp)
    {
        return (currProp.PropGroupID == fireExitGroupID) ? (currTile.Placement.NormalizedPathDepth) : (currTile.Placement.NormalizedDepth);
    }
}
