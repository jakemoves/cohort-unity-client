using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cohort;
using UnityEngine.UI;
using System.Linq;
using System;

public class CohortUIManager : MonoBehaviour
{
    // Model
    [field: Header("Model")]
    [field: SerializeField] public ShowGraphSession ShowGraphSession { get; private set; }
    [field: SerializeField] public string Group { get; private set; }
    [field: SerializeField] public CHSession CohortSession { get; private set; }

    public ShowGraphSession.GraphCursor GraphCursor { get => ShowGraphSession?.Cursor; }

    // View
    [field: Header("View")]
    [field: SerializeField] public GameObject CohortUI { get; private set; }
    [field: SerializeField] public TMPro.TextMeshProUGUI TextCueDisplay { get; set; }
    [field: SerializeField] public TMPro.TextMeshProUGUI StatusDisplay { get; set; }
    [field: SerializeField] public TMPro.TextMeshPro ToggleUiText { get; set; }

    // Show Controls
    [field: Header("Show Controls")]
    [field: SerializeField] public Dropdown GroupSelector { get; set; }
    [field: SerializeField] public Button StartShowButton { get; set; }

    [field: Header("Show Controls - Scene")]
    [field: SerializeField] public GameObject CueContainer { get; set; }
    [field: SerializeField] public TMPro.TextMeshProUGUI CurrentAssetText { get; set; }
    [field: SerializeField] public Button PreviousCue { get; set; }
    [field: SerializeField] public Button NextCue { get; set; }
    [field: SerializeField] public Button Play { get; set; }
    [field: SerializeField] public Button Stop { get; set; }

    [field: Header("Show Controls - Choice")]
    [field: SerializeField] public GameObject ChoiceContainer { get; set; }
    [field: SerializeField] public TMPro.TextMeshProUGUI ChoiceText { get; set; }
    [field: SerializeField] public Button YesButton { get; set; }
    [field: SerializeField] public Button NoButton { get; set; }

#warning CancelButton is currently not used
    [field: SerializeField] public Button CancelButton { get; set; }


    public TMPro.TextMeshPro toggleUiText;


