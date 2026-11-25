using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scenes.Battle.Editor.BehaviorTree
{
    // 標準 Port はラベル付き横向きポート前提の当たり判定のため、縦向き・ラベル無しでは入力側が反応しない。
    // 要素の矩形そのものを当たり判定にする(サイズは USS で大きめに取る)。
    public class BehaviorTreePort : Port
    {
        BehaviorTreePort(Direction direction, Capacity capacity)
            : base(Orientation.Vertical, direction, capacity, typeof(bool))
        {
        }

        public static BehaviorTreePort Create(Direction direction, Capacity capacity, Action<Port, Vector2> onDropOutside)
        {
            var port = new BehaviorTreePort(direction, capacity) { portName = "" };
            port.m_EdgeConnector = new EdgeConnector<BehaviorTreeEdge>(new Listener(onDropOutside));
            port.AddManipulator(port.m_EdgeConnector);
            return port;
        }

        public override bool ContainsPoint(Vector2 localPoint) => new Rect(Vector2.zero, layout.size).Contains(localPoint);

        sealed class Listener : IEdgeConnectorListener
        {
            readonly Action<Port, Vector2> onDropOutside;
            readonly GraphViewChange change;
            readonly List<Edge> edgesToCreate = new();
            readonly List<GraphElement> edgesToDelete = new();

            public Listener(Action<Port, Vector2> onDropOutside)
            {
                this.onDropOutside = onDropOutside;
                change.edgesToCreate = edgesToCreate;
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
                onDropOutside?.Invoke(edge.output ?? edge.input, position);
            }

            public void OnDrop(GraphView graphView, Edge edge)
            {
                edgesToCreate.Clear();
                edgesToCreate.Add(edge);

                edgesToDelete.Clear();
                if (edge.input.capacity == Capacity.Single)
                {
                    foreach (var e in edge.input.connections)
                    {
                        if (e != edge) edgesToDelete.Add(e);
                    }
                }
                if (edge.output.capacity == Capacity.Single)
                {
                    foreach (var e in edge.output.connections)
                    {
                        if (e != edge) edgesToDelete.Add(e);
                    }
                }
                if (edgesToDelete.Count > 0) graphView.DeleteElements(edgesToDelete);

                var result = graphView.graphViewChanged?.Invoke(change) ?? change;
                foreach (var e in result.edgesToCreate)
                {
                    graphView.AddElement(e);
                    edge.input.Connect(e);
                    edge.output.Connect(e);
                }
            }
        }
    }
}
