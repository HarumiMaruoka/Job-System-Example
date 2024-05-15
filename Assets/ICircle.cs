using System;
using UnityEngine;

public interface ICircle
{
    CircleData CircleData { get; }
}

[Serializable]
public struct CircleData
{
    [HideInInspector] public Transform Transform;
    public float Radius;
    public float Mass;
}