    // Start is called before the first frame update
    void Start()
    {
        // NOTE: I switched to a better programming paradigm because this was too time consuming.

        // Initialize Status Display
        StatusDisplay ??= GameObject.Find("Status Display")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (StatusDisplay == null) Debug.Log("Status Display is not set and could not be found");

        // Initialize Models
        ShowGraphSession ??= GameObject.Find("CohortManager")?.GetComponent<ShowGraphSession>();
        if (ShowGraphSession == null)
        {
            SetStatusMessage("SHOW ERROR: Show Graph Session is not set and could not be found.", StatusMessageType.Error);
        }

        CohortSession ??= GameObject.Find("CohortManager")?.GetComponent<CHSession>();
        if (CohortSession == null)
        {
            SetStatusMessage("SHOW ERROR: CoHort Session is not set and could not be found", StatusMessageType.Error);
        }
        else
        {
            CohortSession.onTextCue += OnTextCueHandler;
            CohortSession.onStatusChanged += OnStatusUpdateHandler;
        }

        Group = Group.Trim();
        Group = string.IsNullOrWhiteSpace(name) || name == "All" ? null : Group;

        // Initialize View
        // NOTE: If this is not set or could not be found setting it to this gameObject may lead to unexpected behaviour
        CohortUI ??= GameObject.Find("CohortUI") ?? this.gameObject;

        TextCueDisplay ??= GameObject.Find("Text Cue Display")?.GetComponent<TMPro.TextMeshProUGUI>();
        if (TextCueDisplay == null)
            SetStatusMessage("SHOW ERROR: Text Cue Display is not set and could not be found", StatusMessageType.Error);
        else
            TextCueDisplay.text = "";

        // Initialize Show Controls
        GroupSelector = GroupSelector ?? GameObject.Find("GroupingDropdown")?.GetComponent<Dropdown>();
        if (GroupSelector == null)
            SetStatusMessage("SHOW ERROR: Group Selector Dropdown is not set and could not be found", StatusMessageType.Error);
        else
        {
            // Set Group Selection
            GroupSelector.options.Clear();
            GroupSelector.options.Add(new Dropdown.OptionData("All"));
            GroupSelector.options.AddRange(from g in ShowGraphSession.MasterGroupsArray
                                           select new Dropdown.OptionData(g));

            // Subscribe to events
            GroupSelector.onValueChanged.AddListener((index) => { ShowGraphSession.SetGroup(GroupSelector.options[index].text); });
        }

        // Initialize Show Controls - Cue
        InitObject<GameObject>(CueContainer, "Cue Panel", (o) => { ChoiceContainer.SetActive(false); });
        InitObject<TMPro.TextMeshProUGUI>(CurrentAssetText, "Current Asset Label", null);
        InitObject<Button>(NextCue, "Next Asset");
        InitObject<Button>(PreviousCue, "Prev Asset");
        InitObject<Button>(Play, "Play");
        InitObject<Button>(Stop, "Stop");

        // Initialize Show Controls - Choice
        InitObject<GameObject>(ChoiceContainer, "Choice Panel", (o) => { ChoiceContainer.SetActive(false); });
        InitObject<TMPro.TextMeshProUGUI>(ChoiceText, "Current Question Label");
        InitObject<Button>(YesButton, "Yes");
        InitObject<Button>(NoButton, "No");
    }

#nullable enable
    void InitObject<T>(T component, string name, Action<T>? initAction = null) where T : class?
    {
        if (typeof(T) == typeof(GameObject))
            component ??= GameObject.Find(name) as T;
        else
            component ??= GameObject.Find(name)?.GetComponent<T>();

        if (component == null)
            SetStatusMessage($"SHOW ERROR: {name} is not set and could not be found", StatusMessageType.Error);
        else initAction?.Invoke(component);
    }
#nullable restore

    // Update is called once per frame
    void Update()
    {

    }

    void OnTextCueHandler(CueAction cueAction, string cueText)
    {
        if (cueAction == CueAction.play)
        {
            TextCueDisplay.text = cueText;
        }
        else if (cueAction == CueAction.stop)
        {
            TextCueDisplay.text = "";
        }
    }

    void OnStatusUpdateHandler(string message) => SetStatusMessage(message);

    public void SetStatusMessage(string message, StatusMessageType type = StatusMessageType.Info)
    {
        Color color;
        switch (type)
        {
            case StatusMessageType.Info:
                Debug.Log(message);
                color = Color.white;
                break;

            case StatusMessageType.Warning:
                Debug.LogWarning(message);
                color = Color.yellow;
                break;

            case StatusMessageType.Error:
                Debug.LogError(message);
                color = Color.white;
                break;

            default:
                color = Color.white;
                break;
        }

        if (StatusDisplay == null) return;

        StatusDisplay.color = color;
        StatusDisplay.text = message;
    }

    public void toggleCaptions()
    {
        TextCueDisplay.enabled = !TextCueDisplay.enabled;

        // TODO: textCueBackground
        //if (textCueBackground != null)
        //textCueBackground.enabled = textCueDisplay.enabled;
    }

    public void onShowUI()
    {
        CohortUI.SetActive(true);
    }

    public void onHideUI()
    {
        CohortUI = GameObject.Find("CohortUI");
        CohortUI.SetActive(false);
    }

    public void toggleUI()
    {
        CohortUI ??= GameObject.Find("CohortUI");

        string displayText;

        if (CohortUI.activeSelf)
        {
            onHideUI();
            displayText = "Show UI";
        }
        else
        {
            onShowUI();
            displayText = "Hide UI";
        }

        if (toggleUiText != null)
            toggleUiText.text = displayText;
    }

    public enum StatusMessageType
    {
        Info,
        Warning,
        Error
    }
}
