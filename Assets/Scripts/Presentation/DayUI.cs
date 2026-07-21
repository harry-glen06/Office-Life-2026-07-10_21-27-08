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
    
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button fastButton;
    [SerializeField] private Button superButton;
    
    [SerializeField] private List<ActivitySlot> slots;
    [SerializeField] private TextMeshProUGUI statusText;

    [SerializeField] private float secondsPerMinute = 1f; // game speed
    
    [SerializeField] private GameObject eventBackdrop;
    [SerializeField] private TextMeshProUGUI eventText;
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private GameObject choiceButtonPrefab;   
    [SerializeField] private EventDefinition testEvent;        // drag Stay Late in, for testing
    
    [SerializeField] private Button testEventButton;
    
    private float secondsAccumulator = 0f;
    private bool isPaused = false;
    
    private GameState gameState;
    private DaySimulation simulation;
    
    [SerializeField] private List<EventDefinition> allEvents;
    
    [SerializeField] private List<CoworkerDefinition> coworkers;
    
    private Dictionary<CoworkerDefinition, Button> coworkerButtons = new Dictionary<CoworkerDefinition, Button>();
    
    void Start()
    {
        gameState = new GameState();
        gameState.InitCoworkers(coworkers);
        simulation = new DaySimulation(gameState);
        
        simulation.ScheduleEventForDay(allEvents);
        
        foreach (ActivitySlot slot in slots)
        {
            ActivityDefinition activity = slot.activity;
            slot.button.onClick.AddListener(() => OnActivityClicked(activity));
        }
        
        goHomeButton.onClick.AddListener(OnGoHomeClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
        goHomeButton.gameObject.SetActive(false);
        
        socialiseButton.onClick.AddListener(OnSocialiseClicked);  
        
        pauseButton.onClick.AddListener(OnPauseClicked);
        playButton.onClick.AddListener(OnPlayClicked);
        fastButton.onClick.AddListener(OnFastClicked);
        superButton.onClick.AddListener(OnSuperClicked);
        UpdateSpeedButtons(playButton);
        
        testEventButton.onClick.AddListener(() => ShowEvent(testEvent));
        
        BuildCoworkerButtons();
        RefreshCoworkerButtons();
        coworkerPanel.SetActive(false); 

        UpdateDisplay();
    }

    void Update()
    {
        if (simulation.IsDayOver) return;
        if (isPaused) return; 
        
        // Presentation concern: convert real seconds into ticks.
        secondsAccumulator += Time.deltaTime;
        if (secondsAccumulator >= secondsPerMinute)  
        {
            secondsAccumulator -= secondsPerMinute;
            simulation.Tick();       // the CONSEQUENCE lives in the sim
            
            // check if the tick scheduled an event to show
            EventDefinition ev = simulation.ConsumePendingEvent();
            if (ev != null)
                ShowEvent(ev);
            
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
        simulation.ScheduleEventForDay(allEvents);

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
        
        foreach (ActivitySlot slot in slots)
            slot.button.interactable = simulation.CanAfford(slot.activity);
        
        statusText.text = StatsLine();
    }

    string StatsLine()         
    {
        return $"{gameState.DayName()}, Week {gameState.WeekNumber()}/26 \nEnergy: {simulation.Energy}  Career: {simulation.Career}  " +
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
            GameObject buttonObj = Instantiate(coworkerButtonPrefab, coworkerPanel.transform);

            Button btn = buttonObj.GetComponent<Button>();
            coworkerButtons[coworker] = btn;        // remember it

            CoworkerDefinition c = coworker;
            btn.onClick.AddListener(() => OnCoworkerClicked(c));
        }

        cancelButton.transform.SetAsLastSibling();
    }
    
    void RefreshCoworkerButtons()
    {
        foreach (var pair in coworkerButtons)
        {
            CoworkerDefinition coworker = pair.Key;
            Button button = pair.Value;

            button.GetComponentInChildren<TextMeshProUGUI>().text =
                $"{coworker.coworkerName} ({gameState.GetRelationship(coworker)})";

            button.interactable = simulation.CanAfford(coworker.talkActivity);
        }
    }

    void OnCoworkerClicked(CoworkerDefinition coworker)
    {
        simulation.DoActivity(coworker.talkActivity);
        coworkerPanel.SetActive(false);
        isPaused = false;  
        UpdateDisplay();
    }
    
    void OnSocialiseClicked()         
    {
        RefreshCoworkerButtons();  
        coworkerPanel.SetActive(true);
        isPaused = true; 
    }
    
    void OnCancelClicked()
    {
        coworkerPanel.SetActive(false);
        isPaused = false;
    }
    
    void OnPauseClicked()  { isPaused = true; UpdateSpeedButtons(pauseButton);}
    void OnPlayClicked()   { isPaused = false; secondsPerMinute = 1f; UpdateSpeedButtons(playButton);}
    void OnFastClicked()   { isPaused = false; secondsPerMinute = 0.3f; UpdateSpeedButtons(fastButton);}
    void OnSuperClicked()  { isPaused = false; secondsPerMinute = 0.05f; UpdateSpeedButtons(superButton);}
    
    void UpdateSpeedButtons(Button active)
    {
        // reset all to normal (unhighlighted)
        pauseButton.GetComponent<Image>().color = Color.white;
        playButton.GetComponent<Image>().color = Color.white;
        fastButton.GetComponent<Image>().color = Color.white;
        superButton.GetComponent<Image>().color = Color.white;

        // highlight the active one in its own color
        if (active == pauseButton)
            active.GetComponent<Image>().color = Color.red;
        else
            active.GetComponent<Image>().color = Color.green;
    }
    
    void ShowEvent(EventDefinition ev)
    {
        isPaused = true;
        eventBackdrop.SetActive(true);
        eventText.text = ev.title + "\n\n" + ev.description;

        // clear old choice buttons (events have different choices)
        foreach (Transform child in choiceContainer)
            Destroy(child.gameObject);

        // one button per choice
        foreach (EventChoice choice in ev.choices)
        {
            GameObject btnObj = Instantiate(choiceButtonPrefab, choiceContainer);
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = choice.label;

            EventChoice c = choice;   // closure capture
            btnObj.GetComponent<Button>().onClick.AddListener(() => OnChoicePicked(c));
        }
    }
    
    void OnChoicePicked(EventChoice choice)
    {
        foreach (Effect effect in choice.effects)
        {
            if (effect.affects == StatType.Career)
            {
                gameState.employee.career += effect.amount;
            }
            else if (effect.affects == StatType.Energy)
            {
                gameState.employee.energy += effect.amount;
                gameState.employee.energy = Mathf.Clamp(gameState.employee.energy, 0, 100);
            }
            else if (effect.affects == StatType.CoworkerRelationship)
            {
                gameState.ChangeRelationship(effect.targetCoworker, effect.amount);
            }
            else // Relationships — everyone
            {
                List<CoworkerDefinition> keys = new List<CoworkerDefinition>(gameState.relationships.Keys);
                foreach (CoworkerDefinition c in keys)
                    gameState.ChangeRelationship(c, effect.amount);
            }
        }

        eventBackdrop.SetActive(false);
        isPaused = false;
        UpdateDisplay();
    }
    
}