using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Rotate (right-drag)")]
    [SerializeField] private float rotateSpeed = 4f;

    [Header("Pan (WASD)")]
    [SerializeField] private float panSpeed = 10f;
    [SerializeField] private Vector2 panXBounds = new Vector2(-10f, 10f);
    [SerializeField] private Vector2 panZBounds = new Vector2(-10f, 10f);

    [Header("Zoom (scroll)")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 25f;

    private Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
    }

    void Update()
    {
        Rotate();
        Pan();
        Zoom();
    }

    void Rotate()
    {
        if (!Input.GetMouseButton(1)) return;   // right button held

        float h = Input.GetAxis("Mouse X") * rotateSpeed;
        transform.Rotate(Vector3.up, h, Space.World);   // spin the pivot horizontally
    }

    void Pan()
    {
        float x = Input.GetAxis("Horizontal");   // A/D
        float z = Input.GetAxis("Vertical");     // W/S

        // move relative to where the pivot is facing, so W is always "forward on screen"
        Vector3 move = (transform.forward * z + transform.right * x);
        move.y = 0;
        move = move.normalized * panSpeed * Time.deltaTime;

        Vector3 pos = transform.position + move;
        pos.x = Mathf.Clamp(pos.x, panXBounds.x, panXBounds.y);
        pos.z = Mathf.Clamp(pos.z, panZBounds.x, panZBounds.y);
        transform.position = pos;
    }

    void Zoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (scroll == 0f) return;

        // move the camera along its local forward toward/away from the pivot
        float dist = cam.localPosition.magnitude;
        dist = Mathf.Clamp(dist - scroll * zoomSpeed, minZoom, maxZoom);
        cam.localPosition = cam.localPosition.normalized * dist;
    }
}