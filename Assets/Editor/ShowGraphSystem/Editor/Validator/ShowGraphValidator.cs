using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System;


using ShowGraphSystem.Editor;

namespace ShowGraphSystem.Editor.Validation
{
    public class ShowGraphValidator
    {
        public static void ValidateGraph(ShowGraphView showGraphView)
        {
            if (showGraphView == null)
                throw new ArgumentNullException("ShowGraphView can not be null");

            // Root Validation
            RootValidation(showGraphView);

            //Path Validation
            // TODO: Results Dialogue
            //showGraphView.nodes.ForEach(node =>
            //{
            //    if (node is RootNode rootNode)
            //    {
            //        var res = ValidatePort(rootNode.output, showGraphView);
            //    }
            //    else if (node is ShowGraphNode showGraphNode)
            //    {
            //        var inputPortResult = ValidatePort(showGraphNode.inputPort, showGraphView);

            //        if (showGraphNode is SceneNode sceneNode)
            //        {
            //            var outputPortResult = ValidatePort(sceneNode.outputPort, showGraphView);
            //        }
            //        else if (showGraphNode is ChoiceNode choiceNode)
            //        {
            //            foreach (var port in choiceNode.ChoiceContainer.Children().Where(ve => ve is Port).Select(ve => ve as Port))
            //            {
            //                var outputPortResult = ValidatePort(port, showGraphView);
            //            }
            //        }
            //    }
            //});

            showGraphView.ports.ForEach(port =>
            {
                ValidatePort(port, showGraphView);
            });
            showGraphView.MarkDirtyRepaint();
        }

        private static PathValidationResults ValidatePort(Port port, ShowGraphView showGraphView)
        {
            // Filter Port's Node type
            if (!(port.node is ShowGraphNode || port.node is RootNode))
                return PathValidationResults.InvalidOperation;

            PathValidationResults results = new PathValidationResults();

            // Init
            var connectedPorts =
                (port.direction == Direction.Output ?
                port.connections.Select(e => e.input) :
                port.connections.Select(e => e.output)).ToList();

            var validConnectedPorts = connectedPorts.Where(p => p.node is ShowGraphNode || p.node is RootNode).ToList();

            // Check connected to InvalidNodes
            if (connectedPorts.Except(validConnectedPorts).Any())
            {
                results |= PathValidationResults.InvalidOperation;

                // TODO: UI Color
            }

            string[] portGroups =
                port.node is RootNode ?
                showGraphView.Groups.ToArray() :
                (port.node as ShowGraphNode).GetGroupSelection().Where(kv => kv.Value).Select(kv => kv.Key).ToArray();

            if (port.direction == Direction.Output)
            {
                results = ValidateOutputPort(port, results, validConnectedPorts, portGroups);
            }
            else
            {
                results = ValidateInputPort(port, results, validConnectedPorts, portGroups);
            }

            if (results == PathValidationResults.Valid)
                port.SetPortColorValid("Connections are Valid");

            port.node.Focus();
            port.node.Blur();

            return results;

            static PathValidationResults ValidateOutputPort(Port port, PathValidationResults results, List<Port> validConnectedPorts, string[] portGroups)
            {
                // Check Ambiguities
                var destinationGroups = (from p in validConnectedPorts
                                         from kv in (p.node as ShowGraphNode).GetGroupSelection()
                                         where kv.Value
                                         select (p, kv.Key)).ToList();

                // Get Ambiguities
                var duplicateDestinationsByGroups = (from pk in destinationGroups
                                                     group pk by pk.Key into g
                                                     where g.Count() > 1
                                                     select g);

                var missingGroups = portGroups.Except(destinationGroups.Select(pk => pk.Key).Distinct());

                // Port Results
                if (duplicateDestinationsByGroups.Any())
                {
                    results |= PathValidationResults.Ambiguous;
                    port.SetPortColorWarning($"Ambiguous Path for groups: {string.Join(", ", duplicateDestinationsByGroups.Select(g => g.Key))}");
                }

                if (missingGroups.Any())
                {
                    results |= PathValidationResults.Discontinuity;

                    string ambiguityMessage = results.HasFlag(PathValidationResults.Ambiguous) ? $"\n{port.tooltip}" : null;
                    port.SetPortColorError($"Discontinuity for groups: {string.Join(", ", missingGroups)}{ambiguityMessage}");
                }

                // Edge results (UI)
                foreach (var duplicateGroup in duplicateDestinationsByGroups)
                {
                    foreach (var pk in duplicateGroup)
                    {
                        Edge edge = port.connections.SingleOrDefault(e => (e.input == port && e.output == pk.p) || (e.output == port && e.input == pk.p));

                        if (!(edge is null))
                        {
                            edge.SetEdgeStyleWarning(string.Join(", ", missingGroups));
                        }
                    }
                }

                return results;
            }
            static PathValidationResults ValidateInputPort(Port port, PathValidationResults results, List<Port> validConnectedPorts, string[] portGroups)
            {
                if (validConnectedPorts.Any(p => p.node is RootNode))
                    return results;

                // Check For Discontinuities
                var missingGroups =
                portGroups.Except(
                    validConnectedPorts
                    .Where(p => p.node is ShowGraphNode)
                    .Select(p => (p.node as ShowGraphNode).GetGroupSelection().Where(kv => kv.Value).Select(kv => kv.Key))
                    .SelectMany(en => en)
                    .Distinct()
                );

                if (missingGroups.Any())
                {
                    results |= PathValidationResults.Discontinuity;

                    string ambiguityMessage = results.HasFlag(PathValidationResults.Ambiguous) ? $"\n{port.tooltip}" : null;
                    port.SetPortColorError($"Discontinuity for groups: {string.Join(", ", missingGroups)}{ambiguityMessage}");
                }

                return results;
            }
        }

        //private static IEnumerable<ShowGraphNode> TransverseGraphByGroup(ShowGraphView showGraphView, string group)
        //{

        //}

        private static void RootValidation(ShowGraphView showGraphView)
        {
            if (!showGraphView.HasRootNode)
            {
                EditorUtility.DisplayDialog("Graph Invalid: No Root Node", "Root node is missing...", "Ok");
                return;
            }

            if ((from n in showGraphView.nodes.ToList() where n is RootNode select n).Count() > 1)
            {
                EditorUtility.DisplayDialog("Graph Invalid: Multiple Root Nodes", "The show graph can not have multiple Roots.", "Ok");
                return;
            }

            if (showGraphView.Root is null)
                throw new NullReferenceException("The graph does contain a RootNode, but the Root property is null");
            else if (!showGraphView.Root.output.connections.Any())
            {
                EditorUtility.DisplayDialog("Graph Invalid: Root Node Disconnected", "The root must be connected to a Show Node.", "Ok");
                return;
            }
        }
    }

    [Flags]
    public enum PathValidationResults
    {
        Valid = 0,
        Ambiguous = 1,
        Discontinuity = 2,
        InvalidOperation = 4
    }
}
