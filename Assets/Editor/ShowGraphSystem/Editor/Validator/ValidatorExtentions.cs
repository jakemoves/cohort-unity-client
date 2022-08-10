using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System;

using ShowGraphSystem.Editor;

namespace ShowGraphSystem.Editor.Validation
{
    public static class ValidatorExtentions
    {
        public static readonly StyleColor ErrorStyleColor = new StyleColor(Color.red);
        public static readonly StyleColor WarningStyleColor = new StyleColor(Color.yellow);
        public static readonly StyleColor ValidStyleColor = new StyleColor(Color.green);

        public static Dictionary<string, StyleColor> OriginalStyleColors { get; } = new Dictionary<string, StyleColor>();

        // From https://www.techiedelight.com/find-duplicates-in-list-csharp/
        public static IEnumerable<TSource> Duplicates<TSource>(this IEnumerable<TSource> source) =>
            source.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);

        public static void ValidateNodeEdges(this Edge edge)
        {
            if (!(edge.output.node is ShowGraphNode))
                ;// Invalid Operaion

            if (edge.input.node is SceneNode sceneNode)
            {

            }
            else if (edge.input.node is ChoiceNode choiceNode)
            {

            }
            else
            {
                // Invalid Operation
            }


        }

        public static void SetEdgeStyleWarning(this Edge edge, string message = null)
        {
            string edgeStyleColorKey = $"{nameof(edge)}.{nameof(edge.style)}.{nameof(edge.style.color)}";
            if (!OriginalStyleColors.ContainsKey(edgeStyleColorKey))
                OriginalStyleColors.Add(edgeStyleColorKey, edge.style.color);

            edge.style.color = WarningStyleColor;

            if (!string.IsNullOrWhiteSpace(message))
            {
                edge.title = message;
            }
        }

        public static void SetPortColorValid(this Port port, string message = null)
        {
            string portColorKey = $"{nameof(port)}.{nameof(port.portColor)}";
            if (!OriginalStyleColors.ContainsKey(portColorKey))
                OriginalStyleColors.Add(portColorKey, port.portColor);

            port.portColor = ValidStyleColor.value;

            if (!string.IsNullOrWhiteSpace(message))
            {
                port.tooltip = message;
            }
        }

        public static void SetPortColorWarning(this Port port, string message = null)
        {
            string portColorKey = $"{nameof(port)}.{nameof(port.portColor)}";
            if (!OriginalStyleColors.ContainsKey(portColorKey))
                OriginalStyleColors.Add(portColorKey, port.portColor);

            port.portColor = WarningStyleColor.value;

            if (!string.IsNullOrWhiteSpace(message))
            {
                port.tooltip = message;
            }
        }

        public static void SetPortColorError(this Port port, string message = null)
        {
            string portColorKey = $"{nameof(port)}.{nameof(port.portColor)}";
            if (!OriginalStyleColors.ContainsKey(portColorKey))
                OriginalStyleColors.Add(portColorKey, port.portColor);

            port.portColor = ErrorStyleColor.value;

            if (!string.IsNullOrWhiteSpace(message))
            {
                port.tooltip = message;
            }
        }
    } 
}