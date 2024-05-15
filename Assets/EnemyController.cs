using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public class EnemyController : MonoBehaviour
{
    private List<Transform> _enemyList = new List<Transform>();
    private Player _player;
    private TransformAccessArray _transformArray;
    private NativeArray<Vector3> _playerPositionArray;

    private void Start()
    {
        _player = FindObjectOfType<Player>();

        // Get all Enemy transforms including this one
        GetComponentsInChildren<Transform>(false, _enemyList);
        _enemyList.Remove(transform);
        _enemyList.Remove(_player.transform);
        _transformArray = new TransformAccessArray(_enemyList.ToArray(), 20);
    }

    private void Update()
    {
        // Allocate player position array
        _playerPositionArray = new NativeArray<Vector3>(1, Allocator.TempJob);
        _playerPositionArray[0] = _player.transform.position;

        // Schedule job
        MoveToPlayerJob moveToPlayerJob = new MoveToPlayerJob
        {
            playerPosition = _playerPositionArray[0],
            deltaTime = Time.deltaTime
        };

        JobHandle moveHandle = moveToPlayerJob.Schedule(_transformArray);
        moveHandle.Complete();

        // Dispose arrays
        _playerPositionArray.Dispose();
    }

    private void OnDestroy()
    {
        _transformArray.Dispose();
    }

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
