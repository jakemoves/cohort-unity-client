using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cohort;
using UnityEngine.UI;
using System.Linq;
using System;
using UnityEngine.Events;
using ShowGraphSystem.Runtime;
using ShowGraphSystem;
using static DecisionThroughText;

#warning If multiple CohortUIManagers are active in the scene - they may not work as expected
#warning NullReference Exception thrown if GetComponent in children returns null
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
    [field: SerializeField] public Button ToggleUiButton { get; set; }

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

    [SerializeField]
    private ConnectionIndicator connectionIndicator;

    // Actions
    private UnityAction showStartAction;
    private ShowGraphSystem.CueReference currentCueReference = null;
    private CueReferenceEnumerator cueCursor = null;

    // Start is called before the first frame update
    void Start()
    {
        // Init Actions
        showStartAction += StartShow;

        InitializeUi();

        GroupSelector.value = 0;
    }

    private void InitializeUi()
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
        else
        {
            ShowGraphSession.DecisionsUpdate += DecisionsUpdate;
            ShowGraphSession.Cursor.MakeChoiceCallback = MakeChoice;
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
        GroupSelector ??= GameObject.Find("GroupingDropdown")?.GetComponent<Dropdown>();
        if (GroupSelector == null)
            SetStatusMessage("SHOW ERROR: Group Selector Dropdown is not set and could not be found", StatusMessageType.Error);
        else
        {
            // Set Group Selection
            GroupSelector.options.Clear();
            //GroupSelector.options.Add(new Dropdown.OptionData("All"));
            GroupSelector.options.AddRange(from g in ShowGraphSession.MasterGroupsArray
                                           select new Dropdown.OptionData(g));

            // Subscribe to events
            GroupSelector.onValueChanged.AddListener((index) => { ShowGraphSession.SetGroup(GroupSelector.options[index].text); });
        }

        InitObject<Button>(StartShowButton, "Start Show", (b) =>
        {
            b.gameObject.SetActive(true);
            b.onClick.AddListener(showStartAction);
        });

        // Initialize Show Controls - Cue
        InitObject<GameObject>(CueContainer, "Cue Panel", (o) => { ChoiceContainer.SetActive(false); });
        InitObject<TMPro.TextMeshProUGUI>(CurrentAssetText, "Current Asset Label", null);
        InitObject<Button>(NextCue, "Next Asset", (button) => { button.onClick.AddListener(() => NextAction()); });
        InitObject<Button>(PreviousCue, "Prev Asset", (button) => { button.onClick.AddListener(() => PreviousAction()); });
        InitObject<Button>(Play, "Play", (button) => { button.onClick.AddListener(() => PlayCue()); });
        InitObject<Button>(Stop, "Stop", (button) => { button.onClick.AddListener(() => StopCue()); });

        // Initialize Show Controls - Choice
        InitObject<GameObject>(ChoiceContainer, "Choice Panel", (o) => { ChoiceContainer.SetActive(false); });
        InitObject<TMPro.TextMeshProUGUI>(ChoiceText, "Current Question Label");
        InitObject<Button>(YesButton, "Yes", (button) => { button.onClick.AddListener(() => MakeDecisionAction(true)); });
        InitObject<Button>(NoButton, "No", (button) => { button.onClick.AddListener(() => MakeDecisionAction(false)); });
    }

