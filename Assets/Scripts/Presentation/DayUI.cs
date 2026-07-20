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
    
    [SerializeField] private GameObject coworkerButtonPrefab;   // the prefab to clone
    [SerializeField] private GameObject coworkerPanel; 
    [SerializeField] private Button cancelButton;
    
    [SerializeField] private Button goHomeButton;
    [SerializeField] private Button socialiseButton;
    
    [SerializeField] private List<ActivitySlot> slots;
    [SerializeField] private TextMeshProUGUI statusText;

    [SerializeField] private float secondsPerMinute = 1f; // game speed
    
    private float secondsAccumulator = 0f;
    
    private GameState gameState;
    private DaySimulation simulation;
    
    [SerializeField] private List<CoworkerDefinition> coworkers;
    
    void Start()
    {
        gameState = new GameState();
        gameState.InitCoworkers(coworkers);
        simulation = new DaySimulation(gameState);

        foreach (ActivitySlot slot in slots)
        {
            ActivityDefinition activity = slot.activity;
            slot.button.onClick.AddListener(() => OnActivityClicked(activity));
        }
        
        goHomeButton.onClick.AddListener(OnGoHomeClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
        goHomeButton.gameObject.SetActive(false);
        
        socialiseButton.onClick.AddListener(OnSocialiseClicked);  
        
        BuildCoworkerButtons();
        coworkerPanel.SetActive(false); 

        UpdateDisplay();
    }

    void Update()
    {
        if (simulation.IsDayOver) return;

        // Presentation concern: convert real seconds into ticks.
        secondsAccumulator += Time.deltaTime;
        if (secondsAccumulator >= secondsPerMinute)  
        {
            secondsAccumulator -= secondsPerMinute;
            simulation.Tick();       // the CONSEQUENCE lives in the sim
            UpdateDisplay();
        }
    }

    void OnActivityClicked(ActivityDefinition activity)
    {
        simulation.DoActivity(activity);
        UpdateDisplay();
    }
    
    void OnGoHomeClicked()         
    {
        gameState.dayNumber++;
        gameState.RecoverOvernight(); 
        simulation = new DaySimulation(gameState);

        foreach (ActivitySlot slot in slots)
            slot.button.interactable = true;
        socialiseButton.interactable = true; 
        
        goHomeButton.gameObject.SetActive(false);
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
            socialiseButton.interactable = false;
            goHomeButton.gameObject.SetActive(true);
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

    string StatsLine()         
    {
        return $"{gameState.DayName()}, Week {gameState.WeekNumber()}/26 Energy: {simulation.Energy}  Career: {simulation.Career}  " +
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
    
    void BuildCoworkerButtons()
    {
        foreach (CoworkerDefinition coworker in coworkers)
        {
            // Clone the prefab, put it inside the panel
            GameObject buttonObj = Instantiate(coworkerButtonPrefab, coworkerPanel.transform);
            // Set its label to the coworker's name
            buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = coworker.coworkerName;

            // Wire its click to talk to THIS coworker
            Button btn = buttonObj.GetComponent<Button>();
            CoworkerDefinition c = coworker;   // capture into local — the closure trap again!
            btn.onClick.AddListener(() => OnCoworkerClicked(c));
        }
        cancelButton.transform.SetAsLastSibling();
        
    }

    void OnCoworkerClicked(CoworkerDefinition coworker)
    {
        simulation.DoActivity(coworker.talkActivity);
        coworkerPanel.SetActive(false);
        UpdateDisplay();
    }
    
    void OnSocialiseClicked()         
    {
        coworkerPanel.SetActive(true);
    }
    
    void OnCancelClicked()
    {
        coworkerPanel.SetActive(false);
    }
}