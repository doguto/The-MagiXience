using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Attack;
using Project.Scenes.Battle.Scripts.Model.Attack.PhaseTrigger;
using Project.Scenes.Battle.Scripts.Model.ExitCondition;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Project.Scenes.Battle.Editor.BehaviorTree.Migration
{
    // 旧バトルデータ (BattleSequenceAsset / AttackPreset / MovementPreset / 敵 Prefab の設定) から
    // BehaviorTree 形式のアセットを生成する移行ツール。全ステージの移行が終わったら削除する。
    public sealed partial class LegacyBattleConverter
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        readonly string outDir;
        readonly string prefix;
        readonly List<string> warnings = new();
        readonly Dictionary<object, Object> cache = new();
        readonly HashSet<string> usedNames = new();

        public LegacyBattleConverter(string outDir, string prefix)
        {
            this.outDir = outDir;
            this.prefix = prefix;
        }

        public IReadOnlyList<string> Warnings => warnings;

        public static int Frames(double seconds) => (int)Math.Round(seconds * BattleContext.FramesPerSecond);

        // ---------------- reflection helpers ----------------

        static FieldInfo FindField(Type t, string name)
        {
            for (var type = t; type != null; type = type.BaseType)
            {
                var f = type.GetField(name, Flags | BindingFlags.DeclaredOnly);
                if (f != null) return f;
            }
            return null;
        }

        public static T Get<T>(object o, string name)
        {
            if (o == null) return default;
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException(o.GetType().Name, name);
            var v = f.GetValue(o);
            return v is T t ? t : default;
        }

        public static void Set(object o, string name, object value)
        {
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException(o.GetType().Name, name);
            f.SetValue(o, value);
        }

        void Warn(string message)
        {
            warnings.Add(message);
            Debug.LogWarning("[LegacyBattleConverter] " + message);
        }

        // ---------------- asset helpers ----------------

        string UniqueName(string name)
        {
            name = string.Join("_", name.Split(System.IO.Path.GetInvalidFileNameChars())).Replace(' ', '_');
            var candidate = name;
            for (var i = 2; !usedNames.Add(candidate); i++) candidate = $"{name}_{i}";
            return candidate;
        }

        T CreateAsset<T>(string folder, string name) where T : ScriptableObject
        {
            var dir = string.IsNullOrEmpty(folder) ? outDir : $"{outDir}/{folder}";
            EnsureFolder(dir);
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = UniqueName(name);
            AssetDatabase.CreateAsset(asset, $"{dir}/{asset.name}.asset");
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        BehaviorTreeAsset CreateTree(string folder, string name, NodeDefinition root, IEnumerable<TreeParameter> parameters = null)
        {
            var tree = CreateAsset<BehaviorTreeAsset>(folder, name);
            tree.Root = root;
            if (parameters != null) tree.Parameters.AddRange(parameters);
            TreeLayout.Apply(root);
            EditorUtility.SetDirty(tree);
            return tree;
        }

        // ---------------- node helpers ----------------

        static T WithChildren<T>(T node, IEnumerable<NodeDefinition> children) where T : NodeDefinition
        {
            node.SetChildren(children.Where(c => c != null).ToList());
            return node;
        }

        static NodeDefinition Sequence(IEnumerable<NodeDefinition> children)
        {
            var list = children.Where(c => c != null).ToList();
            return list.Count == 1 ? list[0] : WithChildren(new SequenceNodeDefinition(), list);
        }

        static NodeDefinition Parallel(IEnumerable<NodeDefinition> children, ParallelPolicy policy = ParallelPolicy.RequireAll)
        {
            var list = children.Where(c => c != null).ToList();
            if (list.Count == 1) return list[0];
            var p = new ParallelNodeDefinition { Policy = policy };
            return WithChildren(p, list);
        }

        static NodeDefinition Timeline(IEnumerable<NodeDefinition> children, int durationFrames = 0)
        {
            var list = children.Where(c => c != null).ToList();
            if (durationFrames <= 0 && list.Count == 1) return list[0];
            var t = new TimelineNodeDefinition { Duration = durationFrames };
            return WithChildren(t, list);
        }

        static NodeDefinition Delay(int frames, NodeDefinition child)
        {
            if (child == null) return null;
            if (frames <= 0) return child;
            var d = new DelayNodeDefinition();
            Set(d, "frames", new BtInt(frames));
            d.SetChildren(new NodeDefinition[] { child });
            return d;
        }

        static NodeDefinition Wait(int frames)
        {
            var w = new WaitNodeDefinition { Frames = frames };
            return w;
        }

        static NodeDefinition Loop(int count, NodeDefinition child)
        {
            var l = new LoopNodeDefinition { Count = count };
            l.SetChildren(new NodeDefinition[] { child });
            return l;
        }

        static NodeDefinition Until(ConditionDefinition condition, NodeDefinition child, UntilChildCompletion completion)
        {
            var u = new UntilNodeDefinition { Condition = condition, OnChildCompleted = completion };
            u.SetChildren(new NodeDefinition[] { child ?? Wait(0) });
            return u;
        }

        static NodeDefinition WaitUntil(ConditionDefinition condition)
        {
            var w = new WaitUntilNodeDefinition();
            Set(w, "condition", condition);
            return w;
        }

        static NodeDefinition Check(ConditionDefinition condition)
        {
            var c = new CheckConditionNodeDefinition();
            Set(c, "condition", condition);
            return c;
        }

        static NodeDefinition Marker(string phaseId)
        {
            var m = new PhaseMarkerNodeDefinition();
            Set(m, "phaseId", phaseId ?? "");
            return m;
        }

        static ConditionDefinition FrameElapsed(int frames)
        {
            var c = new FrameElapsedConditionDefinition();
            Set(c, "frames", new BtInt(frames));
            return c;
        }

        static ConditionDefinition HpBelow(HpTarget target, float percent)
        {
            var c = new HpBelowConditionDefinition();
            Set(c, "target", target);
            Set(c, "percent", percent);
            return c;
        }

        static NodeDefinition WithComment(NodeDefinition node, string comment)
        {
            if (node != null && !string.IsNullOrEmpty(comment)) Set(node, "comment", comment);
            return node;
        }
    }

    // 生成したツリーを上から下へ並べる (エディタの Auto Layout と同じ考え方の簡易版)
    static class TreeLayout
    {
        const float Width = 230f;
        const float Height = 150f;

        public static void Apply(NodeDefinition root)
        {
            if (root == null) return;
            var next = 0f;
            Place(root, 1, ref next);
        }

        static float Place(NodeDefinition node, int depth, ref float nextX)
        {
            var children = node.Children.Where(c => c != null).ToList();
            float x;
            if (children.Count == 0)
            {
                x = nextX;
                nextX += Width;
            }
            else
            {
                var xs = new List<float>();
                foreach (var child in children) xs.Add(Place(child, depth + 1, ref nextX));
                x = (xs.First() + xs.Last()) / 2f;
            }
            node.GraphPosition = new Vector2(x, depth * Height);
            return x;
        }
    }
}
