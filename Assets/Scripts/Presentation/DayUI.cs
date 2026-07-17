using UnityEngine;
using UnityEngine.UI;   // for Button
using TMPro;            // for TextMeshProUGUI

public class DayUI : MonoBehaviour
{
    // --- References dragged in via the Inspector ---
    [SerializeField] private Button workButton;
    [SerializeField] private TextMeshProUGUI statusText;

    // --- The simulation state (plain C#, same as DayRunner used) ---
    private Employee employee;
    private int clock;
    private Activity work;

    void Start()
    {
        // Set up the sim
        employee = new Employee();
        clock = 540; // 9am
        work = new Activity("Work", 120, 55, StatType.Career, 7);

        // Wire the button click to our handler.
        // This is the key line: "when workButton is clicked, call OnWorkClicked"
        workButton.onClick.AddListener(OnWorkClicked);

        // Show the starting state
        UpdateStatusText();
    }

    // Called every time the Work button is clicked
    void OnWorkClicked()
    {
        // TODO (yours):
        // 1. Call work.Perform(employee, ref clock)  -- remember ref
        // 2. Then call UpdateStatusText() to refresh the display
    }

    // Rebuilds the status readout from current state
    void UpdateStatusText()
    {
        // TODO (yours):
        // Set statusText.text to a string showing energy, career, relationships, and clock.
        // Use string interpolation like in DayRunner:
        // statusText.text = $"Energy: {employee.energy} ...";
    }
}