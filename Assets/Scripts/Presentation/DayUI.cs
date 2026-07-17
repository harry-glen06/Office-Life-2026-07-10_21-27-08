using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;   // for List

public class DayUI : MonoBehaviour
{
    private float secondsAccumulator = 0f;
    
    private bool dayOver = false;

    // Pair each button with the activity it triggers.
    // [System.Serializable] lets this show up in the Inspector.
    [System.Serializable]
    public class ActivitySlot
    {
        public Button button;
        public ActivityDefinition activity;
    }

    [SerializeField] private List<ActivitySlot> slots;   // your activities, set in Inspector
    [SerializeField] private TextMeshProUGUI statusText;

    private Employee employee;
    private int clock;

    void Start()
    {
        employee = new Employee();
        clock = 540;

        // Wire every slot's button to the same handler, passing its activity.
        foreach (ActivitySlot slot in slots)
        {
            // capture into a local — explained below, this matters
            ActivityDefinition activity = slot.activity;
            slot.button.onClick.AddListener(() => OnActivityClicked(activity));
        }

        UpdateStatusText();
    }
    
    void Update()
    {
        if (dayOver) return;
        
        if (clock >= 1020)     // 5pm reached
        {
            EndDay();
            return;
        }
        
        secondsAccumulator += Time.deltaTime;   // count real time

        // When a full real second has passed, advance one in-game minute.
        if (secondsAccumulator >= 1f)
        {
            secondsAccumulator -= 1f;   // subtract, don't reset to 0 (keeps the remainder)
            clock += 1;                 // one minute passes
            UpdateStatusText();         // refresh the display
        }
    }

    // ONE handler for ALL activities.
    void OnActivityClicked(ActivityDefinition activity)
    {
        activity.Perform(employee, ref clock);
        UpdateStatusText();
    }

    void UpdateStatusText()
    {
        statusText.text = $"Energy: {employee.energy}  Career: {employee.career}  Rel: {employee.relationships}  Time: {FormatTime(clock)}";
    }
    
    string FormatTime(int minutes)
    {
        int hours = minutes / 60;
        int mins = minutes % 60;

        int displayHours = hours;
        if (hours > 12)
            displayHours = hours - 12;
        if (hours == 0)
            displayHours = 12;

        string suffix = hours < 12 ? "AM" : "PM";
        return $"{displayHours}:{mins.ToString("D2")} {suffix}";
    }
    
    void EndDay()
    {
        dayOver = true;

        // Disable every activity button
        foreach (ActivitySlot slot in slots)
            slot.button.interactable = false;

        // Show the end-of-day message
        statusText.text = "Day over — go home";
    }
}




