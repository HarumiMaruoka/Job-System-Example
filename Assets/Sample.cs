using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public class Sample : MonoBehaviour
{
    private List<Transform> _targetsList;
    private Transform[] _targetsArray;
    private NativeArray<float> _radius;
    private NativeArray<float> _mass;
    private NativeArray<Vector3> _positions;
    private NativeArray<Vector3> _newPositions;
    private TransformAccessArray _transformArray;

    private void Start()
    {
        _targetsList = new List<Transform>();
        GetComponentsInChildren<Transform>(false, _targetsList);
        _targetsList.Remove(transform);
        _targetsArray = _targetsList.ToArray();
    }

    void Update()
    {
        int length = _targetsArray.Length;

        // Allocate Arrays
        _positions = new NativeArray<Vector3>(length, Allocator.TempJob);
        _newPositions = new NativeArray<Vector3>(length, Allocator.TempJob);
        _radius = new NativeArray<float>(length, Allocator.TempJob);
        _mass = new NativeArray<float>(length, Allocator.TempJob);

        for (int i = 0; i < length; i++)
        {
            _positions[i] = _targetsList[i].position;
            _newPositions[i] = _positions[i];
            _radius[i] = 0.5f;
            _mass[i] = 1f;
        }

        // Setup TransformAccessArray
        _transformArray = new TransformAccessArray(_targetsArray);

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

        JobHandle updateHandle = updatePositionJob.Schedule(length, 1);
        JobHandle applyHandle = applyPositionJob.Schedule(_transformArray, updateHandle);

        applyHandle.Complete();

        // Dispose Arrays
        _positions.Dispose();
        _newPositions.Dispose();
        _radius.Dispose();
        _mass.Dispose();
        _transformArray.Dispose();
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
