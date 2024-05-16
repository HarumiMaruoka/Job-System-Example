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
    public Vector2 Position;
    public Vector2 NewPosition;
    public float Radius;
    public float Mass;
}