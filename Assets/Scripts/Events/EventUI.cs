using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Displays an event as a modal and applies the chosen effects.
// Knows nothing about the day loop — it just shows, resolves, and reports back.
public class EventUI : MonoBehaviour
{
    [SerializeField] private GameObject eventBackdrop;
    [SerializeField] private TextMeshProUGUI eventText;
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private GameObject choiceButtonPrefab;

    private GameState game;

    // Whoever owns the day loop sets this; we call it once the modal closes.
    public System.Action onEventClosed;

    public void Init(GameState game)
    {
        this.game = game;
        eventBackdrop.SetActive(false);
    }

    public void ShowEvent(EventDefinition ev)
    {
        eventBackdrop.transform.SetAsLastSibling();   // draw on top of everything
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
            game.ApplyEffect(effect);

        eventBackdrop.SetActive(false);
        onEventClosed?.Invoke();
    }
}