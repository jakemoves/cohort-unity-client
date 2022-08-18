using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ShowGraphSystem.Runtime;
using Cohort;
using System.Linq;
using System;

public class ShowGraphSession : MonoBehaviour
{
    [field: Header("Data")]
    [field: SerializeField] public ShowGraphSystem.Serialization.ShowGraphDataSO ShowGraphData { get; private set; }

    [field: Header("External Components")]
    [field: SerializeField] public CHSession CHSession { get; private set; }

    // Runtime Properties
    public event EventHandler<DecisionThroughText.Decisions> DecisionsUpdate;

    public string Group => Cursor?.Group;
    public ShowGraph Graph { get; private set; }
    public GraphCursor Cursor { get; set; }
    public string[] MasterGroupsArray => Graph?.MasterGroupArray;

    void Awake()
    {
        if (ShowGraphData == null)
        {
            Debug.LogError("There is no Graph Data attatched to this component");
            return;
        }
        // TODO: If CHSession is null we need to find it

        // Load and Generate ShowGraph
        Graph = ShowGraph.GenerateGraphFromData(ShowGraphData);

        // Init DecisionThroughText
        if (!(Graph is null))
        {
            foreach (var node in Graph.NodeDictionary)
            {
                if (node.Value is ChoiceNode choiceNode)
                {
                    DecisionThroughText.Instance.AddChoice(choiceNode.ID, choiceNode.GroupKeyArray);
                    DecisionThroughText.Instance[choiceNode.ID].DecisionsChanged += OnDecisionsUpdate;
                }
            }
        }

        Cursor = new GraphCursor(null, Graph, GraphCursor.DefaultDecider);
    }

    private void OnDecisionsUpdate(object sender, DecisionThroughText.Decisions e)
    {
        DecisionsUpdate?.Invoke(this, e);
    }

