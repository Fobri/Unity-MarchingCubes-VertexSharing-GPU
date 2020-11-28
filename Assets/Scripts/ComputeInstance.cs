using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class ComputeInstance : IDisposable
{
    ComputeShader caseCompute;
    int caseKernel;
    ComputeShader vertexCreationCompute;
    int creationKernel;
    ComputeShader vertexSharingCompute;
    int sharingKernel;

    ComputeBuffer vertexBuffer;
    ComputeBuffer argBuffer;
    ComputeBuffer noiseBuffer;
    ComputeBuffer voxelIDBuffer;
    ComputeBuffer tempIndices;
    ComputeBuffer finalIndices;
    ComputeBuffer dispatchArguments;

    CommandBuffer cb;

    Material testMat;

    int size;

    public ComputeBuffer GetRenderArgs()
    {
        return argBuffer;
    }

    public ComputeBuffer GetIndexBuffer()
    {
        return finalIndices;
    }
    public ComputeBuffer GetVertexBuffer()
    {
        return vertexBuffer;
    }

    public void Dispatch(NativeArray<float> noiseMap, Material testMat)
    {
        this.testMat = testMat;
        Reset();
        DispatchInternal(noiseMap);
    }

    void DispatchInternal(NativeArray<float> noiseMap)
    {
        Graphics.ClearRandomWriteTargets();
        Graphics.SetRandomWriteTarget(1, vertexBuffer, false);
        Graphics.SetRandomWriteTarget(2, tempIndices, false);
        Graphics.SetRandomWriteTarget(3, finalIndices, false);
        Graphics.SetRandomWriteTarget(4, argBuffer, false);

        noiseBuffer.SetData(noiseMap);
        caseCompute.SetBuffer(caseKernel, "_noiseMap", noiseBuffer);

        cb.DispatchCompute(caseCompute, caseKernel, size / 8, size / 8, size / 8);
        cb.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.ComputeProcessing);
        cb.CopyCounterValue(voxelIDBuffer, dispatchArguments, 0);
        cb.DispatchCompute(vertexCreationCompute, creationKernel, dispatchArguments, 0);
        cb.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.ComputeProcessing);
        cb.DispatchCompute(vertexSharingCompute, sharingKernel, dispatchArguments, 0);
        //cb.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.ComputeProcessing);
        //cb.CopyCounterValue(finalIndices, argBuffer, 0);
        Graphics.ExecuteCommandBuffer(cb);
        DebugPrint();
    }
    public void DebugPrint()
    {
        uint[] dispatchArgs = new uint[3]
        {
            1, 1, 1
        };
        int[] args = new int[] { 0, 1, 0, 0 };
        //ComputeBuffer.CopyCount(voxelIDBuffer, dispatchArguments, 0);

        //argBuffer.GetData(args);
        //ComputeBuffer.CopyCount(vertexBuffer, dispatchArguments, 0);

        //float[] noise = new float[(size + 1) * (size + 1) * (size + 1)];
        //noiseBuffer.GetData(noise);
        //for(int i = 0; i < noise.Length; i++)
        //{
        // Debug.Log(noise[i]);
        //}
        //ComputeBuffer.CopyCount(finalIndices, dispatchArguments, 0);
        //argBuffer.GetData(dispatchArgs);
        //int[] shit = new int[dispatchArgs[0]];
        //finalIndices.GetData(shit);
        //Debug.Log(dispatchArgs[0]);
        /*for (int i = 0; i < dispatchArgs[0]; i++)
        {
            Debug.Log(shit[i]);
            //Debug.Log(shit[i]);
            uint cubeIndex = shit[i];
            uint z = cubeIndex >> 24;
            uint y = cubeIndex << 8;
            y = y >> 24;
            uint x = cubeIndex << 16;
            x = x >> 24;

            cubeIndex = cubeIndex << 24;
            cubeIndex = cubeIndex >> 24;
            //Debug.Log(y);

            Debug.Log(x.ToString() + "_" + y.ToString() + "_" + z.ToString() + " " + cubeIndex.ToString());
        }*/
        return;
        Vector3[] data = new Vector3[dispatchArgs[0]];
        vertexBuffer.GetData(data);

        ComputeBuffer.CopyCount(finalIndices, dispatchArguments, 0);
        dispatchArguments.GetData(dispatchArgs);
        int[] fuck = new int[dispatchArgs[0]];
        //Debug.Log(dispatchArgs[0]);
        finalIndices.GetData(fuck);
        for(int i = 0; i < dispatchArgs[0]; i++)
        {
            Debug.Log(fuck[i]);
            /*uint cubeIndex = data[i];
            uint z = cubeIndex >> 24;
            uint y = cubeIndex << 8;
            y = y >> 24;
            uint x = cubeIndex << 16;
            x = x >> 24;

            cubeIndex = cubeIndex << 24;
            cubeIndex = cubeIndex >> 24;
            //Debug.Log(y);

            Debug.Log(x.ToString() + "_" + y.ToString() + "_" + z.ToString() + " " + cubeIndex.ToString());*/
        }
    }
    void Reset()
    {
        cb.Clear();
        uint[] dispatchArgs = new uint[3]
        {
            1, 1, 1
        };
        dispatchArguments.SetData(dispatchArgs);
        int[] args = new int[] { 0, 1, 0, 0 };
        argBuffer.SetData(args);
        vertexBuffer.SetCounterValue(0);
        finalIndices.SetCounterValue(0);
        voxelIDBuffer.SetCounterValue(0);
    }

    public ComputeInstance(ComputeShader marchingCubesCaseCompute, ComputeShader vertexCreationCompute, ComputeShader vertexSharingCompute, int size)
    {
        this.caseCompute = marchingCubesCaseCompute;
        this.vertexCreationCompute = vertexCreationCompute;
        this.vertexSharingCompute = vertexSharingCompute;

        this.size = size;
        cb = new CommandBuffer()
        {
            name = "Terrain Compute Instance Buffer"
        };
        caseKernel = marchingCubesCaseCompute.FindKernel("CaseCompute");
        creationKernel = vertexCreationCompute.FindKernel("VertexCreation");
        sharingKernel = vertexSharingCompute.FindKernel("VertexSharing");

        caseCompute.SetFloat("_isoLevel", 0f);
        caseCompute.SetFloat("_gridSize", size);

        vertexCreationCompute.SetFloat("_gridSize", size);

        vertexSharingCompute.SetFloat("_gridSize", size);

        vertexBuffer = new ComputeBuffer((size) * (size) * (size) * 3, sizeof(float) * 6, ComputeBufferType.Counter);
        argBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
        noiseBuffer = new ComputeBuffer((size + 1) * (size + 1) * (size + 1), sizeof(float), ComputeBufferType.Structured);
        voxelIDBuffer = new ComputeBuffer((size + 1) * (size + 1) * (size + 1), sizeof(uint), ComputeBufferType.Counter);
        tempIndices = new ComputeBuffer((size + 1) * (size + 1) * (size + 1), sizeof(uint) * 3, ComputeBufferType.Structured);
        finalIndices = new ComputeBuffer(size * size * size * 3 * 5, sizeof(int), ComputeBufferType.Counter);
        dispatchArguments = new ComputeBuffer(3, sizeof(uint), ComputeBufferType.IndirectArguments);
        uint[] dispatchArgs = new uint[3]
        {
            1, 1, 1
        };
        dispatchArguments.SetData(dispatchArgs);
        int[] args = new int[] { 0, 1, 0, 0 };
        argBuffer.SetData(args);

        caseCompute.SetBuffer(caseKernel, "voxelIDs", voxelIDBuffer);

        vertexCreationCompute.SetBuffer(creationKernel, "vertexBuffer", vertexBuffer);
        vertexCreationCompute.SetBuffer(creationKernel, "voxelIDs", voxelIDBuffer);
        vertexCreationCompute.SetBuffer(creationKernel, "tempIndices", tempIndices);

        vertexSharingCompute.SetBuffer(sharingKernel, "voxelIDs", voxelIDBuffer);
        vertexSharingCompute.SetBuffer(sharingKernel, "tempIndices", tempIndices);
        vertexSharingCompute.SetBuffer(sharingKernel, "indices", finalIndices);
        vertexSharingCompute.SetBuffer(sharingKernel, "argsBuffer", argBuffer);

        

    }
    public void Dispose()
    {
        vertexBuffer.Release();
        argBuffer.Release();
        noiseBuffer.Release();
        voxelIDBuffer.Release();
        tempIndices.Release();
        finalIndices.Release();
        dispatchArguments.Release();
    }
}
