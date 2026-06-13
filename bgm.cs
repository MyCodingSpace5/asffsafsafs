using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ChunkGenerator : MonoBehaviour
{
    private const int CHUNK_SIZE = 16;
    private const int CHUNK_HEIGHT = 128;

    private ushort[,,] chunkData;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private List<UnityEngine.Vector3> vertices = new List<UnityEngine.Vector3>();
    private List<int> triangles = new List<int>();
    private List<UnityEngine.Vector2> uvs = new List<UnityEngine.Vector2>();

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        chunkData = new ushort[CHUNK_SIZE, CHUNK_HEIGHT, CHUNK_SIZE];
    }

    private void Start()
    {
        PopulateChunkWithNoise();
        GenerateGreedyMesh();
    }

    private void PopulateChunkWithNoise()
    {
        for (int x = 0; x < CHUNK_SIZE; x++)
        {
            for (int z = 0; z < CHUNK_SIZE; z++)
            {
                int surfaceHeight = Mathf.FloorToInt(Mathf.PerlinNoise(x * 0.1f, z * 0.1f) * 30) + 10;

                for (int y = 0; y < CHUNK_HEIGHT; y++)
                {
                    if (y > surfaceHeight)       chunkData[x, y, z] = 0;
                    else if (y == surfaceHeight) chunkData[x, y, z] = 3;
                    else if (y > surfaceHeight - 4) chunkData[x, y, z] = 2;
                    else                         chunkData[x, y, z] = 1;
                }
            }
        }
    }

    public ushort GetBlockAt(int x, int y, int z)
    {
        if (x < 0 || x >= CHUNK_SIZE || y < 0 || y >= CHUNK_HEIGHT || z < 0 || z >= CHUNK_SIZE)
        {
            return 0; 
        }
        return chunkData[x, y, z];
    }

    private void GenerateGreedyMesh()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();

        uint[] mask = new uint[CHUNK_SIZE];
        ushort[] uniqueBlocksToRender = new ushort[] { 1, 2, 3 }; 

        foreach (ushort targetBlock in uniqueBlocksToRender)
        {
            for (int y = 0; y < CHUNK_HEIGHT; y++)
            {
                for (int z = 0; z < CHUNK_SIZE; z++)
                {
                    uint rowMask = 0;
                    for (int x = 0; x < CHUNK_SIZE; x++)
                    {
                        bool isTarget = GetBlockAt(x, y, z) == targetBlock;
                        bool isAirAbove = GetBlockAt(x, y + 1, z) == 0;

                        if (isTarget && isAirAbove)
                        {
                            rowMask |= (1U << x);
                        }
                    }
                    mask[z] = rowMask;
                }

                for (int z = 0; z < CHUNK_SIZE; z++)
                {
                    while (mask[z] != 0)
                    {
                        int startX = BitOperations.TrailingZeroCount(mask[z]);
                        
                        int width = 1;
                        while ((startX + width) < CHUNK_SIZE && (mask[z] & (1U << (startX + width))) != 0)
                        {
                            width++;
                        }

                        uint quadMask = ((1U << width) - 1) << startX;

                        int height = 1;
                        while ((z + height) < CHUNK_SIZE && (mask[z + height] & quadMask) == quadMask)
                        {
                            height++;
                        }

                        AddTopQuad(startX, y + 1, z, width, height);

                        for (int h = 0; h < height; h++)
                        {
                            mask[z + h] &= ~quadMask;
                        }
                    }
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "GreedyChunkMesh";
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uvs = uvs.ToArray();
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    private void AddTopQuad(float x, float y, float z, float width, float height)
    {
        int vOffset = vertices.Count;

        vertices.Add(new UnityEngine.Vector3(x, y, z));
        vertices.Add(new UnityEngine.Vector3(x + width, y, z));
        vertices.Add(new UnityEngine.Vector3(x, y, z + height));
        vertices.Add(new UnityEngine.Vector3(x + width, y, z + height));

        uvs.Add(new UnityEngine.Vector2(0, 0));
        uvs.Add(new UnityEngine.Vector2(width, 0));
        uvs.Add(new UnityEngine.Vector2(0, height));
        uvs.Add(new UnityEngine.Vector2(width, height));

        triangles.Add(vOffset + 0);
        triangles.Add(vOffset + 2);
        triangles.Add(vOffset + 1);

        triangles.Add(vOffset + 1);
        triangles.Add(vOffset + 2);
        triangles.Add(vOffset + 3);
    }
}
