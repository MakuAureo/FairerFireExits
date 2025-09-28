using Unity.Netcode;
using UnityEngine;
using static Unity.Netcode.XXHash;

namespace FairerFireExits.Networking;

internal class FFENetworkManager : NetworkBehaviour
{
    private static GameObject prefab = null!;
    public static FFENetworkManager Instance { get; private set; } = null!;

    private const bool default_shouldUseFireExitPatch = true;
    public NetworkVariable<bool> shouldUseFireExitPatch = new(default_shouldUseFireExitPatch);

    public static void CreateAndRegisterPrefab()
    {
        if (prefab != null)
            return;

        prefab = new GameObject(MyPluginInfo.PLUGIN_GUID + " Prefab");
        prefab.hideFlags |= HideFlags.HideAndDontSave;
        NetworkObject networkObject = prefab.AddComponent<NetworkObject>();
        networkObject.GlobalObjectIdHash = prefab.name.Hash32();
        prefab.AddComponent<FFENetworkManager>();
        NetworkManager.Singleton.AddNetworkPrefab(prefab);

        FairerFireExits.Logger.LogInfo("Network prefab created and registered");
    }

    public static void SpawnNetworkHandler()
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
        {
            Object.Instantiate(prefab).GetComponent<NetworkObject>().Spawn();
            FairerFireExits.Logger.LogInfo("Network handler spawned");
        }
    }

    public static void DespawnNetworkHandler()
    {
        if (Instance != null && Instance.gameObject.GetComponent<NetworkObject>().IsSpawned && (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost))
        {
            Instance.gameObject.GetComponent<NetworkObject>().Despawn();
            FairerFireExits.Logger.LogInfo("Network handler despawned");
        }
    }

    private void Awake()
    {
        Instance = this;
    }
}
