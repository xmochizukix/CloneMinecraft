using System;
using Unity.Profiling;
using UnityEngine;

[CreateAssetMenu(menuName = "Terrain generator")]
public class TerrainGenerator: ScriptableObject
{
    public float BaseHeight = 8;
    public NoiseOctaveSettings[] Octaves;
    public NoiseOctaveSettings DomainWarp;
        
    [Serializable]
    public class NoiseOctaveSettings
    {
        public FastNoiseLite.NoiseType NoiseType;
        public float Frequency = 0.2f;
        public float Amplitude = 1;
    }

    private FastNoiseLite[] octaveNoises;
    private FastNoiseLite warpNois;

    private static ProfilerMarker GeneratingMarker = new ProfilerMarker(ProfilerCategory.Loading, "Generating");

    public void Init()
    {
        octaveNoises = new FastNoiseLite[Octaves.Length];
        for (int i = 0; i < Octaves.Length; i++)
        {
            octaveNoises[i] = new FastNoiseLite();
            octaveNoises[i].SetNoiseType(Octaves[i].NoiseType);
            octaveNoises[i].SetFrequency(Octaves[i].Frequency);

        }

        warpNois = new FastNoiseLite();
        warpNois.SetNoiseType(DomainWarp.NoiseType);
        warpNois.SetFrequency(DomainWarp.Frequency);
        warpNois.SetDomainWarpAmp(DomainWarp.Amplitude);
    }

    public BlockType[] GenerateTerrain(float xoffset, float zoffset)
    {
        GeneratingMarker.Begin();
        var result = new BlockType[ChunkRenderer.ChunkWidth * ChunkRenderer.ChunkHeight * ChunkRenderer.ChunkWidth];
        
        for (int x = 0; x < ChunkRenderer.ChunkWidth; x++)
        {
            for (int z = 0; z < ChunkRenderer.ChunkWidth; z++)
            {
                float height = GetHeight(x / 4 + xoffset, z / 4 + zoffset);
                float grassLayerHeight = 3;
                
                for (int y = 0; y < height; y++)
                {
                    int index = x + y * ChunkRenderer.ChunkWidthSq + z * ChunkRenderer.ChunkWidth;
                    if (height - y < grassLayerHeight - 2)
                    {
                        result[index] = BlockType.Grass;
                    }
                    else if (height - y < grassLayerHeight)
                    {
                        result[index] = BlockType.Dirt;
                    }
                    else
                    {
                        result[index] = BlockType.Stone;
                    }
                }
            }
        }
        GeneratingMarker.End();
        return result;
    }
    
    private float GetHeight(float x, float y)
    {
        warpNois.DomainWarp(ref x, ref y);
        
        float result = BaseHeight;

        for(int i = 0; i < Octaves.Length; i++)
        {
            float noise = octaveNoises[i].GetNoise(x, y);
            result += noise * Octaves[i].Amplitude / 2;
        }
        
        return result;
    }
}
