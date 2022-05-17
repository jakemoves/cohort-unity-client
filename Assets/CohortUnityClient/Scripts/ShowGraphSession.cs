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

    public string Group { get; set; } = null;
    public ShowGraph Graph { get; private set; }
    public GraphCursor Cursor { get; set; }
    public string[] MasterGroupsArray => Graph?.MasterGroupArray;

    // Start is called before the first frame update
    void Awake()
    {
        if (ShowGraphData == null)
        {
            Debug.LogError("There is no Graph Data attatched to this component");
            return;
        }

        // Load and Generate ShowGraph
        Graph = ShowGraph.GenerateGraphFromData(ShowGraphData);

        if (!MasterGroupsArray.Contains(Group))
            Group = null;

        // TODO!: Set Callback
#warning MakeChoiceCallback is not set.
        Cursor = new GraphCursor(Group, Graph, null);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetGroup(string groupName)
    {
        if (!string.IsNullOrEmpty(groupName) && MasterGroupsArray.Contains(groupName))
        {
            // TODO: 
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

        private static uint DefaultDecider(ChoiceNode choice, out System.Threading.CancellationToken? cancellationToken)
        {
            cancellationToken = default;
            return (uint)UnityEngine.Random.Range(0, choice.NextShowNodes.Length);
        } 

        public bool MoveNext() => MoveToNextNode() != null;

        private ShowNode MoveToNextNode()
        {
            if (Group == null) return null;

            // We are at the root if the stack has any node
            // otherwise we are at the end
            if (Current == null)
                return previousStack.Any() ? GetNextNode(ShowGraph.EntryPoint) : null;

            // Get Next
            if (Current is SceneNode scene)
            {
                previousStack.Push(Current);
                Current = GetNextNode(scene.NextShowNodes);
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
                Current = GetNextNode(choice.NextShowNodes[decisions & (choice.NextShowNodes.Length - 1)]);
            }

            return Current;
        }

        private ShowNode GetNextNode(List<ShowNode> nextShowNodes)
        {
            var query = from node in nextShowNodes
                        where node.Groups.Contains(Group)
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

    }
}
