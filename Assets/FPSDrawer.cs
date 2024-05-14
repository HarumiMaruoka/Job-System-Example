using System;
using UnityEngine;
using UnityEngine.UI;

public class FPSDrawer : MonoBehaviour
{
    [SerializeField]
    private Text _view;
    [SerializeField]
    private float _interval;

    private float _intervalTimer = 0f;

    private void Update()
    {
        _intervalTimer += Time.deltaTime;

        if (_intervalTimer > _interval)
        {
            _intervalTimer = 0f;
            _view.text = $"fps: {(1f / Time.deltaTime).ToString(".0")}";
        }

    }

}