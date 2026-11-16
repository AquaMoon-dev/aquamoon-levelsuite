#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewScatterProfile", menuName = "AquaMoon/Scatter Profile")]
public class ScatterProfile : ScriptableObject
{
    [Serializable]
    public class PrefabEntry
    {
        public GameObject prefab;
        [Range(1, 100)] public int weight = 50;
    }

    [Header("Prefabs Palette")]
    public List<PrefabEntry> palette = new List<PrefabEntry>();

    [Header("Brush Controls")]
    [Range(0.5f, 50f)] public float radius = 5f;
    [Range(1, 30)] public int burstCount = 4;
    [Range(0.1f, 10f)] public float minDistanceBetweenObjects = 0.8f;

    [Header("Slope & Layer Constraints")]
    public LayerMask targetLayers = ~0;
    [Range(0f, 90f)] public float maxSlopeAngle = 45f;

    [Header("Transformation Jitter")]
    public bool alignToNormal = true;
    public bool randomRotationY = true;
    public Vector3 rotationOffset = Vector3.zero;
    [Range(0.1f, 5f)] public float minScale = 0.8f;
    [Range(0.1f, 5f)] public float maxScale = 1.2f;

    public GameObject GetRandomPrefab()
    {
        if (palette == null || palette.Count == 0) return null;

        int totalWeight = 0;
        for (int i = 0; i < palette.Count; i++)
        {
            if (palette[i].prefab != null) totalWeight += palette[i].weight;
        }

        if (totalWeight <= 0) return null;

        int rnd = UnityEngine.Random.Range(0, totalWeight);
        int current = 0;

        for (int i = 0; i < palette.Count; i++)
        {
            if (palette[i].prefab == null) continue;
            current += palette[i].weight;
            if (rnd < current) return palette[i].prefab;
        }

        return palette[0].prefab;
    }
}
#endif