#nullable enable
    void InitObject<T>(T component, string name, Action<T>? initAction = null) where T : class?
    {
        if (typeof(T) == typeof(GameObject))
            component ??= GameObject.Find(name) as T; // NOTE: this fallback may not work
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

    public void StartShow()
    {
        StartShowButton?.gameObject.SetActive(false);
        //GroupSelector?.gameObject.SetActive(false);
        if (GroupSelector != null)
            GroupSelector.interactable = false;

        if (!(ToggleUiButton is null))
            ToggleUiButton.interactable = false;

        // This might not be necessary, but just incase
        ShowGraphSession?.SetGroup(GroupSelector?.options[GroupSelector.value].text);

        if (GraphCursor.Status != ShowGraphSession.GraphCursor.GraphCursorStatus.AtRoot)
            GraphCursor.Reset();

        // Display Status Message
        if (GraphCursor.MoveNext())
        {
            EnterShowNode(GraphCursor.Current);
            SetStatusMessage("Show Started - At First Show Node");
        }
        else
            SetStatusMessage("ERROR: Failed to move to the top of show", StatusMessageType.Error);
    }

    public void EnterShowNode(ShowNode showNode)
    {
        // TODO: finish the behaviour for this case
        if (showNode is null)
            return;

        StartShowButton?.gameObject.SetActive(false);

        if (showNode is SceneNode sceneNode)
        {
            ChoiceContainer.SetActive(false);
            CueContainer.SetActive(true);

            cueCursor = new CueReferenceEnumerator(sceneNode.CueListByGroups[GraphCursor.Group]);

            // This is set Just in case the User presses the Play Button
            // without hitting next first
            if (sceneNode.CueListByGroups[GraphCursor.Group].Length > 0)
                currentCueReference = sceneNode.CueListByGroups[GraphCursor.Group][0];
            else
                currentCueReference = null;

            CurrentAssetText.text = "TOP OF SCENE - Press Next Cue to Select the first cue";

            PreviousCue.interactable = true;
            PreviousCue.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Previous Scene";

            NextCue.interactable = true;
            NextCue.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Next Cue";
            SetStatusMessage(sceneNode.ToString());
        }
        else if (showNode is ChoiceNode choiceNode)
        {
            ChoiceContainer.SetActive(true);
            CueContainer.SetActive(true);

            // The if is intended to make this supper robust
            if (!(choiceNode.GroupTransitionCues is null) && choiceNode.GroupTransitionCues.ContainsKey(GraphCursor.Group))
            {
                CurrentAssetText.text = "< No Transition >";
                SetCue(choiceNode.GroupTransitionCues[GraphCursor.Group]);
            }
            else
            {
                currentCueReference = null;
                CurrentAssetText.text = "< No Transition >";
            }

            Decisions decisions = DecisionThroughText.Instance[choiceNode];

            // This is to ensure ALL DEVICES stay in sync
            var shouldEnableButtons = decisions[GraphCursor.Group] is null;
            YesButton.gameObject.SetActive(shouldEnableButtons);
            NoButton.gameObject.SetActive(shouldEnableButtons);

            cueCursor = null;

            PreviousCue.interactable = true;
            PreviousCue.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Previous Scene";

            NextCue.interactable = true;
            NextCue.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Next Scene";

            // TODO: Flesh out behaviour
            ChoiceText.text = choiceNode.GroupChoices[GraphCursor.Group];

            var groupsDecided = decisions.Keys.Where(k => !(decisions[k] is null));
            if (groupsDecided.Any())
                SetStatusMessage($"CHOICE NODE\nGroups Already Decided: {string.Join(", ", groupsDecided)}");
            else
                SetStatusMessage("CHOICE NODE\nNo decisions made...");

            // TODO: Should we disable the Next button?
        }
    }

    public void SetCue(ShowGraphSystem.CueReference cueReference)
    {
        currentCueReference = cueReference;

        if (currentCueReference is null)
            return;

        // Sets the selected Asset text to the CueDescription
        if (CurrentAssetText != null)
        {
            string assetDescritption = cueReference.MediaDomain switch
            {
                ShowGraphSystem.MediaDomain.Sound => CohortSession.soundCues.Find(cue => cue.cueNumber == cueReference.CueID)?.accessibleAlternative,
                ShowGraphSystem.MediaDomain.Image => CohortSession.imageCues.Find(cue => cue.cueNumber == cueReference.CueID)?.accessibleAlternative,
                ShowGraphSystem.MediaDomain.Video => CohortSession.videoCues.Find(cue => cue.cueNumber == cueReference.CueID)?.accessibleAlternative,
                ShowGraphSystem.MediaDomain.Text => CohortSession.textCues.Find(cue => cue.cueNumber == cueReference.CueID)?.text,
                _ => cueReference.ToString(),
            };

            CurrentAssetText.text = assetDescritption ?? $"<< Invalid Cue: {cueReference.MediaDomain} Cue {cueReference.CueID} Was Not Found >>";
        }
    }

    private void NextAction()
    {
        if (GraphCursor.Current is ChoiceNode choice)
        {
            TryMoveOnFromDecision(DecisionThroughText.Instance[choice]);
            return;
        }

        if (cueCursor == null)
            throw new InvalidOperationException("Cue Cursor must not be null");

        if (cueCursor.AtBeginning)
            PreviousCue.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Previous Cue";

        if (cueCursor.AtEnd)
        {
            MoveToNextNode();
        }
        else if (cueCursor.MoveNext())
        {
            SetCue(cueCursor.Current);
        }
        else
        {
            SetCueControlUiEndState();
        }
    }

    private void SetCueControlUiEndState()
    {
        CurrentAssetText.text = "END OF SCENE - Press Next Scene/Choice to proceed";
        // NOTE: It may be usful to add a PeekNext to the graph cursor
        // inorder for this to provide information to the user
        NextCue.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Next Scene/Choice";
    }

    private void MoveToNextNode()
    {
        // Go To Next Scene
        if (GraphCursor.MoveNext())
        {
            EnterShowNode(GraphCursor.Current);
        }
        else
        {
            // End of Show
            SetStatusMessage("END OF SHOW", StatusMessageType.Warning);
            SetEndOfShowUIState();
        }
    }

    private void PreviousAction()
    {
        // NOTE: Change this if you want different behaviour for Choice Node
        if (GraphCursor.Current is ChoiceNode)
        {
            TryMoveToPreviousNode();
            return;
        }

        if (cueCursor == null)
            throw new InvalidOperationException("Cue Cursor must not be null");

        if (cueCursor.AtEnd)
            NextCue.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Next Cue";

        if (cueCursor.AtBeginning)
        {
            TryMoveToPreviousNode();
        }
        else if (cueCursor.MovePrevious())
        {
            SetCue(cueCursor.Current);
        }
        else
        {
            CurrentAssetText.text = "TOP OF SCENE - Press Previous Scene/Choice to go back the last scene";
            // NOTE: It may be usful to add a PeekLast to the graph cursor
            // inorder for this to provide information to the user
            PreviousCue.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Previous Scene/Choice";
        }
    }

    private void TryMoveToPreviousNode()
    {

        // Go To Previous Scene
        if (GraphCursor.MovePrevious())
        {
            EnterShowNode(GraphCursor.Current);

            if (GraphCursor.Current is SceneNode sceneNode)
                {
                // Cue Cursor GoToEnd
                cueCursor.GoToEnd();

                // Set Cue Cursor To Last Cue
                SetCueControlUiEndState();

                var cueCount = sceneNode.CueListByGroups[GraphCursor.Group].Length;
                currentCueReference = cueCount > 0 ? sceneNode.CueListByGroups[GraphCursor.Group][cueCount - 1] : null;
            }
        }
        else
        {
            // Top of Show
            SetStatusMessage("TOP OF SHOW", StatusMessageType.Warning);
            SetTopOfShowUIState();
        }
    }

    private void SetTopOfShowUIState()
    {
        StartShowButton?.gameObject.SetActive(true);

        if (GroupSelector != null)
            GroupSelector.interactable = true;

        if (!(ToggleUiButton is null))
            ToggleUiButton.interactable = true;

        CueContainer?.SetActive(false);
        ChoiceContainer?.SetActive(false);

        if (GraphCursor.Status != ShowGraphSession.GraphCursor.GraphCursorStatus.AtRoot)
            GraphCursor.Reset();
    }

    private void SetEndOfShowUIState()
    {
#warning Method not implemented
        Debug.LogWarning(new NotImplementedException());
    }

    private void PlayCue()
    {
        if (currentCueReference is null)
            return;

        CohortSession.FireCue(currentCueReference.ToCohortCue(CueAction.play));

        if (currentCueReference.VibrateOnCue)
            CohortSession.FireCue(currentCueReference.GetVibrationCue());
    }

    private void StopCue()
    {
        if (currentCueReference is null)
            return;

        CohortSession.FireCue(currentCueReference.ToCohortCue(CueAction.stop));
    }

    private void MakeDecisionAction(bool yes)
    {
        // Double Check if what the node is
        if (GraphCursor.Current is SceneNode)
        {
            // Take Corrective Action
            EnterShowNode(GraphCursor.Current);
            ChoiceContainer.SetActive(false);
            CueContainer.SetActive(true);
            return;
        }
        else if (!(GraphCursor.Current is ChoiceNode))
            throw new InvalidOperationException();

        var choice = (ChoiceNode)GraphCursor.Current;

        if (!DecisionThroughText.Instance.ContainsKey(choice))
            //DecisionThroughText.Instance.AddChoice(choice);
            Debug.LogError($"The node {choice} does not exsist in the decisions dictionary");
        
        // Validate Decision Context
        if (!DecisionThroughText.Instance[choice].ContainsKey(GraphCursor.Group))
            throw new InvalidOperationException($"The current choice node does not support the group {GraphCursor.Group}");

        // Make the Decision
        // Instansiate the command BEFORE we set our decision as it will cause the event to be raised
        var decisionCommand = new DecisionCommand(GraphCursor.Current.ID, GraphCursor.Group) { Decision = yes };
        //DecisionThroughText.Instance[choice][GraphCursor.Group] = yes;

        Debug.Log($"Sending Command: {decisionCommand}");

        CohortSession.TransmitCommand(choice.GroupKeyArray, decisionCommand);

        SetStatusMessage("... Waiting for other choices");
        //TryMoveOnFromDecision(DecisionThroughText.Instance[choice]);
    }

    private void DecisionsUpdate(object sender, DecisionThroughText.Decisions e)
    {
        // Validate Eventarguments
        if (ShowGraphSession.Graph.NodeDictionary.ContainsKey(e.NodeID) && ShowGraphSession.Graph.NodeDictionary[e.NodeID] is ChoiceNode)
        {
            //TryMoveOnFromDecision(e);
            DicisionChangeUiUpdate(e);
        }
        else
            Debug.LogError($"DecisionsUpdate event args invalid [{e.NodeID}]");
    }

    private void TryMoveOnFromDecision(DecisionThroughText.Decisions decisions)
    {
        if (GraphCursor.Current is ChoiceNode choiceNode && GraphCursor.Current.ID == decisions.NodeID)
        {
            if (decisions.TryGetDecisionsValue(out int value))
            {
                Debug.Log($"Choice Made {choiceNode.ID}.{GraphCursor.Group} ->> into {value}");
                MoveToNextNode();
            }
            else
            {
                Debug.LogWarning($"Still waiting for decisions... Please wait");

                var getDecidedGroups = decisions.Keys.Where(k => !(decisions[k] is null));
                var getDecidedGroupsText = (getDecidedGroups.Any() ? string.Join(", ", getDecidedGroups) : "None");

                SetStatusMessage($"... Still waiting for other choices\n({getDecidedGroupsText} have decided)",
                    StatusMessageType.Warning);
            }
        }
    }

    private void DicisionChangeUiUpdate(DecisionThroughText.Decisions decisions)
    {
        if (GraphCursor.Current is ChoiceNode choiceNode && GraphCursor.Current.ID == decisions.NodeID)
        {
            // Prevent User from making the choice again
            var shouldEnableButtons = decisions[GraphCursor.Group] is null;
            YesButton.gameObject.SetActive(shouldEnableButtons);
            NoButton.gameObject.SetActive(shouldEnableButtons);

            if (decisions.TryGetDecisionsValue(out int value))
            {
                NextCue.interactable = true;

                var nextNode = GraphCursor.PeekNextNode(choiceNode.NextShowNodes[value & (choiceNode.NextShowNodes.Length - 1)]);
                SetStatusMessage($"Ready for next scene\nNEXT >> {nextNode.Title}");
            }
            else
            {
                SetStatusMessage($"... waiting for remaining choices\nGroups Decided: {string.Join(", ", decisions.Keys.Where(k => !(decisions[k] is null)))}", StatusMessageType.Info);
            }
        }
    }

    public uint MakeChoice(ChoiceNode choiceNode, out System.Threading.CancellationToken? cancellationToken)
    {
        // I dont think we need this as we are decision making syncronisly
        cancellationToken = null;

        // State Checks
        if (choiceNode == null || !DecisionThroughText.Instance.ContainsKey(choiceNode?.ID))
            throw new NullReferenceException($"Crucial decision information is missing" +
                $"\nChoiceNode -> {choiceNode?.ID}" +
                $"\nDecisions is Null -> {DecisionThroughText.Instance.ContainsKey(choiceNode?.ID)}");

        int value;
        if (!DecisionThroughText.Instance[choiceNode].TryGetDecisionsValue(out value))
            throw new InvalidOperationException($"There is one or more decisions not yet made\n" +
                $"{DecisionThroughText.Instance[choiceNode]}");

        return (uint)value;
    }

    private void CancelChoice()
    {
        throw new NotImplementedException();
#warning TODO
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

        if (!(connectionIndicator is null))
            connectionIndicator.AutoHide = false;
    }

    public void onHideUI()
    {
        CohortUI = GameObject.Find("CohortUI");
        CohortUI.SetActive(false);

        if (!(connectionIndicator is null))
            connectionIndicator.AutoHide = true;
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

        // TODO:
        //if (toggleUiText != null)
        //    toggleUiText.text = displayText;
    }

    public enum StatusMessageType
    {
        Info,
        Warning,
        Error
    }
}
