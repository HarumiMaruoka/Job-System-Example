using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public class Sample : MonoBehaviour
{
    public static Sample Instance { get; private set; }

    private List<ICircle> _targetsList = new List<ICircle>();

    private NativeArray<CircleData> _data;
    private TransformAccessArray _transformArray;

    private SpatialGrid _spatialGrid;
    private void Awake()
    {
        Instance = this;
        _spatialGrid = new SpatialGrid(1.0f, 1024);
    }

    private void OnDestroy()
    {
        Instance = null;
        _spatialGrid.Dispose();
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

        // グリッドをクリアして再構築
        _spatialGrid.Clear();
        for (int i = 0; i < _data.Length; i++)
        {
            _spatialGrid.AddObject(_data[i].Position, i);
        }

        // Setup and Schedule Jobs
        var copyData = new NativeArray<CircleData>(_data.Length, Allocator.TempJob);
        for (int i = 0; i < copyData.Length; i++) copyData[i] = _data[i];
        UpdatePositionJob updatePositionJob = new UpdatePositionJob()
        {
            frontData = _data,
            backData = copyData,
            spatialGrid = _spatialGrid,
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
        [ReadOnly] public SpatialGrid spatialGrid;

        public void Execute(int index)
        {
            Vector3 myPosition = frontData[index].Position;
            float myRadius = frontData[index].Radius;
            float myMass = frontData[index].Radius;

            var nearbyIndices = spatialGrid.GetNearbyObjectIndices(myPosition, Allocator.TempJob);
            foreach (var i in nearbyIndices)
            {
                if (index == i) continue;

                Vector3 neighborPosition = backData[i].Position;
                float neighborRadius = backData[i].Radius;
                float neighborMass = backData[i].Mass;
                float sqrDistance = Vector3.SqrMagnitude(myPosition - neighborPosition);

                if (sqrDistance < (myRadius + neighborRadius) * (myRadius + neighborRadius))
                {
                    // Collision response
                    float overlap = (myRadius + neighborRadius) - Mathf.Sqrt(sqrDistance);
                    Vector3 direction = (neighborPosition - myPosition).normalized;
                    var circle = frontData[index];
                    circle.NewPosition -= direction * (overlap * (neighborMass / (myMass + neighborMass)));
                    frontData[index] = circle;
                }
            }

            nearbyIndices.Dispose();

            //for (int i = 0; i < frontData.Length; i++)
            //{
            //    if (index == i) continue;

            //    Vector3 neighborPosition = backData[i].Position;
            //    float neighborRadius = backData[i].Radius;
            //    float neighborMass = backData[i].Mass;
            //    float sqrDistance = Vector3.SqrMagnitude(myPosition - neighborPosition);

            //    if (sqrDistance < (myRadius + neighborRadius) * (myRadius + neighborRadius))
            //    {
            //        // Collision response
            //        float overlap = (myRadius + neighborRadius) - Mathf.Sqrt(sqrDistance);
            //        Vector3 direction = (neighborPosition - myPosition).normalized;
            //        var circle = frontData[index];
            //        circle.NewPosition -= direction * (overlap * (neighborMass / (myMass + neighborMass)));
            //        frontData[index] = circle;
            //    }
            //}
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
