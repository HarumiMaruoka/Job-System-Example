using System;
using System.Collections.Generic;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class Sample : MonoBehaviour
{
    public static Sample Instance { get; private set; }

    private List<ICircle> _targetsList = new List<ICircle>();

    private NativeArray<CircleData> _data;
    private TransformAccessArray _transformArray;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public void Add(ICircle circle)
    {
        _targetsList.Add(circle);
        if (Time.frameCount != 1) InitializeArray();
    }

    public void Remove(ICircle circle)
    {
        _targetsList.Remove(circle);
        if (Time.frameCount != 1) InitializeArray();
    }

    private void InitializeArray()
    {
        _data.Dispose();

        int length = _targetsList.Count;

        // Allocate Arrays
        _data = new NativeArray<CircleData>(length, Allocator.Persistent);

        for (int i = 0; i < length; i++)
        {
            var pos = _targetsList[i].transform.position;
            var newPos = pos;
            var rad = _targetsList[i].CircleData.Radius;
            var mass = _targetsList[i].CircleData.Mass;
            _data[i] = new CircleData()
            {
                Position = pos,
                NewPosition = newPos,
                Radius = rad,
                Mass = mass,
            };
        }

        // Setup TransformAccessArray
        _transformArray = new TransformAccessArray(length);
        for (int i = 0; i < length; i++) _transformArray.Add(_targetsList[i].transform);
    }

    private void Update()
    {
        if (Time.frameCount == 1) InitializeArray();

        for (int i = 0; i < _targetsList.Count; i++)
        {
            var circle = _data[i];
            circle.Position = _targetsList[i].transform.position;
            circle.NewPosition = circle.Position;
            _data[i] = circle;
        }

        // Setup and Schedule Jobs
        var copyData = new NativeArray<CircleData>(_data.Length, Allocator.TempJob);
        for (int i = 0; i < copyData.Length; i++) copyData[i] = _data[i];
        UpdatePositionJob updatePositionJob = new UpdatePositionJob()
        {
            frontData = _data,
            backData = copyData,
        };

        ApplyPositionJob applyPositionJob = new ApplyPositionJob()
        {
            newPositions = _data,
        };

        JobHandle updateHandle = updatePositionJob.Schedule(_targetsList.Count, 64);
        JobHandle applyHandle = applyPositionJob.Schedule(_transformArray, updateHandle);

        applyHandle.Complete();

        copyData.Dispose();
    }

    [BurstCompile]
    struct UpdatePositionJob : IJobParallelFor
    {
        public NativeArray<CircleData> frontData;
        [ReadOnly] public NativeArray<CircleData> backData;

        public void Execute(int index)
        {
            float2 myPosition = frontData[index].Position;
            float myRadius = frontData[index].Radius;
            float myMass = frontData[index].Radius;

            for (int i = 0; i < frontData.Length; i++)
            {
                if (index == i) continue;

                float2 neighborPosition = backData[i].Position;
                float neighborRadius = backData[i].Radius;
                float neighborMass = backData[i].Mass;
                float sqrDistance = (myPosition.x - neighborPosition.x) * (myPosition.x - neighborPosition.x) + (myPosition.y - neighborPosition.y) * (myPosition.y - neighborPosition.y); // Vector2.SqrMagnitude(myPosition - neighborPosition);

                if (sqrDistance < (myRadius + neighborRadius) * (myRadius + neighborRadius))
                {
                    // Collision response
                    float overlap = (myRadius + neighborRadius) - math.sqrt(sqrDistance);
                    var diff = neighborPosition - myPosition;
                    Vector2 direction = math.normalize(new float2(diff.x, diff.y));
                    var circle = frontData[index];
                    circle.NewPosition -= direction * (overlap * (neighborMass / (myMass + neighborMass)));
                    frontData[index] = circle;
                }
            }
        }
    }

    [BurstCompile]
    struct ApplyPositionJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<CircleData> newPositions;

        public void Execute(int index, TransformAccess transform)
        {
            transform.position = newPositions[index].NewPosition;
        }
    }
}
