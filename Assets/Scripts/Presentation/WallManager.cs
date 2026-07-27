using UnityEngine;
using System.Collections.Generic;

public class WallManager : MonoBehaviour
{
    [SerializeField] private List<Transform> walls;
    [SerializeField] private Transform cameraPivot;   // or the camera itself

    void Update()
    {
        foreach (Transform wall in walls)
        {
            // direction from the wall out to the camera
            Vector3 toCamera = Camera.main.transform.position - wall.position;

            // does the wall's outward face point toward the camera?
            float facing = Vector3.Dot(wall.forward, toCamera);

            // facing > 0 means we're on the outer side, looking through it — hide
            wall.gameObject.SetActive(facing <= 0);
        }
    }
}