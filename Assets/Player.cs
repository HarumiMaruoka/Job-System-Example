using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    private void Update()
    {
        transform.position += new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0f) * Time.deltaTime * 8f;
    }
}