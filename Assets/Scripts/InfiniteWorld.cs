using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class InfiniteWorld : MonoBehaviour
{
    public float chunkDrawDistance;
    HashSet<float3> newChunks = new HashSet<float3>();
    List<float3> toRemove = new List<float3>();

    public static Dictionary<float3, Chunk> currentChunks = new Dictionary<float3, Chunk>();
    public static Queue<Chunk> freeChunks = new Queue<Chunk>();

    public byte chunkSize = 0;
    public Material sourceMat;
    public Transform player;
    public ComputeShader marchingCubesCaseCompute;
    public ComputeShader vertexCreationCompute;
    public ComputeShader vertexSharingCompute;

    Vector3 lastPos;

    //public static List<CommandBuffer> cbs = new List<CommandBuffer>();

    private void Start()
    {
        StartGenerate();
        UpdateChunks();
    }
    void StartGenerate()
    {
        int amount = Mathf.RoundToInt(chunkDrawDistance / (chunkSize));
        float rootPosX = Mathf.RoundToInt(player.position.x / (chunkSize)) * (chunkSize);
        float rootPosY = Mathf.RoundToInt(player.position.y / (chunkSize)) * (chunkSize);
        float rootPosZ = Mathf.RoundToInt(player.position.z / (chunkSize)) * (chunkSize);
        for (int x = -amount / 2; x < amount / 2; x++)
        {
            for (int y = -amount / 2; y < amount / 2; y++)
            {
                for (int z = -amount / 2; z < amount / 2; z++)
                {
                    var pos = new float3(rootPosX + x * (chunkSize), rootPosY + y * (chunkSize), rootPosZ + z * (chunkSize));
                    if (Vector3.Distance(pos, player.position + Vector3.up * 3) < chunkDrawDistance / 2)
                    {
                        Material mat = new Material(sourceMat);
                        currentChunks.Add(pos, new Chunk(mat, chunkSize, new ComputeInstance(Instantiate(marchingCubesCaseCompute), Instantiate(vertexCreationCompute), Instantiate(vertexSharingCompute), chunkSize), pos));
                    }

                }
            }
        }
    }
    private void Update()
    {
        if (Vector3.Distance(lastPos, player.position) > chunkSize)
        {
            //if (currentlyGenerated.Count == 0)
            //{
            UpdateChunks();
            lastPos = player.position;
            return;
            //}
        }
        bool print = Input.GetKeyDown(KeyCode.Space);
        //Graphics.ClearRandomWriteTargets();
        foreach(var chunk in currentChunks.Values)
        {
            if (print)
                chunk.computeInstance.DebugPrint();
            chunk.CheckGenerationStates();
            chunk.Render();
        }
    }
    void PoolChunk(float3 pos)
    {
        freeChunks.Enqueue(currentChunks[pos]);
        currentChunks.Remove(pos);
    }
    public void UpdateChunks()
    {
        //newChunks.Clear();
        toRemove.Clear();
            
        foreach(var fuck in currentChunks.Keys)
        {
            if (Vector3.Distance(fuck, player.position + Vector3.up * 3) > chunkDrawDistance / 2)
            {
                //currentChunks[fuck].gameObject.SetActive(false);
                toRemove.Add(fuck);
            }
        }
        toRemove.ForEach(x => PoolChunk(x));
        int amount = Mathf.RoundToInt(chunkDrawDistance / (chunkSize));
        float rootPosX = Mathf.RoundToInt(player.position.x / (chunkSize)) * (chunkSize);
        float rootPosY = Mathf.RoundToInt(player.position.y / (chunkSize)) * (chunkSize);
        float rootPosZ = Mathf.RoundToInt(player.position.z / (chunkSize)) * (chunkSize);
        for (int x = -amount / 2; x < amount / 2; x++)
        {
            for (int y = -amount / 2; y < amount / 2; y++)
            {
                for (int z = -amount / 2; z < amount / 2; z++)
                {
                    if (freeChunks.Count == 0)
                        return;
                    var pos = new float3(rootPosX + x * (chunkSize), rootPosY + y * (chunkSize), rootPosZ + z * (chunkSize));
                    if (currentChunks.ContainsKey(pos))
                        continue;
                    if (Vector3.Distance(pos, player.position + Vector3.up * 3) < chunkDrawDistance / 2)
                    {
                        var chumk = freeChunks.Dequeue();
                        chumk.Generate(pos);
                        currentChunks.Add(pos, chumk);
                    }
                }
            }
        }
    }
    private void OnApplicationQuit()
    {
        foreach(var chunk in currentChunks.Values)
        {
            chunk.Dispose();
        }
        currentChunks.Clear();
        freeChunks.Clear();
    }
}