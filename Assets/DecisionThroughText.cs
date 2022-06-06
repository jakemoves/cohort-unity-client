using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#warning The singlton is not thread safe
public class DecisionThroughText
{
    private static DecisionThroughText instance;
    public static DecisionThroughText Instance
    {
        get 
        { 
            // NOTE: Creating the instance here is not thread-safe
            if (instance == null)
            {
                // Just Incase
                instance = new DecisionThroughText();
            }

            return instance; 
        }
    }

    private Dictionary<string, Decisions> choiceDecisions;

    public Decisions this[string id] => choiceDecisions[id];
    public Decisions this[ShowGraphSystem.Runtime.ChoiceNode choice] => choiceDecisions[choice.ID];

    static DecisionThroughText()
    {
        instance = new DecisionThroughText();
    }

    private DecisionThroughText()
    {
        choiceDecisions = new Dictionary<string, Decisions>(10);
    }

    public void AddChoice(string nodeID, string[] groupKeys)
    {
        if (choiceDecisions.ContainsKey(nodeID))
        {
            if (choiceDecisions[nodeID] is null)
                choiceDecisions[nodeID] = new Decisions(groupKeys, nodeID);
            return;
        }

        choiceDecisions.Add(nodeID, new Decisions(groupKeys, nodeID));
    }

    public void AddChoice(ShowGraphSystem.Runtime.ChoiceNode choiceNode) 
        => AddChoice(choiceNode.ID, choiceNode.GroupKeyArray);

    public bool ContainsKey(string id) => choiceDecisions.ContainsKey(id);
    public bool ContainsKey(ShowGraphSystem.Runtime.ChoiceNode choiceNode) 
        => choiceDecisions.ContainsKey(choiceNode.ID);

    public class Decisions : IReadOnlyDictionary<string, bool?>
    {
        private readonly bool?[] boolArray;
        private readonly Dictionary<string, int> indices;

        public event EventHandler<Decisions> DecisionsChanged;

        public bool? this[string key]
        {
            get => boolArray[indices[key]];

            set
            {
                boolArray[indices[key]] = value;

                OnDecisionsChanged();
            }
        }

        public string NodeID { get; private set; }

        private void OnDecisionsChanged() =>
            DecisionsChanged?.Invoke(this, this);

        public string[] Keys { get; private set; }

        IEnumerable<string> IReadOnlyDictionary<string, bool?>.Keys => Keys;

        public IEnumerable<bool?> Values => from i in Enumerable.Range(0, Keys.Length)
                                            select boolArray[i];
        public int Count => Keys.Length;

        public Decisions (string[] keys, string nodeID)
        {
            if (keys.Length >= 32)
                throw new OverflowException("The length of the keys must not excede 31");

            Keys = keys;
            this.boolArray = new bool?[keys.Length];
            this.indices = Enumerable.Range(0, keys.Length).ToDictionary<int, string, int>( i => keys[i], i => i);

            NodeID = nodeID;
        }

        public IEnumerable AwaitDecisionsCoroutine(Action<int> continueAction, System.Threading.CancellationToken? cancellationToken = null, bool continueOnCancel = false)
        {
            // Check if cancelled
            if (cancellationToken?.IsCancellationRequested ?? false)
            {
                if (continueOnCancel)
                {
                    TryGetDecisionsValue(out int value);
                    continueAction(value);
                }

                yield break;
            }

            // Wait for decisions
            while (!HasAllGroupsDecided())
            {
                if (cancellationToken?.IsCancellationRequested ?? false)
                {
                    if (continueOnCancel)
                    {
                        TryGetDecisionsValue(out int value);
                        continueAction(value);
                    }

                    yield break;
                }

                yield return null;
            }

            // Finish
            {
                TryGetDecisionsValue(out int value);
                continueAction(value);
            }
            yield break;
        }

        public bool HasAllGroupsDecided() 
            => !boolArray.Contains(null);

        public bool TryGetDecisionsValue(out int value)
        {
            if (HasAllGroupsDecided())
            {
                value = 0;
                // TODO: Test
                for (int i = 0; i < Count; i++)
                {
                    value |= (boolArray[i].Value ? 1 : 0) << i;
                }
                return true;
            }
            else
            {
                value = -1;
                return false;
            }
        }

        public bool ContainsKey(string key) => Keys.Contains(key);

        public IEnumerator<KeyValuePair<string, bool?>> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return new KeyValuePair<string, bool?>(Keys[i], boolArray[i]);
            }
        }

        public bool TryGetValue(string key, out bool? value)
        {
            try
            {
                value = this[key];
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString() 
            => $"[{NodeID}] {string.Join(", ", from kv in this select $"{kv.Key}->{kv.Value}")}";
    }
}