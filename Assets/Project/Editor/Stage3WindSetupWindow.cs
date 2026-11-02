#if UNITY_EDITOR
using System;
using System.Linq;
using DG.Tweening;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Attack;
using Project.Scenes.Battle.Scripts.Model.ExitCondition;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scripts.Infra;
using Project.Commons.Debugger.Scripts.Presenter;
using Project.Scripts.Model;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Project.Editor
{
    /// <summary>Unityの編集UIからStage3の試作データを作成する。既存のPhase1〜5は変更しない。</summary>
    public class Stage3WindSetupWindow : EditorWindow
    {
        const string Data = "Assets/Project/DataStore/Stage3/";
        const string Prefabs = "Assets/Project/Scenes/Battle/Prefabs/Stage3/";
        const string SpriteFolder = "Assets/Project/Textures/Character/Tatsumi/Dot/";
        Sprite bossSprite;
        Sprite armSprite;
        string result;

        [MenuItem("Tools/Stage3/Wind Stage Setup")]
        static void Open() => GetWindow<Stage3WindSetupWindow>("Stage3 風ステージ作成");

        void OnEnable()
        {
            LoadSprites();
        }

        void LoadSprites()
        {
            bossSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "Tatsumi_Dot.png");
            armSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "Tatsumi_WindCue_Dot.png");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Phase6〜8・本ボスの試作", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Phase1〜5は保持します。Phase6〜8、専用プリセット、Stage3Boss、3_Tatsumiを作成・更新します。\n風：上 → 下 → 左。敵は上下同時、放物弾＋扇状通常弾。\nボス：HP1000、半分で強化。7秒ごとに放物弾／N-wayを交互に使用。", MessageType.Info);
            bossSprite = (Sprite)EditorGUILayout.ObjectField("ボス画像（仮差替可）", bossSprite, typeof(Sprite), false);
            armSprite = (Sprite)EditorGUILayout.ObjectField("腕上げ画像（任意）", armSprite, typeof(Sprite), false);
            EditorGUILayout.HelpBox("通常画像：Downloads/untitled_8.png\n合図画像：Downloads/untitled_14.png\nステージ2のボスを基にHP表示・強化・撃破演出を継承しています。", MessageType.Info);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("タツミの2枚をSpriteとして取り込む"))
                {
                    foreach (var file in new[] { "Tatsumi_Dot.png", "Tatsumi_WindCue_Dot.png" })
                    {
                        var path = SpriteFolder + file;
                        AssetDatabase.ImportAsset(path);
                        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.spritePixelsPerUnit = 100f;
                        importer.filterMode = FilterMode.Point;
                        importer.mipmapEnabled = false;
                        importer.alphaIsTransparency = true;
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        importer.SaveAndReimport();
                    }
                    LoadSprites();
                    result = "通常画像・合図画像の取り込み完了。";
                }
            using (new EditorGUI.DisabledScope(Application.isPlaying || bossSprite == null || armSprite == null))
                if (GUILayout.Button("ボスの画像・合図・当たり判定だけ更新", GUILayout.Height(30)))
                {
                    try { Selection.activeObject = MakeBossPrefab(); AssetDatabase.SaveAssets(); result = "タツミPrefabを更新しました。"; }
                    catch (Exception ex) { result = ex.Message; Debug.LogException(ex); }
                }
            using (new EditorGUI.DisabledScope(Application.isPlaying || bossSprite == null))
                if (GUILayout.Button("Phase6〜8 とボスを作成・保存", GUILayout.Height(36)))
                {
                    try { Build(); result = "作成完了。参照と風の数値チェック成功。詳細はConsole。"; }
                    catch (Exception ex) { result = ex.Message; Debug.LogException(ex); }
                }
            if (!string.IsNullOrEmpty(result)) EditorGUILayout.HelpBox(result, MessageType.Info);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("Phase6から道中を確認再生"))
                {
                    var initializer = FindFirstObjectByType<DebugRuntimeModelInitializer>();
                    if (initializer == null) throw new InvalidOperationException("Open Battle scene first");
                    var init = new SerializedObject(initializer);
                    init.FindProperty("stageNumber").intValue = 3;
                    init.FindProperty("situation").enumValueIndex = (int)BattleSituation.Way;
                    init.FindProperty("startPhaseIndex").intValue = 5;
                    init.ApplyModifiedProperties();
                    EditorApplication.isPlaying = true;
                }
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("再生中の動作確認（保存されません）", EditorStyles.boldLabel);
                var player = FindFirstObjectByType<PlayerEntityPresenter>();
                if (GUILayout.Button("確認用に自機HPを一時的に増やす")) player?.Model?.SetMaxHpAndRefill(100000);
                var boss = FindFirstObjectByType<BossEntityPresenter>();
                if (boss?.Model != null)
                {
                    EditorGUILayout.LabelField($"HP {boss.Model.CurrentHp.Value} / 強化 {boss.Model.IsInStrongMode} / 風 {BattleWind.Direction}");
                    if (GUILayout.Button("前半HPを0にして強化切替を確認")) boss.Model.TakeDamage(boss.Model.NormalHp.Value);
                    if (GUILayout.Button("撃破演出を確認")) boss.Model.TakeDamage(boss.Model.MaxHp);
                }
                Repaint();
            }
        }

        static T Asset<T>(string path, string template = null) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            if (template != null)
            {
                if (!AssetDatabase.CopyAsset(template, path)) throw new InvalidOperationException("Cannot copy " + template);
                asset = AssetDatabase.LoadAssetAtPath<T>(path);
            }
            else { asset = CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            return asset;
        }

        static SerializedProperty P(SerializedProperty parent, string name) => parent.FindPropertyRelative(name);
        static SerializedObject Edit(UnityEngine.Object target)
        {
            Undo.RecordObject(target, "Stage3 wind setup");
            return new SerializedObject(target);
        }
        static void Apply(SerializedObject so) { so.ApplyModifiedProperties(); EditorUtility.SetDirty(so.targetObject); }

        void Build()
        {
            CheckWindMath();
            var hold = MakeMovement("3_Wind_HoldAndExit", false);
            var sweep = MakeMovement("3_Wind_DiagonalSweep", true);
            var fan = MakeFan();
            var showerUp = MakeShower("3_Wind_ShowerUp", 1);
            var showerDown = MakeShower("3_Wind_ShowerDown", -1);
            MakeWave(6, hold, sweep, fan, showerUp, showerDown, 4, 4f);
            MakeWave(7, hold, sweep, fan, showerUp, showerDown, 3, 3.6f);
            MakeWave(8, hold, sweep, fan, showerUp, showerDown, 4, 3f);
            UpdateWay();
            var prefab = MakeBossPrefab();
            MakeBossSequence(prefab);
            var table = AssetDatabase.LoadAssetAtPath<StageDataObject>("Assets/Project/DataStore/StageData.asset");
            var so = Edit(table);
            var rows = so.FindProperty("stageData");
            for (int i = 0; i < rows.arraySize; i++)
                if (P(rows.GetArrayElementAtIndex(i), "stageNumber").intValue == 3)
                    P(rows.GetArrayElementAtIndex(i), "bossSequenceAddress").stringValue = Data + "Boss/Stage3Boss.asset";
            Apply(so);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) throw new InvalidOperationException("Addressable settings missing");
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(Data + "Boss/Stage3Boss.asset"), settings.DefaultGroup);
            entry.address = Data + "Boss/Stage3Boss.asset";
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[Stage3WindSetup] Phase6:16 enemies/Up, Phase7:12 enemies/Down, Phase8:16 enemies/Left. Boss HP1000/50% strong. Sprite: " + AssetDatabase.GetAssetPath(bossSprite));
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<BattleSequenceAsset>(Data + "Way/Stage3Way.asset");
        }

        static MovementPreset MakeMovement(string name, bool diagonal)
        {
            var asset = Asset<MovementPreset>(Data + "Movement/" + name + ".asset");
            var so = Edit(asset);
            var steps = so.FindProperty("steps");
            steps.arraySize = 3;
            steps.GetArrayElementAtIndex(0).managedReferenceValue = new TweenMovementConfig(new Vector3(-6.5f, 0f, 0f), 1.4f, (int)Ease.OutSine, true);
            steps.GetArrayElementAtIndex(1).managedReferenceValue = new WaitMovementConfig();
            steps.GetArrayElementAtIndex(2).managedReferenceValue = new TweenMovementConfig(new Vector3(-20f, diagonal ? 2f : 0f, 0f), 4.5f, (int)Ease.InSine, true);
            Apply(so);
            so.Update();
            P(so.FindProperty("steps").GetArrayElementAtIndex(1), "duration").floatValue = diagonal ? 2.8f : 4.5f;
            Apply(so);
            return asset;
        }

        static AttackPreset MakeFan()
        {
            var asset = Asset<AttackPreset>(Data + "Attack/3_Wind_Fan5.asset", Data + "Attack/Normal_Fan3.asset");
            var so = Edit(asset);
            var phase = P(so.FindProperty("attackTimeline"), "phases").GetArrayElementAtIndex(0);
            P(phase, "loopStart").floatValue = 1.5f;
            P(phase, "loopEnd").floatValue = 5.4f;
            P(phase, "cycleDuration").floatValue = 0.8f;
            var entry = P(phase, "entries").GetArrayElementAtIndex(0);
            P(entry, "directionProvider").managedReferenceValue = new AimDirectionConfig();
            P(P(entry, "signal"), "wayCount").intValue = 5;
            P(P(entry, "signal"), "spreadAngle").floatValue = 20f;
            Apply(so);
            return asset;
        }

        static AttackPreset MakeShower(string name, int sign)
        {
            var movement = Asset<MovementPreset>(Data + "Bullet/" + name + ".asset");
            var movementSo = Edit(movement);
            var steps = movementSo.FindProperty("steps");
            steps.arraySize = 1;
            steps.GetArrayElementAtIndex(0).managedReferenceValue = new VertexParabolaMovementConfig(new Vector2(-7.5f, sign * 3.8f), 8f);
            Apply(movementSo);
            var attack = Asset<AttackPreset>(Data + "Attack/" + name + ".asset", Data + "Attack/3_Phase4_ShowerDown.asset");
            var so = Edit(attack);
            var phase = P(so.FindProperty("attackTimeline"), "phases").GetArrayElementAtIndex(0);
            P(phase, "loopStart").floatValue = 1.6f;
            P(phase, "loopEnd").floatValue = 5.2f;
            P(phase, "cycleDuration").floatValue = 0.32f;
            var entry = P(phase, "entries").GetArrayElementAtIndex(0);
            P(entry, "bulletMovementOverride").objectReferenceValue = movement;
            P(entry, "directionProvider").managedReferenceValue = new FixedDirectionConfig();
            Apply(so);
            return attack;
        }

        static void MakeWave(int number, MovementPreset hold, MovementPreset sweep, AttackPreset fan,
            AttackPreset up, AttackPreset down, int waves, float gap)
        {
            var asset = Asset<BattleTimelineBuilderAsset>(Data + $"Way/3_Phase{number}.asset");
            var blue = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "3_Enemy_skyblue.prefab");
            var pink = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "3_Enemy_pink.prefab");
            var so = Edit(asset);
            var tracks = so.FindProperty("enemySpawnTracks");
            tracks.arraySize = 1;
            var track = tracks.GetArrayElementAtIndex(0);
            P(track, "trackName").stringValue = "EnemySpawn";
            var clips = P(track, "clips");
            clips.arraySize = waves * 4;
            for (int wave = 0; wave < waves; wave++)
                for (int slot = 0; slot < 4; slot++)
                {
                    bool top = slot % 2 == 0;
                    bool guard = slot >= 2;
                    float sign = top ? 1f : -1f;
                    var clip = clips.GetArrayElementAtIndex(wave * 4 + slot);
                    P(clip, "time").doubleValue = wave * gap + (guard ? 1.3f : 0f);
                    P(clip, "spawnPosition").vector3Value = new Vector3(10f + (guard ? 0.8f : 0f), sign * (guard ? 1.15f : 2.6f), 0f);
                    P(clip, "prefab").objectReferenceValue = guard ? pink : blue;
                    P(clip, "movementOverride").objectReferenceValue = guard ? sweep : hold;
                    P(clip, "attackOverride").objectReferenceValue = guard ? fan : (top ? down : up);
                    P(clip, "bulletMovementOverride").objectReferenceValue = null;
                }
            Apply(so);
        }

        static void Wind(SerializedProperty wind, WindDirection direction)
        {
            P(wind, "enabled").boolValue = true;
            P(wind, "playerSpeedRatio").floatValue = 0.2f;
            P(wind, "bulletAcceleration").floatValue = 2f;
            P(wind, "bulletDrag").floatValue = 0.25f;
            var intervals = P(wind, "intervals");
            intervals.arraySize = 1;
            P(intervals.GetArrayElementAtIndex(0), "direction").enumValueIndex = (int)direction;
            P(intervals.GetArrayElementAtIndex(0), "seconds").floatValue = 3600f;
        }

        static void UpdateWay()
        {
            var asset = AssetDatabase.LoadAssetAtPath<BattleSequenceAsset>(Data + "Way/Stage3Way.asset");
            var so = Edit(asset);
            var phases = P(so.FindProperty("sequenceGroups").GetArrayElementAtIndex(0), "phases");
            phases.arraySize = 8;
            for (int i = 5; i < 8; i++)
            {
                var phase = phases.GetArrayElementAtIndex(i);
                P(phase, "phaseId").stringValue = (i + 1).ToString();
                P(phase, "timelineBuilder").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BattleTimelineBuilderAsset>(Data + $"Way/3_Phase{i + 1}.asset");
                P(phase, "exitConditionConfig").managedReferenceValue = new AllEnemiesDefeatedExitConditionConfig();
                P(phase, "overrideWind").boolValue = true;
                Wind(P(phase, "wind"), i == 5 ? WindDirection.Up : i == 6 ? WindDirection.Down : WindDirection.Left);
            }
            Apply(so);
        }

        GameObject MakeBossPrefab()
        {
            string path = Prefabs + "3_Tatsumi.prefab";
            var root = PrefabUtility.LoadPrefabContents(AssetDatabase.LoadAssetAtPath<GameObject>(path) != null
                ? path : "Assets/Project/Scenes/Battle/Prefabs/Stage2/2_Azuma.prefab");
            try
            {
                root.name = "3_Tatsumi";
                var renderer = root.GetComponent<SpriteRenderer>();
                renderer.sprite = bossSprite;
                renderer.flipX = false;
                float oldScale = root.transform.localScale.x;
                float scale = 2.2f / bossSprite.bounds.size.y;
                root.transform.localScale = new Vector3(scale, scale, 1f);
                var body = root.GetComponent<CapsuleCollider2D>();
                if (body == null) body = root.AddComponent<CapsuleCollider2D>();
                // Reuse the Stage2 capsule; the hair and signalling arm are outside the body hitbox.
                var size = bossSprite.bounds.size;
                body.size = new Vector2(size.x * 0.43f, size.y * 0.70f);
                body.offset = new Vector2(size.x * 0.02f, -size.y * 0.02f);
                body.direction = CapsuleDirection2D.Vertical;
                body.isTrigger = true;
                body.enabled = true;
                foreach (var extra in root.GetComponents<BoxCollider2D>()) DestroyImmediate(extra);
                var boss = root.GetComponent<BossEntityPresenter>();
                var bossSo = Edit(boss);
                bossSo.FindProperty("maxHp").intValue = 1000;
                bossSo.FindProperty("strongHpRatio").floatValue = 0.5f;
                bossSo.FindProperty("enemySpawnPrefabs").arraySize = 0;
                var pools = root.GetComponentsInChildren<BulletPool>(true);
                if (pools.Length < 2) throw new InvalidOperationException("Boss template needs two bullet pools");
                var existingController = root.GetComponent<Stage3WindBossController>();
                var existingSo = existingController != null ? new SerializedObject(existingController) : null;
                var normal = existingSo?.FindProperty("normalBulletPool").objectReferenceValue as BulletPool;
                var parabola = existingSo?.FindProperty("parabolaBulletPool").objectReferenceValue as BulletPool;
                if (normal == null) normal = pools[0];
                if (parabola == null) parabola = pools.First(p => p != normal);
                normal.name = "LinearBulletPool";
                parabola.name = "ParabolaBulletPool";
                var yellow = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "3_Enemy_pink.prefab").GetComponentInChildren<BulletPool>(true);
                var yellowRef = new SerializedObject(yellow).FindProperty("bulletPrefab").objectReferenceValue;
                foreach (var pair in new[] { (pool: normal, bullet: yellowRef), (pool: parabola, bullet: (UnityEngine.Object)AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "3_Bullet_ParabolaGreen.prefab").GetComponent<BulletEntityPresenter>()) })
                {
                    var poolSo = Edit(pair.pool);
                    poolSo.FindProperty("bulletPrefab").objectReferenceValue = pair.bullet;
                    Apply(poolSo);
                    pair.pool.transform.localPosition = Vector3.zero;
                }
                var references = bossSo.FindProperty("bulletPools");
                references.arraySize = 2;
                references.GetArrayElementAtIndex(0).objectReferenceValue = normal;
                references.GetArrayElementAtIndex(1).objectReferenceValue = parabola;
                Apply(bossSo);
                foreach (var unused in pools)
                    if (unused != normal && unused != parabola) DestroyImmediate(unused.gameObject);
                // Keep the template's HP bar and death sequence at their original world size.
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                    if (canvas.transform.parent == root.transform) canvas.transform.localScale *= oldScale / scale;
                var controller = root.GetComponent<Stage3WindBossController>();
                if (controller == null) controller = root.AddComponent<Stage3WindBossController>();
                var controllerSo = Edit(controller);
                controllerSo.FindProperty("normalBulletPool").objectReferenceValue = normal;
                controllerSo.FindProperty("parabolaBulletPool").objectReferenceValue = parabola;
                controllerSo.FindProperty("raisedArmsSprite").objectReferenceValue = armSprite;
                Apply(controllerSo);
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void MakeBossSequence(GameObject prefab)
        {
            var builder = Asset<BattleTimelineBuilderAsset>(Data + "Boss/3_Boss_WindCycle.asset");
            var asset = Asset<BattleSequenceAsset>(Data + "Boss/Stage3Boss.asset");
            var so = Edit(asset);
            so.FindProperty("situation").enumValueIndex = 1;
            so.FindProperty("bossPrefab").objectReferenceValue = prefab;
            so.FindProperty("bossSpawnPosition").vector3Value = new Vector3(10f, 0f, 0f);
            var entrance = so.FindProperty("bossEntranceMovement");
            entrance.arraySize = 1;
            entrance.GetArrayElementAtIndex(0).managedReferenceValue = new TweenMovementConfig(new Vector3(5.7f, 0f, 0f), 2f, (int)Ease.OutSine, false);
            var groups = so.FindProperty("sequenceGroups");
            groups.arraySize = 1;
            var group = groups.GetArrayElementAtIndex(0);
            P(group, "loop").boolValue = true;
            P(group, "loopCount").intValue = 0;
            P(group, "randomPick").boolValue = false;
            var phases = P(group, "phases");
            phases.arraySize = 1;
            var phase = phases.GetArrayElementAtIndex(0);
            P(phase, "phaseId").stringValue = "WindCycle";
            P(phase, "timelineBuilder").objectReferenceValue = builder;
            P(phase, "timelineBuilderStrong").objectReferenceValue = builder;
            P(phase, "exitConditionConfig").managedReferenceValue = new TimeLimitExitConditionConfig(7f);
            Apply(so);
        }

        static void CheckWindMath()
        {
            try
            {
                BattleWind.Begin(new BattleWindSettings { enabled = true, bulletAcceleration = 3f, bulletDrag = 0.6f,
                    intervals = new() { new WindInterval { direction = WindDirection.Right, seconds = 3600f } } });
                Vector2 velocity = Vector2.zero, offset = Vector2.zero;
                BattleWind.IntegrateBullet(Vector2.zero, 1f, ref velocity, ref offset);
                float expectedVelocity = 5f * (1f - Mathf.Exp(-0.6f));
                if (Mathf.Abs(velocity.x - expectedVelocity) > 0.0001f) throw new Exception("Wind analytic velocity check failed");
                var oneStep = offset;
                velocity = offset = Vector2.zero;
                for (int i = 0; i < 100; i++) BattleWind.IntegrateBullet(Vector2.zero, 0.01f, ref velocity, ref offset);
                if (Vector2.Distance(oneStep, offset) > 0.0002f) throw new Exception("Wind frame-rate independence check failed");
                velocity = offset = Vector2.zero;
                BattleWind.IntegrateBullet(new Vector2(-6f, 0f), 100f, ref velocity, ref offset);
                if (Mathf.Abs((-6f + velocity.x) - 5f) > 0.0001f) throw new Exception("Wind terminal speed check failed");
                BattleWind.Reset();
                velocity = offset = Vector2.zero;
                BattleWind.IntegrateBullet(new Vector2(-6f, 4f), 1f, ref velocity, ref offset);
                if (offset != Vector2.zero) throw new Exception("No-wind trajectory changed");
                Debug.Log("[Stage3WindSetup] Wind checks passed: analytic solution, frame-rate independence, terminal speed, no-wind preservation.");
            }
            finally { BattleWind.Reset(); }
        }
    }
}
#endif
