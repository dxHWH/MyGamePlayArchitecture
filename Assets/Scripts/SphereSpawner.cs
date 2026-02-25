using GamePlayArchitecture;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using UnityEngine;

public class SphereSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject _sphere;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var sphere = Instantiate(_sphere);
        sphere.AddComponent<MovingSphere>();
        TimerSystem.Instance.CreateTimer(2, 
            () => { 
                if (sphere) { 
                    Log.D("Sphere Destroyed at position " + sphere.transform.position.x.ToString());
                    DestroyImmediate(sphere);
                } 
            },
            timerName : "SphereTimer");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
