using System;
using UnityEngine;

public interface ICircle
{
    Transform transform { get; }
    CircleData CircleData { get; }
}

[Serializable]
public struct CircleData
{
    public Vector3 Position;
    public Vector3 NewPosition;
    public float Radius;
    public float Mass;
}