using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;   // for List

public class DayUI : MonoBehaviour
{
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
}




