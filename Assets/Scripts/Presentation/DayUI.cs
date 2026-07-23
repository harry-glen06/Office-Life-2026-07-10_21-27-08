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

    // ---------- Data (authored assets) ----------
    [Header("Data")]
    [SerializeField] private List<CoworkerDefinition> coworkers;
    [SerializeField] private List<EventDefinition> allEvents;
    [SerializeField] private EventDefinition accidentEvent;
    [SerializeField] private List<ActivitySlot> slots;

    // ---------- HUD ----------
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private TextMeshProUGUI actionText;
    [SerializeField] private TextMeshProUGUI careerText;
    [SerializeField] private Image energyBarFill;
    [SerializeField] private Image toiletBarFill;

    // ---------- Coworker panel ----------
    [Header("Coworker panel")]
    [SerializeField] private GameObject coworkerPanel;
    [SerializeField] private GameObject coworkerButtonPrefab;
    [SerializeField] private Button socialiseButton;
    [SerializeField] private Button cancelButton;

    // ---------- Relationship dropdown ----------
    [Header("Relationship dropdown")]
    [SerializeField] private Button relationshipButton;
    [SerializeField] private GameObject relationshipPanel;
    [SerializeField] private GameObject relationshipRowPrefab;

    // ---------- Event modal ----------
    [Header("Event modal")]
    [SerializeField] private GameObject eventBackdrop;
    [SerializeField] private TextMeshProUGUI eventText;
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private GameObject choiceButtonPrefab;

    // ---------- Day cycle & speed ----------
    [Header("Day cycle & speed")]
    [SerializeField] private Button goHomeButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button fastButton;
    [SerializeField] private Button superButton;
    [SerializeField] private float secondsPerMinute = 1f;

    // ---------- Runtime state ----------
    private GameState gameState;
    private DaySimulation simulation;
    private float secondsAccumulator = 0f;
    private bool isPaused = false;

    private Dictionary<CoworkerDefinition, Button> coworkerButtons = new Dictionary<CoworkerDefinition, Button>();
    private Dictionary<CoworkerDefinition, TextMeshProUGUI> relationshipRows = new Dictionary<CoworkerDefinition, TextMeshProUGUI>();


    // =====================================================================
    // Setup
    // =====================================================================

    void Start()
    {
        gameState = new GameState();
        gameState.InitCoworkers(coworkers);

        StartNewDay();

        WireButtons();

        BuildCoworkerButtons();
        BuildRelationshipRows();

        coworkerPanel.SetActive(false);
        relationshipPanel.SetActive(false);
        goHomeButton.gameObject.SetActive(false);

        UpdateSpeedButtons(playButton);
        UpdateDisplay();
    }

    void WireButtons()
    {
        foreach (ActivitySlot slot in slots)
        {
            ActivityDefinition activity = slot.activity;
            slot.button.onClick.AddListener(() => OnActivityClicked(activity));
        }

        socialiseButton.onClick.AddListener(OnSocialiseClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
        relationshipButton.onClick.AddListener(OnRelationshipClicked);
        goHomeButton.onClick.AddListener(OnGoHomeClicked);

        pauseButton.onClick.AddListener(OnPauseClicked);
        playButton.onClick.AddListener(OnPlayClicked);
        fastButton.onClick.AddListener(OnFastClicked);
        superButton.onClick.AddListener(OnSuperClicked);
    }

    // Creates a fresh day and gives it everything it needs.
    void StartNewDay()
    {
        simulation = new DaySimulation(gameState);
        simulation.ScheduleEventForDay(allEvents);
        simulation.SetAccidentEvent(accidentEvent);
    }


    // =====================================================================
    // Main loop
    // =====================================================================

    void Update()
    {
        HandleWorldClicks();

        if (simulation.IsDayOver) return;
        if (isPaused) return;

        // Presentation concern: convert real seconds into ticks.
        secondsAccumulator += Time.deltaTime;
        if (secondsAccumulator >= secondsPerMinute)
        {
            secondsAccumulator -= secondsPerMinute;
            simulation.Tick();       // the CONSEQUENCE lives in the sim

            EventDefinition ev = simulation.ConsumePendingEvent();
            if (ev != null)
                ShowEvent(ev);

            UpdateDisplay();
        }
    }

    void HandleWorldClicks()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        ClickableObject clickable = hit.collider.GetComponentInParent<ClickableObject>();
        if (clickable != null)
        {
            simulation.DoActivity(clickable.activity);
            UpdateDisplay();
        }
    }


    // =====================================================================
    // Display
    // =====================================================================

    void UpdateDisplay()
    {
        UpdateBar(energyBarFill, simulation.Energy);
        UpdateBar(toiletBarFill, simulation.toilet);
        
        if (relationshipPanel.activeSelf)
            RefreshRelationshipRows();

        clockText.text = $"{gameState.DayName()}, Week {gameState.WeekNumber()}/26\n{FormatTime(simulation.Clock)}";
        careerText.text = $"Career: {simulation.Career}";
        relationshipButton.GetComponentInChildren<TextMeshProUGUI>().text =
            $"Relationships: {simulation.AverageRelationship}";

        if (simulation.IsDayOver)
        {
            actionText.text = "Day over, go home";
            SetActionButtonsInteractable(false);
            goHomeButton.gameObject.SetActive(true);
            return;
        }

        if (simulation.IsBusy)
        {
            actionText.text = $"{simulation.CurrentActivityName} ({simulation.RemainingMinutes} min left)";
            SetActionButtonsInteractable(false);
            return;
        }

        // Idle: nothing in progress, enable whatever is affordable.
        actionText.text = "";
        socialiseButton.interactable = true;
        foreach (ActivitySlot slot in slots)
            slot.button.interactable = simulation.CanAfford(slot.activity);
    }

    // Sets a bar's fill and colours it by how low the value is.
    void UpdateBar(Image fill, int value)
    {
        fill.fillAmount = (float)value / 100f;

        if (value < 15)
            fill.color = Color.red;
        else if (value < 30)
            fill.color = new Color(1f, 0.6f, 0f);   // amber
        else
            fill.color = Color.green;
    }

    void SetActionButtonsInteractable(bool on)
    {
        socialiseButton.interactable = on;
        foreach (ActivitySlot slot in slots)
            slot.button.interactable = on;
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


    // =====================================================================
    // Activities
    // =====================================================================

    void OnActivityClicked(ActivityDefinition activity)
    {
        simulation.DoActivity(activity);
        UpdateDisplay();
    }


    // =====================================================================
    // Coworker panel (pick someone to talk to)
    // =====================================================================

    void BuildCoworkerButtons()
    {
        foreach (CoworkerDefinition coworker in coworkers)
        {
            GameObject buttonObj = Instantiate(coworkerButtonPrefab, coworkerPanel.transform);

            Button btn = buttonObj.GetComponent<Button>();
            coworkerButtons[coworker] = btn;

            CoworkerDefinition c = coworker;   // capture into local for the lambda
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

    void OnSocialiseClicked()
    {
        RefreshCoworkerButtons();
        coworkerPanel.SetActive(true);
        isPaused = true;
    }

    void OnCoworkerClicked(CoworkerDefinition coworker)
    {
        simulation.DoActivity(coworker.talkActivity);
        coworkerPanel.SetActive(false);
        isPaused = false;
        UpdateDisplay();
    }

    void OnCancelClicked()
    {
        coworkerPanel.SetActive(false);
        isPaused = false;
    }


    // =====================================================================
    // Relationship dropdown (read-only breakdown)
    // =====================================================================

    void BuildRelationshipRows()
    {
        foreach (CoworkerDefinition coworker in coworkers)
        {
            GameObject rowObj = Instantiate(relationshipRowPrefab, relationshipPanel.transform);
            relationshipRows[coworker] = rowObj.GetComponent<TextMeshProUGUI>();
        }
    }

    void RefreshRelationshipRows()
    {
        foreach (var pair in relationshipRows)
            pair.Value.text = $"{pair.Key.coworkerName}: {gameState.GetRelationship(pair.Key)}";
    }

    // Toggles the dropdown open/closed.
    void OnRelationshipClicked()
    {
        bool nowOpen = !relationshipPanel.activeSelf;
        relationshipPanel.SetActive(nowOpen);
        if (nowOpen) RefreshRelationshipRows();
    }


    // =====================================================================
    // Events
    // =====================================================================

    void ShowEvent(EventDefinition ev)
    {
        eventBackdrop.transform.SetAsLastSibling();   // draw on top of everything
        isPaused = true;
        eventBackdrop.SetActive(true);
        eventText.text = ev.title + "\n\n" + ev.description;

        // clear old choice buttons (events have different choices)
        foreach (Transform child in choiceContainer)
            Destroy(child.gameObject);

        foreach (EventChoice choice in ev.choices)
        {
            GameObject btnObj = Instantiate(choiceButtonPrefab, choiceContainer);
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = choice.label;

            EventChoice c = choice;   // capture into local for the lambda
            btnObj.GetComponent<Button>().onClick.AddListener(() => OnChoicePicked(c));
        }
    }

    void OnChoicePicked(EventChoice choice)
    {
        foreach (Effect effect in choice.effects)
            ApplyEffect(effect);

        eventBackdrop.SetActive(false);
        isPaused = false;
        UpdateDisplay();
    }

    void ApplyEffect(Effect effect)
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
            // copy the keys first: ChangeRelationship modifies the dictionary
            List<CoworkerDefinition> keys = new List<CoworkerDefinition>(gameState.relationships.Keys);
            foreach (CoworkerDefinition c in keys)
                gameState.ChangeRelationship(c, effect.amount);
        }
    }


    // =====================================================================
    // Day cycle
    // =====================================================================

    void OnGoHomeClicked()
    {
        gameState.dayNumber++;
        gameState.RecoverOvernight();

        StartNewDay();

        goHomeButton.gameObject.SetActive(false);
        isPaused = false;
        UpdateDisplay();
    }


    // =====================================================================
    // Speed controls
    // =====================================================================

    void OnPauseClicked() { isPaused = true;  UpdateSpeedButtons(pauseButton); }
    void OnPlayClicked()  { isPaused = false; secondsPerMinute = 1f;    UpdateSpeedButtons(playButton); }
    void OnFastClicked()  { isPaused = false; secondsPerMinute = 0.3f;  UpdateSpeedButtons(fastButton); }
    void OnSuperClicked() { isPaused = false; secondsPerMinute = 0.05f; UpdateSpeedButtons(superButton); }

    void UpdateSpeedButtons(Button active)
    {
        pauseButton.GetComponent<Image>().color = Color.white;
        playButton.GetComponent<Image>().color = Color.white;
        fastButton.GetComponent<Image>().color = Color.white;
        superButton.GetComponent<Image>().color = Color.white;

        active.GetComponent<Image>().color = (active == pauseButton) ? Color.red : Color.green;
    }
}