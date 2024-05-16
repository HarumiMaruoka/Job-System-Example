using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public class EnemyController : MonoBehaviour
{
    private List<Transform> _enemyList = new List<Transform>();
    private Player _player;
    private TransformAccessArray _transformArray;

    private void Start()
    {
        _player = FindObjectOfType<Player>();

        // Get all Enemy transforms
        GetComponentsInChildren<Transform>(false, _enemyList);
        _enemyList.Remove(transform);
        _enemyList.Remove(_player.transform);
        _transformArray = new TransformAccessArray(_enemyList.ToArray(), 20);
    }

    private void Update()
    {
        // Schedule job
        MoveToPlayerJob moveToPlayerJob = new MoveToPlayerJob
        {
            playerPosition = _player.transform.position,
            deltaTime = Time.deltaTime
        };

        JobHandle moveHandle = moveToPlayerJob.Schedule(_transformArray);
        moveHandle.Complete();
    }

    private void OnDestroy()
    {
        _transformArray.Dispose();
    }

    [BurstCompile]
    struct MoveToPlayerJob : IJobParallelForTransform
    {
        [ReadOnly] public Vector3 playerPosition;
        public float deltaTime;

        public void Execute(int index, TransformAccess transform)
        {
            Vector3 direction = (playerPosition - transform.position).normalized;
            transform.position += direction * deltaTime * 4f;
        }
    }
}
