using System;
using UnityEngine;
using UnityEngine.Jobs;

public class Enemy : MonoBehaviour, ICircle
{
    [SerializeField] private CircleData _circleData;

    private void Start()
    {
        Sample.Instance.Add(this);
    }

    public CircleData CircleData => _circleData;
}