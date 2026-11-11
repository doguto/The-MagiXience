using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using UnityEditor;
using UnityEngine;

namespace Project.Scenes.Battle.Editor.BehaviorTree.Migration
{
    public sealed partial class LegacyBattleConverter
    {
        static readonly Dictionary<string, Type> MotionTypes = new()
        {
            ["InfiniteMovementConfig"] = typeof(VelocityMotion),
            ["TweenMovementConfig"] = typeof(MoveToMotion),
            ["AimTweenMovementConfig"] = typeof(MoveToPlayerMotion),
            ["PathMovementConfig"] = typeof(PathMotion),
            ["AimStraightMovementConfig"] = typeof(AimStraightMotion),
            ["AimRotateConfig"] = typeof(AimRotateMotion),
            ["ForwardMovementConfig"] = typeof(ForwardMotion),
            ["HomingMovementConfig"] = typeof(HomingMotion),
            ["StraightWithHomingMovementConfig"] = typeof(StraightWithHomingMotion),
            ["SineMovementConfig"] = typeof(SineMotion),
            ["AnchoredSineMovementConfig"] = typeof(AnchoredSineMotion),
            ["QuadraticMovementConfig"] = typeof(QuadraticMotion),
            ["AcceleratedMovementConfig"] = typeof(AcceleratedMotion),
            ["VertexParabolaMovementConfig"] = typeof(VertexParabolaMotion),
            ["ReflectMovementConfig"] = typeof(ReflectMotion),
            ["ZigzagMovementConfig"] = typeof(ZigzagMotion),
            ["SpiralMovementConfig"] = typeof(SpiralMotion),
            ["DriftMovementConfig"] = typeof(DriftMotion),
            ["InvincibleFadeMoveMovementConfig"] = typeof(FadeMoveMotion),
            ["WaitMovementConfig"] = typeof(WaitMotion),
            ["InvincibleWaitMovementConfig"] = typeof(WaitMotion),
            ["AnimationMovementConfig"] = typeof(AnimationMotion),
            ["LoopMovementConfig"] = typeof(LoopMotion),
        };

        public MotionPreset ConvertMotionPreset(MovementPreset preset)
        {
            if (preset == null) return null;
            if (cache.TryGetValue(preset, out var cached)) return (MotionPreset)cached;

            var motion = CreateAsset<MotionPreset>("Motion", preset.name);
            cache[preset] = motion;
            motion.Steps.AddRange(ConvertSteps(preset.Steps));
            EditorUtility.SetDirty(motion);
            return motion;
        }

        public List<MotionStep> ConvertSteps(IEnumerable<IMovementStep> steps)
        {
            var result = new List<MotionStep>();
            if (steps == null) return result;
            foreach (var step in steps)
            {
                var converted = ConvertStep(step);
                if (converted != null) result.Add(converted);
            }
            return result;
        }

        MotionStep ConvertStep(IMovementStep step)
        {
            if (step == null) return null;
            var oldType = step.GetType().Name;
            if (!MotionTypes.TryGetValue(oldType, out var newType))
            {
                Warn($"未対応の移動: {oldType}");
                return null;
            }

            var motion = (MotionStep)Activator.CreateInstance(newType);
            CopyByName(step, motion);

            switch (motion)
            {
                case MoveToMotion or MoveToPlayerMotion or PathMotion:
                    Set(motion, "ease", ConvertEase(step));
                    break;
            }

            switch (step)
            {
                case InvincibleWaitMovementConfig:
                    Set(motion, "invincible", true);
                    break;
                case PathMovementConfig:
                    var pathType = Get<DG.Tweening.PathType>(step, "pathType");
                    Set(motion, "shape", pathType == DG.Tweening.PathType.Linear ? PathMotion.Shape.Linear : PathMotion.Shape.CatmullRom);
                    break;
                case LoopMovementConfig:
                    ConvertLoopEntries(step, (LoopMotion)motion);
                    break;
            }
            return motion;
        }

        MotionEase ConvertEase(object oldStep)
        {
            var preset = Get<EaseCurvePreset>(oldStep, "curvePreset");
            return new MotionEase
            {
                easeValue = Get<int>(oldStep, "easeValue"),
                customCurve = Get<AnimationCurve>(oldStep, "customCurve"),
                curvePreset = ConvertCurve(preset),
            };
        }

        EaseCurveAsset ConvertCurve(EaseCurvePreset preset)
        {
            if (preset == null) return null;
            if (cache.TryGetValue(preset, out var cached)) return (EaseCurveAsset)cached;

            var curve = CreateAsset<EaseCurveAsset>("Ease", preset.name);
            Set(curve, "curve", new AnimationCurve(preset.Curve.keys));
            cache[preset] = curve;
            EditorUtility.SetDirty(curve);
            return curve;
        }

        void ConvertLoopEntries(object oldLoop, LoopMotion loop)
        {
            var entries = new List<LoopMotion.Entry>();
            foreach (var oldEntry in Get<IList>(oldLoop, "entries") ?? Array.Empty<object>())
            {
                if (oldEntry == null) continue;
                entries.Add(new LoopMotion.Entry
                {
                    preset = ConvertMotionPreset(Get<MovementPreset>(oldEntry, "preset")),
                    inlineSteps = ConvertSteps(Get<IEnumerable<IMovementStep>>(oldEntry, "inlineSteps")),
                    repeatCount = Mathf.Max(1, Get<int>(oldEntry, "repeatCount")),
                });
            }
            Set(loop, "entries", entries);
        }

        // 同名・同型のフィールドを写す。enum は値で写す
        static void CopyByName(object from, object to)
        {
            for (var type = to.GetType(); type != null && type != typeof(object); type = type.BaseType)
            {
                foreach (var target in type.GetFields(Flags | System.Reflection.BindingFlags.DeclaredOnly))
                {
                    var source = FindField(from.GetType(), target.Name);
                    if (source == null) continue;
                    var value = source.GetValue(from);
                    if (value == null) continue;

                    if (target.FieldType.IsAssignableFrom(source.FieldType))
                    {
                        target.SetValue(to, value is ICloneable c && !(value is string) ? c.Clone() : value);
                    }
                    else if (target.FieldType.IsEnum && (source.FieldType.IsEnum || value is int))
                    {
                        target.SetValue(to, Enum.ToObject(target.FieldType, Convert.ToInt32(value)));
                    }
                }
            }
        }
    }
}
