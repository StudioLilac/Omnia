using UnityEngine;

/// <summary>Editor setup inputs stored as Unity references so asset moves and renames are safe.</summary>
[CreateAssetMenu(fileName = "DualGridSetupSettings", menuName = "Tiles/Dual Grid Setup Settings")]
public sealed class DualGridSetupSettings : ScriptableObject
{
    [Tooltip("Terrain used when upgrading ordinary painted tiles. Its sprite and tile sets are used as authored.")]
    public DualGridTerrainTile terrain;
}
