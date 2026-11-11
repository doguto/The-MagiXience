using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Scenes.Battle.Editor.BehaviorTree
{
    // 端点付近ではクリックを拾わない線。ポートの上に線が重なっていてもポートを掴めるようにする。
    public class BehaviorTreeEdge : Edge
    {
        const float EndpointRadius = 14f;

        public override bool ContainsPoint(Vector2 localPoint)
        {
            var world = this.LocalToWorld(localPoint);
            if (Near(output, world) || Near(input, world)) return false;
            return base.ContainsPoint(localPoint);
        }

        static bool Near(Port port, Vector2 world)
        {
            if (port == null) return false;
            var center = port.worldBound.center;
            return (center - world).sqrMagnitude <= EndpointRadius * EndpointRadius;
        }
    }
}
