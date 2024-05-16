using System;
using UnityEngine;

public class Player : MonoBehaviour, ICircle
{
    [SerializeField] private CircleData _circleData;
    public CircleData CircleData => _circleData;

    private void Start()
    {
        Sample.Instance.Add(this);
    }

    private void Update()
    {
        transform.position += new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0f) * Time.deltaTime * 8f;
    }
}