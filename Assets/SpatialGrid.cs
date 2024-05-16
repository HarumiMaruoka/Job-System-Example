using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public struct SpatialGrid : IDisposable
{
    private NativeParallelMultiHashMap<int2, int> grid;
    private float cellSize;

    public SpatialGrid(float cellSize, int initialCapacity)
    {
        this.cellSize = cellSize;
        grid = new NativeParallelMultiHashMap<int2, int>(initialCapacity, Allocator.Persistent);
    }

    public void Dispose()
    {
        if (grid.IsCreated)
        {
            grid.Dispose();
        }
    }

    public int2 GetGridPosition(Vector3 position)
    {
        return new int2(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

    public void AddObject(Vector3 position, int objectIndex)
    {
        var gridPosition = GetGridPosition(position);
        grid.Add(gridPosition, objectIndex);
    }

    public void Clear()
    {
        grid.Clear();
    }

    public NativeList<int> GetNearbyObjectIndices(Vector3 position, Allocator allocator)
    {
        var gridPosition = GetGridPosition(position);
        NativeList<int> nearbyIndices = new NativeList<int>(allocator);

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                int2 neighborPos = gridPosition + new int2(x, y);
                if (grid.TryGetFirstValue(neighborPos, out int value, out NativeParallelMultiHashMapIterator<int2> iterator))
                {
                    do
                    {
                        nearbyIndices.Add(value);
                    }
                    while (grid.TryGetNextValue(out value, ref iterator));
                }
            }
        }

        return nearbyIndices;
    }
}