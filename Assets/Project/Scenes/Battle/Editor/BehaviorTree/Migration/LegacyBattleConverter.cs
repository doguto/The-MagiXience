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

        // 途中でファイルを作るとフォルダ作成などで再読み込みが走り、作成済みのアセットが空の状態に戻ってしまう。
        // メモリ上で全て組み立ててから Flush でまとめて書き出す。
        readonly List<(ScriptableObject asset, string dir)> pending = new();

        T CreateAsset<T>(string folder, string name) where T : ScriptableObject
        {
            var dir = string.IsNullOrEmpty(folder) ? outDir : $"{outDir}/{folder}";
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = UniqueName(name);
            pending.Add((asset, dir));
            return asset;
        }

        public void Flush()
        {
            foreach (var dir in pending.Select(p => p.dir).Distinct()) EnsureFolder(dir);

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var (asset, dir) in pending) AssetDatabase.CreateAsset(asset, $"{dir}/{asset.name}.asset");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // 参照先が全てアセット化された後に保存し直し、アセット間の参照を確定させる
            foreach (var (asset, _) in pending) EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            pending.Clear();
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

        // 子が無い Composite は作らない (null を返し、親側で取り除く)
        static NodeDefinition Sequence(IEnumerable<NodeDefinition> children)
        {
            var list = children.Where(c => c != null).ToList();
            if (list.Count == 0) return null;
            return list.Count == 1 ? list[0] : WithChildren(new SequenceNodeDefinition(), list);
        }

        static NodeDefinition Parallel(IEnumerable<NodeDefinition> children, ParallelPolicy policy = ParallelPolicy.All)
        {
            var list = children.Where(c => c != null).ToList();
            if (list.Count == 0) return null;
            if (list.Count == 1) return list[0];
            var p = new ParallelNodeDefinition { Policy = policy };
            return WithChildren(p, list);
        }

        static NodeDefinition Timeline(IEnumerable<NodeDefinition> children, int durationFrames = 0)
        {
            var list = children.Where(c => c != null).ToList();
            if (list.Count == 0) return durationFrames > 0 ? Wait(durationFrames) : null;
            if (durationFrames <= 0 && list.Count == 1) return list[0];
            var timed = TimedSequence(list, durationFrames);
            if (timed != null) return timed;
            var t = durationFrames > 0
                ? new ParallelNodeDefinition { Policy = ParallelPolicy.Duration, Duration = durationFrames }
                : new ParallelNodeDefinition { Policy = ParallelPolicy.All };
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

        static NodeDefinition Repeat(int count, int intervalFrames, NodeDefinition child)
        {
            if (child == null) return null;
            if (count == 1) return child;
            var r = new RepeatNodeDefinition { Count = new BtInt(count), Interval = new BtInt(Mathf.Max(1, intervalFrames)) };
            r.SetChildren(new[] { child });
            return r;
        }

        // 1 フレームで終わるノード (周期で打ち切られることがない)
        static bool IsInstant(NodeDefinition node) => BehaviorTreeGraphView.IsInstant(node);

        // Delay で開始時刻を付けた子が全て即時完了なら、Wait を挟んだ Sequence で表す
        static NodeDefinition TimedSequence(List<NodeDefinition> children, int durationFrames)
        {
            var timed = new List<(int time, NodeDefinition node)>();
            foreach (var child in children)
            {
                var time = 0;
                var node = child;
                if (child is DelayNodeDefinition delay)
                {
                    var frames = Get<BtInt>(delay, "frames");
                    if (!string.IsNullOrEmpty(frames.param) || delay.Children.Count == 0) return null;
                    time = frames.value;
                    node = delay.Children[0];
                }
                if (!IsInstant(node)) return null;
                if (durationFrames > 0 && time >= durationFrames) return null;
                timed.Add((time, node));
            }

            var list = new List<NodeDefinition>();
            var now = 0;
            foreach (var (time, node) in timed.OrderBy(x => x.time))
            {
                if (time > now) list.Add(Wait(time - now));
                list.Add(node);
                now = time;
            }
            if (durationFrames > now) list.Add(Wait(durationFrames - now));
            return list.Count == 1 ? list[0] : WithChildren(new SequenceNodeDefinition(), list);
        }

        static NodeDefinition Loop(int count, NodeDefinition child)
        {
            if (child == null) return null;
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
