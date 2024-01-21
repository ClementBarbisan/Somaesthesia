using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereDeformation : MonoBehaviour
{
    private Vector3 initPos;

    private void Start()
    {
        initPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 pos = transform.position;
        transform.position = new Vector3(Mathf.PerlinNoise(pos.x, pos.y) * Mathf.Cos(Time.time) * 1.5f,Mathf.PerlinNoise(pos.z, pos.y)* Mathf.Sin(Time.time) * 1.5f,
            Mathf.PerlinNoise(pos.x, pos.z)* Mathf.Cos(Time.time) * Mathf.Sin(Time.time));
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position + initPos, 1f);
    }
}