    private void Start()
    {
        if (CHSession != null)
        {
            CHSession.Groups = (Graph?.MasterGroupArray ?? new string[0])
                .Concat(new string[] { "all" })
                .ToArray();
        }
        else
            Debug.LogError("CHSession was not set, App will exibit undefined behavior.");
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetGroup(string groupName)
    {
        groupName = groupName.Trim();
        if (!string.IsNullOrEmpty(groupName) && groupName != "All" && MasterGroupsArray.Contains(groupName))
        {
            if (Cursor.Status == GraphCursor.GraphCursorStatus.AtRoot)
            {
                Cursor.Group = groupName;
                if (CHSession != null)
                    CHSession.grouping = groupName;
                Debug.Log($"Group set to {Group}");
            }
            else if (Cursor.Status == GraphCursor.GraphCursorStatus.Unknown)
            {
                Cursor.Reset();
                Cursor.Group = groupName;
                if (CHSession != null)
                    CHSession.grouping = groupName;
                Debug.Log($"Group set to {Group}");
            }
            else
                throw new NotImplementedException("Changing groups in the middle or end of the show has not been implemented");
        }
        else
        {
            Cursor.Reset();
            Cursor.Group = null;

            if (!string.IsNullOrEmpty(groupName))
                Debug.LogError($"Group name {groupName} is invalid - group setting will be considered none/All");
            Debug.LogWarning("Setting group to all - cursor enmerator reset");
        }
    }

    public class GraphCursor : IEnumerator<ShowNode>
    {
        public delegate uint MakeChoice(ChoiceNode choice, out System.Threading.CancellationToken? cancellationToken);

        public string Group { get; set; }
        public ShowGraph ShowGraph { get; set; }
        public ShowNode Current { get; private set; }
        public ShowGraphSystem.CueReference[] CurrentCueList
        {
            get
            {
                if (Group == null || Current == null)
                    return null;

                if (Current is ChoiceNode)
                    return null;
                if (Current is SceneNode scene)
                    return scene.CueListByGroups[Group];

                return null;
            }
        }

        /// <summary>
        /// Gets the List of ShowNodes that make up the path.
        /// The last element is the current position.
        /// </summary>
        public List<ShowNode> Path
        {
            get
            {
                var value = previousStack.ToList();
                value.Add(Current);

                return value;
            }
        }

        public MakeChoice MakeChoiceCallback { get; set; } = DefaultDecider;

        object IEnumerator.Current => Current;

        public GraphCursorStatus Status
        {
            get
            {
                if (Group == null) return GraphCursorStatus.Unknown;
                if (Current == null)
                {
                    if (previousStack.Any()) return GraphCursorStatus.AtEnd;
                    return GraphCursorStatus.AtRoot;
                }
                else
                    return GraphCursorStatus.InMiddle;

                return GraphCursorStatus.Unknown;
            }
        }

        private Stack<ShowNode> previousStack = new Stack<ShowNode>(50);

        public GraphCursor(string groupName, ShowGraph showGraph, MakeChoice choiceCallback)
        {
            if (groupName == null || showGraph.MasterGroupArray.Contains(groupName))
                Group = groupName;
            else
            {
                Group = null;
                Debug.LogError($"the group {groupName} does not exist - the group will be null");
            }

            ShowGraph = showGraph;
            MakeChoiceCallback = choiceCallback;
        }

        public static GraphCursor FromPath(string groupName, ShowGraph showGraph, MakeChoice choiceCallback, List<ShowNode> path) =>
            new GraphCursor(groupName, showGraph, choiceCallback)
            {
                previousStack = new Stack<ShowNode>(path.GetRange(0, path.Count - 1)),
                Current = path[path.Count - 1]
            };

        public static uint DefaultDecider(ChoiceNode choice, out System.Threading.CancellationToken? cancellationToken)
        {
            cancellationToken = default;
            return (uint)UnityEngine.Random.Range(0, choice.NextShowNodes.Length);
        }

        public bool MoveNext() => MoveToNextNode() != null;

        private ShowNode MoveToNextNode()
        {
            if (Group == null) return null;

            // We are at the root if the stack does not have any node
            // otherwise we are at the end
            if (Current == null)
            {
                Current = !previousStack.Any() ? PeekNextNode(ShowGraph.EntryPoint) : null;
                return Current;
            }

            // Get Next
            if (Current is SceneNode scene)
            {
                previousStack.Push(Current);
                Current = PeekNextNode(scene.NextShowNodes);
            }
            else if (Current is ChoiceNode choice)
            {
                if (MakeChoiceCallback == null)
                    throw new InvalidOperationException($"{nameof(MakeChoiceCallback)} can not be null");

                System.Threading.CancellationToken? cancellationToken;
                var decisions = MakeChoiceCallback.Invoke(choice, out cancellationToken);

                if ((cancellationToken?.IsCancellationRequested).GetValueOrDefault(false))
                    return Current;

                previousStack.Push(Current);

                // This is actually kind of smart
                // By ANDing by L - 1 we constrain the amount of bits used, using only the relevent bits
                Current = PeekNextNode(choice.NextShowNodes[decisions & (choice.NextShowNodes.Length - 1)]);
            }

            return Current;
        }

        public ShowNode PeekNextNode(List<ShowNode> nextShowNodes)
        {
            var query = from node in nextShowNodes
                        where node.SelectedGroups.Contains(Group)
                        select node;

            // Note: Getting the count on IEnumerable is performance wise costly.
            if (query.Count() > 1)
                Debug.LogWarning("The path was ambiguous - Chosing first node.");

            return query.DefaultIfEmpty(null).First();
        }

        public bool MovePrevious() => MoveToPreviousNode() != null;

        private ShowNode MoveToPreviousNode()
        {
            if (Group == null) return null;

#warning If the previous node is a ChoiceNode - the entire show can become desynced
            if (previousStack.Any())
                Current = previousStack.Pop();
            else
                Current = null;

            return Current;
        }

        public void Reset()
        {
            previousStack.Clear();
            Current = null;
        }

        public void Dispose()
        {
            previousStack = null;
            Current = null;
        }
        // TODO: IsAmbigous

        public enum GraphCursorStatus
        {
            Unknown,
            AtRoot,
            AtEnd,
            InMiddle
        }
    }
}
