using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using DunGen.Graph;
using HarmonyLib;

namespace FairerFireExits;

public class FairerFireExitsConfig
{
    internal Dictionary<DungeonFlow, ConfigEntry<bool>> ApplyFireExitChangePerInterior;
    private bool? lethalConfigLoaded;

    public FairerFireExitsConfig(ConfigFile cfg, DungeonFlow[] AllInteriors)
    {
        if (lethalConfigLoaded == null)
            lethalConfigLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(FairerFireExits.LethalConfigGUID);

        ApplyFireExitChangePerInterior = new();

        cfg.SaveOnConfigSet = false;
        foreach (DungeonFlow interior in AllInteriors)
        {
            ConfigEntry<bool> interiorConfig = cfg.Bind(
                 "General",
                 Regex.Replace(interior.name, "Flow$", "", RegexOptions.None),
                 true,
                 "Apply Fairer Fire Exits patch to this interior"
            );

            if (ApplyFireExitChangePerInterior.TryGetValue(interior, out ConfigEntry<bool> config))
            {
                FairerFireExits.Logger.LogError($"{interior.name} has duplicate entries, this shouldn't happen... skipping this entry");
                continue;
            }

            if (lethalConfigLoaded.Value)
                AddLethalConfigItem(interiorConfig);

            ApplyFireExitChangePerInterior[interior] = interiorConfig;
        }

        ClearOrphanedEntries(cfg);
        cfg.Save();
        cfg.SaveOnConfigSet = true;

        if (lethalConfigLoaded.Value)
            ConfigLethalConfigModEntry();
    }

    private void ClearOrphanedEntries(ConfigFile cfg)
    {
        PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
        var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg);
        orphanedEntries.Clear();
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private void AddLethalConfigItem(ConfigEntry<bool> entry)
    {
        LethalConfig.ConfigItems.BoolCheckBoxConfigItem item = new(entry, requiresRestart: false);
        LethalConfig.LethalConfigManager.AddConfigItem(item);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private void ConfigLethalConfigModEntry()
    {
        LethalConfig.LethalConfigManager.SetModDescription("Choose what modded interiors are affected by this mod!");
    }
}
