using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct NoiseJob : IJobParallelFor
{
    [ReadOnly] public float surfaceLevel;
    [ReadOnly] public float3 offset;
    [ReadOnly] public float3 seed;
    [ReadOnly] public float ampl;
    [ReadOnly] public float freq;
    [ReadOnly] public int oct;

    [NativeDisableParallelForRestriction, WriteOnly]
    public NativeArray<float> noiseMap;
    [ReadOnly] public int size;



    public void Execute(int index)
    {
        //int3 localPos = new int3(index / (chunkSize * chunkSize), index / chunkSize % chunkSize, index % chunkSize);
        noiseMap[index] = FinalNoise(new float3(index / (size * size), index / size % size, index % size));
    }
    float FinalNoise(float3 pos)
    {
        float value = 0f;
        if (SurfaceNoise2D(pos.x, pos.z) > pos.y + offset.y - surfaceLevel && SurfaceNoise2D(pos.x + seed.x, pos.z + seed.z) > -pos.y - offset.y - surfaceLevel)
        {
            value = 1f;//SurfaceNoise2D(pos.x, pos.z);
            value -= PerlinNoise3D(pos.x, pos.y, pos.z);
            if (value < 1f)
            {
                value += PerlinNoise3DSnake(pos.x, pos.y, pos.z);
            }
            //value += -pos.y + pos.y % 2f;
        }
        value = -value;
        return value;
    }
    float PerlinNoise3D(float x, float y, float z)
    {
        float total = 0;
        var ampl = this.ampl;
        var freq = this.freq;
        for (int i = 0; i < oct; i++)
        {
            total += noise.snoise(math.float3((x + offset.x + seed.x) * freq, (y + offset.y + seed.y) * freq, (z + offset.z + seed.z) * freq) * ampl);

            ampl *= 2;
            freq *= 0.5f;
        }
        total -= total % 2.5f;
        return total;
    }
    float PerlinNoise3DSnake(float x, float y, float z)
    {
        float total = 0;
        var ampl = this.ampl;
        var freq = this.freq + 0.03f;
        for (int i = 0; i < oct; i++)
        {
            total += noise.snoise(math.float3((x + offset.x + seed.x) * freq, (y + offset.y + seed.y) * freq, (z + offset.z + seed.z) * freq) * ampl);

            ampl *= 2;
            freq *= 0.5f;
        }
        total -= total % 2.5f;
        return total;
    }
    float SurfaceNoise2D(float x, float z)
    {
        float total = 0;
        var ampl = this.ampl;
        var freq = this.freq;
        for (int i = 0; i < oct; i++)
        {
            total += noise.snoise(math.float2((x + offset.x + seed.x) * freq, (z + offset.z + seed.z) * freq) * ampl);

            ampl *= 2;
            freq *= 0.5f;
        }
        total = total % 5f;
        return total;
    }
}