using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DayUI : MonoBehaviour
{
    // Pairing buttons to activities is a UI concern — stays here.
    [System.Serializable]
    public class ActivitySlot
    {
        public Button button;
        public ActivityDefinition activity;
    }

    [SerializeField] private List<ActivitySlot> slots;
    [SerializeField] private TextMeshProUGUI statusText;

    private DaySimulation simulation;
    private float secondsAccumulator = 0f;

    void Start()
    {
        simulation = new DaySimulation();

        foreach (ActivitySlot slot in slots)
        {
            ActivityDefinition activity = slot.activity;
            slot.button.onClick.AddListener(() => OnActivityClicked(activity));
        }

        UpdateDisplay();
    }

    void Update()
    {
        if (simulation.IsDayOver) return;

        // Presentation concern: convert real seconds into ticks.
        secondsAccumulator += Time.deltaTime;
        if (secondsAccumulator >= 1f)
        {
            secondsAccumulator -= 1f;
            simulation.Tick();       // the CONSEQUENCE lives in the sim
            UpdateDisplay();
        }
    }

    void OnActivityClicked(ActivityDefinition activity)
    {
        simulation.DoActivity(activity);
        UpdateDisplay();
    }

    // Reads the sim's state and draws it. No logic, just display.
    void UpdateDisplay()
    {
        if (simulation.IsDayOver)
        {
            statusText.text = StatsLine() + "\nDay over, go home";
            foreach (ActivitySlot slot in slots)
                slot.button.interactable = false;
            return;
        }

        if (simulation.IsBusy)
        {
            statusText.text = StatsLine() + 
                              $"\nCurrently: {simulation.CurrentActivityName} ({simulation.RemainingMinutes} min left)";
            return;
        }
        
        statusText.text = StatsLine();
    }

    string StatsLine()          // ← moved here, sibling to FormatTime
    {
        return $"Energy: {simulation.Energy}  Career: {simulation.Career}  " +
               $"Rel: {simulation.Relationships}  Time: {FormatTime(simulation.Clock)}";
    }
    
    // Pure display formatting — correctly a UI concern.
    string FormatTime(int minutes)
    {
        int hours = minutes / 60;
        int mins = minutes % 60;
        int displayHours = hours;
        if (hours > 12) displayHours = hours - 12;
        if (hours == 0) displayHours = 12;
        string suffix = hours < 12 ? "AM" : "PM";
        return $"{displayHours}:{mins.ToString("D2")} {suffix}";
    }
}