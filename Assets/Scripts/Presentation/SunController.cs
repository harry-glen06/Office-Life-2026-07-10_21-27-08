using UnityEngine;

public class SunController : MonoBehaviour
{
    [Header("Angle (X rotation across the day)")]
    [SerializeField] private float morningAngle = 20f;    // low, sunrise
    [SerializeField] private float eveningAngle = 160f;   // low, sunset (passes ~90 at midday)
    [SerializeField] private float compassAngle = 30f;    // Y — which way the light comes from

    [Header("Colour")]
    [SerializeField] private Color middayColor = Color.white;
    [SerializeField] private Color edgeColor = new Color(1f, 0.6f, 0.3f);   // warm dawn/dusk

    private Light sun;

    void Start()
    {
        sun = GetComponent<Light>();
    }

    // progress: 0 at start of day, 1 at end
    public void SetDayProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        // angle arcs low -> high -> low
        float angle = Mathf.Lerp(morningAngle, eveningAngle, progress);
        transform.rotation = Quaternion.Euler(angle, compassAngle, 0);

        // colour is warm at the edges (dawn/dusk), neutral at midday
        // distance from midday: 0 at noon, 1 at either end
        float edgeness = Mathf.Abs(progress - 0.5f) * 2f;
        sun.color = Color.Lerp(middayColor, edgeColor, edgeness);
    }
}