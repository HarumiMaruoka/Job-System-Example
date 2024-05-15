using System;
using UnityEngine;

public class Enemy : MonoBehaviour, ICircle
{
    [SerializeField] private CircleData _circleData;

    private void Start()
    {
        _circleData.Transform = transform;
        Sample.Instance.Add(this);
    }

    public CircleData CircleData => _circleData;
}