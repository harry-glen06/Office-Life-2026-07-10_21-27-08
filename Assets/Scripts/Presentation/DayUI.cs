using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DayUI : MonoBehaviour
{
    // Pairing buttons to activities is a UI concern, stays here.
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
    [SerializeField] private List<ActivitySlot> slots;

    // ---------- HUD ----------
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI clockText;
    [SerializeField] private TextMeshProUGUI actionText;
    [SerializeField] private TextMeshProUGUI careerText;
    [SerializeField] private Image energyBarFill;
    [SerializeField] private Image toiletBarFill;
    [SerializeField] private GameObject skillProgressBar;   // the whole bar object
    [SerializeField] private Image skillProgressFill;
    [SerializeField] private TextMeshProUGUI skillProgressLabel;
    
    // ---------- Skills -------
    [Header("Skills")]
    [SerializeField] private GameObject skillPanel;
    [SerializeField] private Transform computerStandPoint;
    [SerializeField] private Button workButton;
    [SerializeField] private Button programmingButton;
    [SerializeField] private Button writingButton;
    [SerializeField] private Button adminButton;
    [SerializeField] private Button scienceButton;

    [SerializeField] private ActivityDefinition workActivity;
    [SerializeField] private ActivityDefinition programmingActivity;
    [SerializeField] private ActivityDefinition writingActivity;
    [SerializeField] private ActivityDefinition adminActivity;
    [SerializeField] private ActivityDefinition scienceActivity;

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

    // ---------- Events ----------
    [Header("Events")]
    [SerializeField] private EventUI eventUI;
    [SerializeField] private List<EventDefinition> triggerEvents;

    // ---------- Character ----------
    [Header("Character")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Transform playerCharacter;
    [SerializeField] private Transform dayStartPoint;

    // ---------- Day cycle & speed ----------
    [Header("Day cycle & speed")]
    [SerializeField] private Button goHomeButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button fastButton;
    [SerializeField] private Button superButton;
    [SerializeField] private float secondsPerMinute = 1f;
    [SerializeField] private float minutesPerUnit = 1f;
    [SerializeField] private SunController sun;
    
    // ---------- sound ----------
    [Header("Sound")]
    [SerializeField] private AudioSource loopSource;      // for working/coffee/toilet
    [SerializeField] private AudioSource oneShotSource;   // for talking, events
    [SerializeField] private AudioSource talkSource;
    [SerializeField] private AudioClip workingLoop;
    [SerializeField] private AudioClip coffeeLoop;
    [SerializeField] private AudioClip toiletLoop;
    [SerializeField] private AudioClip[] talkingClips;    // three, picked at random

    private CharacterPose lastPose = CharacterPose.Idle;

    // ---------- Runtime state ----------
    private GameState gameState;
    private DaySimulation simulation;
    private float secondsAccumulator = 0f;
    private bool isPaused = false;

    private string failureMessage = "";
    private float failureTimer = 0f;

    private Dictionary<CoworkerDefinition, Button> coworkerButtons = new Dictionary<CoworkerDefinition, Button>();
    private Dictionary<CoworkerDefinition, TextMeshProUGUI> relationshipRows = new Dictionary<CoworkerDefinition, TextMeshProUGUI>();
    
    private Transform walkTarget;
    private int totalTravelMinutes;
    private Vector3 walkStart;


    // =====================================================================
    // Setup
    // =====================================================================

    void Start()
    {
        gameState = new GameState();
        gameState.InitCoworkers(coworkers);

        eventUI.Init(gameState);
        eventUI.onEventClosed = OnEventClosed;

        StartNewDay();

        WireButtons();

        BuildCoworkerButtons();
        BuildRelationshipRows();

        coworkerPanel.SetActive(false);
        relationshipPanel.SetActive(false);
        goHomeButton.gameObject.SetActive(false);
        
        workButton.onClick.AddListener(() => OnSkillChosen(workActivity));
        programmingButton.onClick.AddListener(() => OnSkillChosen(programmingActivity));
        writingButton.onClick.AddListener(() => OnSkillChosen(writingActivity));
        adminButton.onClick.AddListener(() => OnSkillChosen(adminActivity));
        scienceButton.onClick.AddListener(() => OnSkillChosen(scienceActivity));

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
        simulation.SetTriggerEvents(triggerEvents);
    }


    // =====================================================================
    // Main loop
    // =====================================================================

    void Update()
    {
        HandleWorldClicks();
        
        if (simulation.IsTravelling && walkTarget != null)
        {
            float smoothProgress = simulation.TravelProgressSmoothed(secondsAccumulator / secondsPerMinute);
            playerCharacter.position = Vector3.Lerp(walkStart, walkTarget.position, smoothProgress);

            Vector3 dir = walkTarget.position - playerCharacter.position;
            dir.y = 0;
            if (dir != Vector3.zero)
                playerCharacter.rotation = Quaternion.LookRotation(dir);
        }
        else if (!simulation.IsTravelling && walkTarget != null)
        {
            // just arrived, face the way the stand-point points
            playerCharacter.rotation = walkTarget.rotation; 
            walkTarget = null;
        }

        // failure messages fade out in real time, even while paused
        if (failureTimer > 0f)
            failureTimer -= Time.deltaTime;

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
            {
                isPaused = true;
                eventUI.ShowEvent(ev);
            }

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
            if (clickable.opensSkillMenu)
            {
                skillPanel.SetActive(true);
                isPaused = true;
                return;
            }
            
            if (clickable.standPoint == null) return;
            
            float distance = Vector3.Distance(playerCharacter.position, clickable.standPoint.position);
            int travelMinutes = Mathf.Max(1, Mathf.RoundToInt(distance * minutesPerUnit));

            ActivityResult result = simulation.StartTravel(clickable.activity, travelMinutes);
            if (result == ActivityResult.Started)
            {
                walkTarget = clickable.standPoint;
                walkStart = playerCharacter.position;
            }
            ReportResult(result);
            UpdateDisplay();
        }
    }

    // Called by EventUI once the player has resolved an event.
    void OnEventClosed()
    {
        isPaused = false;
        UpdateDisplay();
    }


    // =====================================================================
    // Display
    // =====================================================================

    void UpdateDisplay()
    {
        UpdateAudio();
        
        float dayProgress = (simulation.Clock - 540f) / (1020f - 540f);
        sun.SetDayProgress(dayProgress);
        
        playerAnimator.SetInteger("pose", (int)simulation.CurrentPose);
        playerAnimator.SetBool("isTired", simulation.Energy < 30);

        UpdateBar(energyBarFill, simulation.Energy);
        UpdateBar(toiletBarFill, simulation.Toilet);

        clockText.text = $"{gameState.DayName()}, Week {gameState.WeekNumber()}/26\n{FormatTime(simulation.Clock)}";
        careerText.text = $"Career: {simulation.Career}";
        relationshipButton.GetComponentInChildren<TextMeshProUGUI>().text =
            $"Relationships: {simulation.AverageRelationship}";

        // keep the dropdown live while it's open
        if (relationshipPanel.activeSelf)
            RefreshRelationshipRows();
        
        if (simulation.IsBuildingSkill)
        {
            skillProgressBar.SetActive(true);
            skillProgressFill.fillAmount = simulation.CurrentSkillProgress;
            skillProgressLabel.text = $"{simulation.CurrentSkillName} → Level {simulation.CurrentSkillTargetLevel}";
        }
        else
        {
            skillProgressBar.SetActive(false);
        }

        if (simulation.IsDayOver)
        {
            actionText.text = "Day over, go home";
            SetActionButtonsInteractable(false);
            goHomeButton.gameObject.SetActive(true);
        }
        else if (simulation.IsTravelling)
        {
            actionText.text = "Walking...";
            SetActionButtonsInteractable(false);
        }
        else if (simulation.IsBusy)
        {
            actionText.text = $"{simulation.CurrentActivityName} ({simulation.RemainingMinutes} min left)";
            SetActionButtonsInteractable(false);
        }
        else
        {
            // Idle: nothing in progress, enable whatever is affordable.
            actionText.text = "";
            socialiseButton.interactable = true;
            foreach (ActivitySlot slot in slots)
                slot.button.interactable = simulation.CanAfford(slot.activity);
        }

        // a recent failure overrides whatever the state text would have been
        if (failureTimer > 0f)
            actionText.text = failureMessage;
    }
    
    void UpdateAudio()
    {
        CharacterPose pose = simulation.CurrentPose;
        if (pose == lastPose) return;   // only act on change
        lastPose = pose;

        // stop any loop when leaving a looping activity
        loopSource.Stop();
        talkSource.Stop(); 

        if (pose == CharacterPose.Working)
        {
            loopSource.clip = workingLoop;
            loopSource.Play();
        }
        else if (pose == CharacterPose.Drinking)
        {
            loopSource.clip = coffeeLoop;
            loopSource.Play();
        }
        else if (pose == CharacterPose.Toilet)
        {
            loopSource.clip = toiletLoop;
            loopSource.Play();
        }
        else if (pose == CharacterPose.Talking)
        {
            talkSource.clip = talkingClips[Random.Range(0, talkingClips.Length)];
            talkSource.Play();
        }
    }

    // Turns a refused action into something the player can read.
    void ReportResult(ActivityResult result)
    {
        if (result == ActivityResult.Started) return;

        if (result == ActivityResult.TooTired)
            failureMessage = "Too tired for that";
        else if (result == ActivityResult.NotEnoughTime)
            failureMessage = "Not enough time left today";
        else if (result == ActivityResult.AlreadyBusy)
            failureMessage = "Already busy";
        else
            failureMessage = "The day's over";

        failureTimer = 2f;   // seconds on screen
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

    // Pure display formatting, correctly a UI concern.
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
        ReportResult(simulation.DoActivity(activity));
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
        ReportResult(simulation.DoActivity(coworker.talkActivity));
        coworkerPanel.SetActive(false);
        isPaused = false;
        UpdateDisplay();
    }
    
    void OnSkillChosen(ActivityDefinition activity)
    {
        skillPanel.SetActive(false);
        isPaused = false;

        float distance = Vector3.Distance(playerCharacter.position, computerStandPoint.position);
        int travelMinutes = Mathf.Max(1, Mathf.RoundToInt(distance * minutesPerUnit));

        ActivityResult result = simulation.StartTravel(activity, travelMinutes);
        if (result == ActivityResult.Started)
        {
            walkTarget = computerStandPoint;
            walkStart = playerCharacter.position;
        }
        ReportResult(result);
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
    // Day cycle
    // =====================================================================

    void OnGoHomeClicked()
    {
        gameState.dayNumber++;
        gameState.RecoverOvernight();

        StartNewDay();
        playerCharacter.position = dayStartPoint.position;
        playerCharacter.rotation = dayStartPoint.rotation;
        walkTarget = null;   // cancel any leftover walk state

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