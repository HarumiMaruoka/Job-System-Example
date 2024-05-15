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

    private NativeArray<float> _radius;
    private NativeArray<float> _mass;
    private NativeArray<Vector3> _positions;
    private NativeArray<Vector3> _newPositions;
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

        _positions.Dispose();
        _newPositions.Dispose();
        _radius.Dispose();
        _mass.Dispose();

        int length = _targetsList.Count;

        // Allocate Arrays
        _positions = new NativeArray<Vector3>(length, Allocator.Persistent);
        _newPositions = new NativeArray<Vector3>(length, Allocator.Persistent);
        _radius = new NativeArray<float>(length, Allocator.Persistent);
        _mass = new NativeArray<float>(length, Allocator.Persistent);

        for (int i = 0; i < length; i++)
        {
            _positions[i] = _targetsList[i].CircleData.Transform.position;
            _newPositions[i] = _positions[i];
            _radius[i] = _targetsList[i].CircleData.Radius;
            _mass[i] = _targetsList[i].CircleData.Mass;
        }

        // Setup TransformAccessArray
        _transformArray = new TransformAccessArray(length);
        for (int i = 0; i < length; i++) _transformArray.Add(_targetsList[i].CircleData.Transform);
    }

    public void Remove(ICircle circle)
    {
        _targetsList.Remove(circle);

        _positions.Dispose();
        _newPositions.Dispose();
        _radius.Dispose();
        _mass.Dispose();

        int length = _targetsList.Count;

        // Allocate Arrays
        _positions = new NativeArray<Vector3>(length, Allocator.Persistent);
        _newPositions = new NativeArray<Vector3>(length, Allocator.Persistent);
        _radius = new NativeArray<float>(length, Allocator.Persistent);
        _mass = new NativeArray<float>(length, Allocator.Persistent);

        for (int i = 0; i < length; i++)
        {
            _positions[i] = _targetsList[i].CircleData.Transform.position;
            _newPositions[i] = _positions[i];
            _radius[i] = _targetsList[i].CircleData.Radius;
            _mass[i] = _targetsList[i].CircleData.Mass;
        }

        // Setup TransformAccessArray
        _transformArray = new TransformAccessArray(length);
        for (int i = 0; i < length; i++) _transformArray.Add(_targetsList[i].CircleData.Transform);
    }

    void Update()
    {
        for (int i = 0; i < _targetsList.Count; i++)
        {
            _positions[i] = _targetsList[i].CircleData.Transform.position;
            _newPositions[i] = _positions[i];
        }

        // Setup and Schedule Jobs
        UpdatePositionJob updatePositionJob = new UpdatePositionJob()
        {
            positions = _positions,
            radius = _radius,
            mass = _mass,
            newPositions = _newPositions
        };

        ApplyPositionJob applyPositionJob = new ApplyPositionJob()
        {
            newPositions = _newPositions
        };

        JobHandle updateHandle = updatePositionJob.Schedule(_targetsList.Count, 64);
        JobHandle applyHandle = applyPositionJob.Schedule(_transformArray, updateHandle);

        applyHandle.Complete();
    }

    [BurstCompile]
    struct UpdatePositionJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> positions;
        [ReadOnly] public NativeArray<float> radius;
        [ReadOnly] public NativeArray<float> mass;
        public NativeArray<Vector3> newPositions;

        public void Execute(int index)
        {
            Vector3 myPosition = positions[index];
            float myRadius = radius[index];
            float myMass = mass[index];

            for (int i = 0; i < positions.Length; i++)
            {
                if (index == i) continue;

                Vector3 neighborPosition = positions[i];
                float neighborRadius = radius[i];
                float neighborMass = mass[i];
                float sqrDistance = Vector3.SqrMagnitude(myPosition - neighborPosition);

                if (sqrDistance < (myRadius + neighborRadius) * (myRadius + neighborRadius))
                {
                    // Collision response
                    float overlap = (myRadius + neighborRadius) - Mathf.Sqrt(sqrDistance);
                    Vector3 direction = (neighborPosition - myPosition).normalized;
                    newPositions[index] -= direction * (overlap * (neighborMass / (myMass + neighborMass)));
                }
            }
        }
    }

    [BurstCompile]
    struct ApplyPositionJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<Vector3> newPositions;

        public void Execute(int index, TransformAccess transform)
        {
            transform.position = newPositions[index];
        }
    }
}
