using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class Chunk : IDisposable
{
    Material myMat;
    byte mapSize;

    float3 pos;
    
    NativeArray<float> noiseMap;

    JobHandle noiseJob;
    bool generating = false;
    bool dispatchIssued = false;

    Bounds bounds;
    Matrix4x4 matrix;

    public ComputeInstance computeInstance;
    
    public void Render()
    {
        myMat.SetBuffer("indices", computeInstance.GetIndexBuffer());
        myMat.SetBuffer("vertices", computeInstance.GetVertexBuffer());

        Graphics.DrawProceduralIndirect(myMat, bounds, MeshTopology.Triangles, computeInstance.GetRenderArgs());
    }

    public void Generate(float3 pos)
    {
        noiseJob.Complete();
        this.pos = pos;
        matrix = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
        myMat.SetMatrix("model", matrix);
        myMat.SetBuffer("indices", computeInstance.GetIndexBuffer());
        myMat.SetBuffer("vertices", computeInstance.GetVertexBuffer());
        var noise = new NoiseJob()
        {
            noiseMap = noiseMap,
            surfaceLevel = 50f,
            freq = 0.01f,
            ampl = 1.5f,
            oct = 6,
            offset = pos,
            size = mapSize + 1,
            seed = 0
        };
        noiseJob = noise.Schedule((mapSize + 1) * (mapSize + 1) * (mapSize + 1), 64);
        generating = true;
    }
    public void CheckGenerationStates()
    {
        if (!generating) return;
        
        if (noiseJob.IsCompleted)
        {
            noiseJob.Complete();
            UpdateBuffers();
            generating = false;
        }
    }
    void UpdateBuffers()
    {
        
        computeInstance.Dispatch(noiseMap, myMat);
        //noiseJob.Complete();
        //noiseBuffer.SetData(noiseMap);
        //computeShader.SetBuffer(kernelMC, "_noiseMap", noiseBuffer);
        //appendVertexBuffer.SetCounterValue(0);
        /*cb.Clear();

        cb.DispatchCompute(computeShader, kernelMC, mapSize / 8, mapSize / 8, mapSize / 8);
        cb.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.ComputeProcessing);
        cb.CopyCounterValue(appendVertexBuffer, argBuffer, 0);
        Graphics.ExecuteCommandBuffer(cb);

        myMat.SetBuffer("triangles", appendVertexBuffer);
        myMat.SetMatrix("model", matrix);
        //dispatchIssued = true;
        return;*/

        
       // computeShader.Dispatch(kernelMC, mapSize / 8, mapSize / 8, mapSize / 8);
        
        bounds = new Bounds(pos + mapSize / 2, new float3((int)mapSize));

        //OnDataReceived();
    }
    /*void OnDataReceived()
    {
        int[] args = new int[] { 0, 1, 0, 0 };
        argBuffer.SetData(args);

        ComputeBuffer.CopyCount(appendVertexBuffer, argBuffer, 0);
        
        argBuffer.GetData(args);
        args[0] *= 3;
        argBuffer.SetData(args);

        myMat.SetPass(0);
        myMat.SetBuffer("triangles", appendVertexBuffer);
        myMat.SetMatrix("model", matrix);
        Debug.Log("Vertex count:" + args[0]);
    }*/
    public void Dispose()
    {
        computeInstance.Dispose();
        noiseJob.Complete();
        noiseMap.Dispose();
    }
    public Chunk(Material materialInstance, byte size, ComputeInstance ci, float3 pos)
    {
        mapSize = size;
        myMat = materialInstance;

        noiseMap = new NativeArray<float>((size + 1) * (size + 1) * (size + 1), Allocator.Persistent);

        computeInstance = ci;
        
        Generate(pos);
    }
}
