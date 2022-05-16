using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Experimental.GraphView;

namespace ShowGraphSystem.Editor
{
    public static class EdgeExtentions
    {
        public static Serialization.BasicShowEdgeData ToBasicShowEdgeData(this Edge edge)
        {
            return new Serialization.BasicShowEdgeData(
                (edge.input.node as IShowGraphNode).ID,
                (edge.output.node as IShowGraphNode).ID,
                edge.input.portName,
                edge.output.portName);
        }

        public static bool CanGetShowEdgeData(this Edge edge) => (edge.input.node is Editor.IShowGraphNode) && (edge.output.node is Editor.IShowGraphNode);
    }
}
