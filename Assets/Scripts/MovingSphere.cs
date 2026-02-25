using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MovingSphere : MonoBehaviour
{
    private float speed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        speed = 1.0f;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += new Vector3(1, 0, 0) * speed * Time.deltaTime;
    }
}
