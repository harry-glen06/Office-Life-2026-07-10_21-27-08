using UnityEngine;
using UnityEngine.UI;   // for Button
using TMPro;            // for TextMeshProUGUI


public class DayUI : MonoBehaviour
{
    // --- References dragged in via the Inspector ---
    [SerializeField] private Button workButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button socialiseButton;
    [SerializeField] private Button coffeeButton;

    // --- The simulation state (plain C#, same as DayRunner used) ---
    private Employee employee;
    private int clock;
    private Activity work;
    private Activity socialise;
    private Activity coffee;

    void Start()
    {
        // Set up the sim
        employee = new Employee();
        clock = 540; // 9am
        work = new Activity("Work", 120, 45, StatType.Career, 7);
        socialise = new Activity("Socialise", 60, 25, StatType.Relationships, 3);
        coffee = new Activity("Coffee", 10, -15, StatType.Career, 0); // no stat gain, just energy

        // Wire the button click to our handler.
        // This is the key line: "when workButton is clicked, call OnWorkClicked"
        workButton.onClick.AddListener(OnWorkClicked);
        socialiseButton.onClick.AddListener(OnSocialiseClicked);
        coffeeButton.onClick.AddListener(OnCoffeeClicked);

        // Show the starting state
        UpdateStatusText();
    }
    
    void OnSocialiseClicked()
    {
        socialise.Perform(employee, ref clock);
        UpdateStatusText();
    }
    
    void OnCoffeeClicked()
    {
        coffee.Perform(employee, ref clock);
        UpdateStatusText();
    }

    // Called every time the Work button is clicked
    void OnWorkClicked()
    {
        work.Perform(employee, ref clock);
        UpdateStatusText();
    }

    // Rebuilds the status readout from the current state
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




