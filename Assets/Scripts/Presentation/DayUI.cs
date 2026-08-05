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
    [SerializeField] private GameObject[] skillBars;      // bar containers (pool)
    [SerializeField] private Image[] skillBarFills;       // their fills
    [SerializeField] private TextMeshProUGUI[] skillBarLabels;
    [SerializeField] private TextMeshProUGUI levelUpText;
    [SerializeField] private AudioClip levelUpChime;

    // ---------- End Screen ----------
    [Header("End screen")]
    [SerializeField] private GameObject endScreenPanel;
    [SerializeField] private TextMeshProUGUI endScreenText;
    [SerializeField] private Button playAgainButton;

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
    [SerializeField] private AudioSource loopSource;
    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private AudioSource talkSource;
    [SerializeField] private AudioClip workingLoop;
    [SerializeField] private AudioClip coffeeLoop;
    [SerializeField] private AudioClip toiletLoop;
    [SerializeField] private AudioClip[] talkingClips;

    // ---------- Win condition ----------
    [Header("Win condition")]
    [SerializeField] private int winCareerThreshold = 900;
    [SerializeField] private int winAvgRelationshipThreshold = 200;
    [SerializeField] private int winBossRelationshipThreshold = 150;
    [SerializeField] private CoworkerDefinition bossDefinition;
    [SerializeField] private int finalDay = 130;   // 130 = week 26, day 5

    private CharacterPose lastPose = CharacterPose.Idle;

    // ---------- Runtime state ----------
    private GameState gameState;
    private DaySimulation simulation;
    private float secondsAccumulator = 0f;

    private bool isPaused = false;    // the PLAYER paused the clock
    private bool modalOpen = false;   // a blocking UI (menu/event/end) is up

    private string failureMessage = "";
    private float failureTimer = 0f;

    private Dictionary<CoworkerDefinition, Button> coworkerButtons = new Dictionary<CoworkerDefinition, Button>();
    private Dictionary<CoworkerDefinition, TextMeshProUGUI> relationshipRows = new Dictionary<CoworkerDefinition, TextMeshProUGUI>();

    private Transform walkTarget;
    private Vector3 walkStart;

    private Dictionary<SkillType, int> lastSkillLevels = new Dictionary<SkillType, int>();
    private string levelUpMessage = "";
    private float levelUpTimer = 0f;


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
        endScreenPanel.SetActive(false);
        modalOpen = false;

        workButton.onClick.AddListener(() => OnSkillChosen(workActivity));
        programmingButton.onClick.AddListener(() => OnSkillChosen(programmingActivity));
        writingButton.onClick.AddListener(() => OnSkillChosen(writingActivity));
        adminButton.onClick.AddListener(() => OnSkillChosen(adminActivity));
        scienceButton.onClick.AddListener(() => OnSkillChosen(scienceActivity));
        playAgainButton.onClick.AddListener(OnPlayAgain);

        foreach (SkillType type in System.Enum.GetValues(typeof(SkillType)))
            lastSkillLevels[type] = gameState.GetSkillLevel(type);

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

        // speed keys only when no blocking UI is up
        if (!modalOpen)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (isPaused) OnPlayClicked();
                else OnPauseClicked();
            }
            if (Input.GetKeyDown(KeyCode.Alpha1)) OnPlayClicked();
            if (Input.GetKeyDown(KeyCode.Alpha2)) OnFastClicked();
            if (Input.GetKeyDown(KeyCode.Alpha3)) OnSuperClicked();
        }

        // walking is visual — runs regardless of pause
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
            playerCharacter.rotation = walkTarget.rotation;   // face the stand-point on arrival
            walkTarget = null;
        }

        // real-time timers, even while paused
        if (failureTimer > 0f)
            failureTimer -= Time.deltaTime;

        if (levelUpTimer > 0f)
        {
            levelUpTimer -= Time.deltaTime;
            levelUpText.gameObject.SetActive(true);
            levelUpText.text = levelUpMessage;
        }
        else
        {
            levelUpText.gameObject.SetActive(false);
        }

        if (simulation.IsDayOver) return;
        if (isPaused || modalOpen) return;

        // convert real seconds into ticks
        secondsAccumulator += Time.deltaTime;
        if (secondsAccumulator >= secondsPerMinute)
        {
            secondsAccumulator -= secondsPerMinute;
            simulation.Tick();

            EventDefinition ev = simulation.ConsumePendingEvent();
            if (ev != null)
            {
                modalOpen = true;
                eventUI.ShowEvent(ev);
            }
            
            // advance the queue if we just went idle
            if (!simulation.IsBusy && !simulation.IsTravelling && taskQueue.Count > 0)
            {
                QueuedTask next = taskQueue[0];
                taskQueue.RemoveAt(0);
                StartTravelTo(next.activity, next.standPoint);
            }

            UpdateDisplay();
        }
    }

    void HandleWorldClicks()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        // clicking UI (a button) shouldn't also raycast the world
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        ClickableObject clickable = hit.collider.GetComponentInParent<ClickableObject>();
        if (clickable != null)
        {
            if (clickable.opensSkillMenu)
            {
                skillPanel.SetActive(true);
                modalOpen = true;
                return;
            }

            if (clickable.standPoint == null) return;
            
            // Busy or travelling? Queue it instead of starting now.
            if (simulation.IsBusy || simulation.IsTravelling)
            {
                if (taskQueue.Count < maxQueued)
                {
                    taskQueue.Add(new QueuedTask { activity = clickable.activity, standPoint = clickable.standPoint });
                }
                else
                {
                    failureMessage = "Queue is full";
                    failureTimer = 2f;
                }
                UpdateDisplay();
                return;
            }
            
            // Idle, start travelling now.
            StartTravelTo(clickable.activity, clickable.standPoint);
            UpdateDisplay();
        }
    }
    
    void StartTravelTo(ActivityDefinition activity, Transform standPoint)
    {
        float distance = Vector3.Distance(playerCharacter.position, standPoint.position);
        int travelMinutes = Mathf.Max(1, Mathf.RoundToInt(distance * minutesPerUnit));

        ActivityResult result = simulation.StartTravel(activity, travelMinutes);
        if (result == ActivityResult.Started)
        {
            walkTarget = standPoint;
            walkStart = playerCharacter.position;
        }
        ReportResult(result);
    }
    void OnEventClosed()
    {
        modalOpen = false;
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
        UpdateSkillBars();

        clockText.text = $"{gameState.DayName()}, Week {gameState.WeekNumber()}/26\n{FormatTime(simulation.Clock)}";
        careerText.text = $"Career: {simulation.Career}";
        relationshipButton.GetComponentInChildren<TextMeshProUGUI>().text =
            $"Relationships: {simulation.AverageRelationship}";

        if (relationshipPanel.activeSelf)
            RefreshRelationshipRows();

        CheckSkillLevelUps();
        // debug temp
        Debug.Log($"Queue count: {taskQueue.Count}");
        if (simulation.IsDayOver)
        {
            actionText.color = Color.white;
            actionText.text = "Day over, go home";
            SetActionButtonsInteractable(false);
            goHomeButton.gameObject.SetActive(true);
        }
        else if (simulation.IsTravelling)
        {
            actionText.color = Color.white;
            actionText.text = "Walking...";
            SetActionButtonsInteractable(false);
        }
        else if (simulation.IsBusy)
        {
            int pct = Mathf.RoundToInt(simulation.CurrentEfficiency * 100f);
            actionText.text = $"{simulation.CurrentActivityName} ({simulation.RemainingMinutes} min left) — {pct}%";

            if (pct >= 90) actionText.color = Color.green;
            else if (pct >= 60) actionText.color = new Color(1f, 0.6f, 0f);
            else actionText.color = Color.red;
        }
        else
        {
            actionText.color = Color.white;
            actionText.text = "";
            socialiseButton.interactable = true;
            foreach (ActivitySlot slot in slots)
                slot.button.interactable = simulation.CanAfford(slot.activity);
        }

        if (failureTimer > 0f)
        {
            actionText.color = Color.white;
            actionText.text = failureMessage;
        }
    }

    void UpdateAudio()
    {
        CharacterPose pose = simulation.CurrentPose;
        if (pose == lastPose) return;
        lastPose = pose;

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

    void UpdateSkillBars()
    {
        List<SkillType> building = simulation.SkillsBeingBuilt();

        for (int i = 0; i < skillBars.Length; i++)
        {
            if (i < building.Count)
            {
                SkillType skill = building[i];
                skillBars[i].SetActive(true);
                skillBarFills[i].fillAmount = simulation.SkillProgress(skill);
                skillBarLabels[i].text = $"{skill} → Level {simulation.SkillTargetLevel(skill)}";
            }
            else
            {
                skillBars[i].SetActive(false);
            }
        }
    }

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

        failureTimer = 2f;
    }

    void UpdateBar(Image fill, int value)
    {
        fill.fillAmount = (float)value / 100f;

        if (value < 15)
            fill.color = Color.red;
        else if (value < 30)
            fill.color = new Color(1f, 0.6f, 0f);
        else
            fill.color = Color.green;
    }

    void SetActionButtonsInteractable(bool on)
    {
        socialiseButton.interactable = on;
        foreach (ActivitySlot slot in slots)
            slot.button.interactable = on;
    }

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
    // Coworker panel
    // =====================================================================

    void BuildCoworkerButtons()
    {
        foreach (CoworkerDefinition coworker in coworkers)
        {
            GameObject buttonObj = Instantiate(coworkerButtonPrefab, coworkerPanel.transform);
            Button btn = buttonObj.GetComponent<Button>();
            coworkerButtons[coworker] = btn;

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

    void OnSocialiseClicked()
    {
        RefreshCoworkerButtons();
        coworkerPanel.SetActive(true);
        modalOpen = true;
    }

    void OnCoworkerClicked(CoworkerDefinition coworker)
    {
        ReportResult(simulation.DoActivity(coworker.talkActivity));
        coworkerPanel.SetActive(false);
        modalOpen = false;
        UpdateDisplay();
    }

    void OnCancelClicked()
    {
        coworkerPanel.SetActive(false);
        modalOpen = false;
    }

    void OnSkillChosen(ActivityDefinition activity)
    {
        skillPanel.SetActive(false);
        modalOpen = false;

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

    void CheckSkillLevelUps()
    {
        foreach (SkillType type in System.Enum.GetValues(typeof(SkillType)))
        {
            int current = gameState.GetSkillLevel(type);
            if (current > lastSkillLevels[type])
            {
                lastSkillLevels[type] = current;
                OnSkillLevelUp(type, current);
            }
        }
    }

    void OnSkillLevelUp(SkillType type, int newLevel)
    {
        levelUpMessage = $"{type} reached Level {newLevel}!";
        levelUpTimer = 2.5f;
        oneShotSource.PlayOneShot(levelUpChime);
    }


    // =====================================================================
    // Relationship dropdown
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
        if (gameState.dayNumber >= finalDay)
        {
            EndGame();
            return;
        }

        gameState.dayNumber++;
        gameState.RecoverOvernight();

        StartNewDay();
        playerCharacter.position = dayStartPoint.position;
        playerCharacter.rotation = dayStartPoint.rotation;
        walkTarget = null;

        goHomeButton.gameObject.SetActive(false);
        UpdateDisplay();
    }

    void EndGame()
    {
        bool won = HasWon();
        endScreenPanel.SetActive(true);
        endScreenText.text = won
            ? "The boss retired, and you got the job. You're the boss now."
            : "The boss retired. The job went to the rival.";
        modalOpen = true;
    }

    void OnPlayAgain()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    bool HasWon()
    {
        return simulation.Career >= winCareerThreshold
               && simulation.AverageRelationship >= winAvgRelationshipThreshold
               && gameState.GetRelationship(bossDefinition) >= winBossRelationshipThreshold;
    }


    // =====================================================================
    // Speed controls (the only place isPaused is touched)
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
    
    private struct QueuedTask
    {
        public ActivityDefinition activity;
        public Transform standPoint;
    }

    private List<QueuedTask> taskQueue = new List<QueuedTask>();
    private const int maxQueued = 2;   // waiting tasks; +1 running = 3 total
}