#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
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
        Vector2 scroll;
        [SerializeField] bool prepareStrongPreview;

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

        void Update()
        {
            if (!prepareStrongPreview || !Application.isPlaying) return;
            var player = FindFirstObjectByType<PlayerEntityPresenter>();
            var boss = FindFirstObjectByType<BossEntityPresenter>();
            if (player?.Model == null || boss?.Model == null) return;
            player.Model.SetMaxHpAndRefill(100000);
            boss.Model.TakeDamage(boss.Model.NormalHp.Value);
            prepareStrongPreview = false;
        }

        void OnGUI()
        {
            DrawRuntimePreview();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("Phase1〜8の出現秒数を小数第1位へ丸めて保存"))
                {
                    try { RoundPhaseSpawnTimes(); result = "Phase1〜8の出現秒数を小数第1位に揃えました。"; }
                    catch (Exception ex) { result = ex.Message; Debug.LogException(ex); }
                }
            EditorGUILayout.LabelField("強化後の風に沿う並行弾", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("untitled_15の紫弾を風上の画面外から3発並行に発射。初速1.5／中心間隔2.0（自機の身長約1.5体分）。ボスの本体攻撃は保持します。", MessageType.Info);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("画面外攻撃を紫の並行弾へ更新・保存", GUILayout.Height(30)))
                {
                    try { UpdateWindParallel(); result = "並行弾を保存。4方向・間隔・弾数・速度の確認成功。"; }
                    catch (Exception ex) { result = ex.Message; Debug.LogException(ex); }
                }
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("現在の開始設定で再生・自機HP補助とボス強化を確認"))
                {
                    prepareStrongPreview = true;
                    EditorApplication.isPlaying = true;
                }
            EditorGUILayout.LabelField("ボスのプリセット移行", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("現在の本ボスをStage1・2形式へ移行します。道中・中ボス・画像・サイズ・HPは保持。\n弾速：通常3／放物8、発射間隔：0.3／強化0.2秒、合図0.9秒、風加速度1.5／抵抗0.25。", MessageType.Info);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("本ボスをプリセットへ移行・保存", GUILayout.Height(32)))
                {
                    try { MigrateBossPresets(); result = "移行・保存完了。弾数・軌道・風順・移動の比較チェック成功。"; }
                    catch (Exception ex) { result = ex.Message; Debug.LogException(ex); }
                }
                if (GUILayout.Button("移行したボスの設定・攻撃を検証"))
                {
                    try { ValidateBossPresets(); result = "比較チェック成功。詳細はConsole。"; }
                    catch (Exception ex) { result = ex.Message; Debug.LogException(ex); }
                }
            }
            EditorGUILayout.Space();
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
            EditorGUILayout.EndScrollView();
        }

        void DrawRuntimePreview()
        {
            if (!Application.isPlaying) return;
            EditorGUILayout.LabelField("再生中の動作確認（保存されません）", EditorStyles.boldLabel);
            var player = FindFirstObjectByType<PlayerEntityPresenter>();
            if (GUILayout.Button("確認用に自機HPを一時的に増やす")) player?.Model?.SetMaxHpAndRefill(100000);
            var boss = FindFirstObjectByType<BossEntityPresenter>();
            if (boss?.Model != null)
            {
                EditorGUILayout.LabelField($"HP {boss.Model.CurrentHp.Value} / 強化 {boss.Model.IsInStrongMode} / 風 {BattleWind.Direction}");
                if (GUILayout.Button("前半HPを0にして強化切替を確認")) boss.Model.TakeDamage(boss.Model.NormalHp.Value);
                if (GUILayout.Button("フェーズ切替時の移動継続を検証"))
                {
                    try { ValidateBossMovementContinuity(boss); }
                    catch (Exception ex) { Debug.LogException(ex); }
                }
                if (GUILayout.Button("撃破演出を確認")) boss.Model.TakeDamage(boss.Model.MaxHp);
            }
            Repaint();
        }

        static void ValidateBossMovementContinuity(BossEntityPresenter boss)
        {
            var sequence = AssetDatabase.LoadAssetAtPath<BattleSequenceAsset>(Data + "Boss/Stage3Boss.asset");
            var movementField = typeof(BossEntityPresenter).GetField("movementCts",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            object playback = movementField.GetValue(boss);
            if (playback == null) throw new InvalidOperationException("Wait for the boss movement to start");
            Vector3 position = boss.transform.position;
            int checkedPhases = 0;
            foreach (var phase in sequence.SequenceGroups[0].Phases)
                foreach (var builder in new[] { phase.TimelineBuilder, phase.TimelineBuilderStrong })
                {
                    boss.OnPhaseStarted(null, builder);
                    if (!ReferenceEquals(playback, movementField.GetValue(boss)))
                        throw new Exception("Movement restarted at " + builder.name);
                    if (boss.transform.position != position) throw new Exception("Boss position changed at phase start");
                    checkedPhases++;
                }
            boss.OnPhaseStarted(null, null);
            if (movementField.GetValue(boss) != null) throw new Exception("Missing movement did not stop playback");
            boss.OnPhaseStarted(null, sequence.SequenceGroups[0].Phases[0].TimelineBuilder);
            if (movementField.GetValue(boss) == null || ReferenceEquals(playback, movementField.GetValue(boss)))
                throw new Exception("Stopped movement did not restart");
            Debug.Log($"[Stage3BossMovement] Passed {checkedPhases} normal/strong phase switches without restarting movement or changing position; stop/restart also passed.");
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
            MigrateBossPresets();
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
                    P(clip, "time").doubleValue = Math.Round(wave * (double)gap + (guard ? 1.3d : 0d), 1, MidpointRounding.AwayFromZero);
                    P(clip, "spawnPosition").vector3Value = new Vector3(10f + (guard ? 0.8f : 0f), sign * (guard ? 1.15f : 2.6f), 0f);
                    P(clip, "prefab").objectReferenceValue = guard ? pink : blue;
                    P(clip, "movementOverride").objectReferenceValue = guard ? sweep : hold;
                    P(clip, "attackOverride").objectReferenceValue = guard ? fan : (top ? down : up);
                    P(clip, "bulletMovementOverride").objectReferenceValue = null;
                }
            Apply(so);
        }

        static void RoundPhaseSpawnTimes()
        {
            int changed = 0;
            for (int number = 1; number <= 8; number++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<BattleTimelineBuilderAsset>(Data + $"Way/3_Phase{number}.asset");
                if (asset == null) continue;
                using var so = new SerializedObject(asset);
                var tracks = so.FindProperty("enemySpawnTracks");
                for (int trackIndex = 0; trackIndex < tracks.arraySize; trackIndex++)
                {
                    var clips = P(tracks.GetArrayElementAtIndex(trackIndex), "clips");
                    for (int clipIndex = 0; clipIndex < clips.arraySize; clipIndex++)
                    {
                        var time = P(clips.GetArrayElementAtIndex(clipIndex), "time");
                        double rounded = Math.Round(time.doubleValue, 1, MidpointRounding.AwayFromZero);
                        if (time.doubleValue == rounded) continue;
                        time.doubleValue = rounded;
                        changed++;
                    }
                }
                if (so.hasModifiedProperties) Apply(so);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Stage3PhaseTimes] Rounded {changed} spawn times to one decimal place in Phase1–8.");
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
                var normal = pools.FirstOrDefault(p => p.name == "LinearBulletPool");
                var parabola = pools.FirstOrDefault(p => p.name == "ParabolaBulletPool");
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
                controllerSo.FindProperty("raisedArmsSprite").objectReferenceValue = armSprite;
                Apply(controllerSo);
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static AttackTimelineEntry BossEntry(float time, IAttackSignal signal, int source = 0,
            MovementPreset bullet = null, IDirectionProvider direction = null) => new()
        {
            time = time, signal = signal, sourceIndexProvider = new ConstantSourceIndex(source),
            directionProvider = direction ?? new FixedDirectionConfig(),
            rotationProvider = new FixedRotationConfig(), bulletMovementOverride = bullet
        };

        static AttackPreset BossAttack(string name, params AttackTimelineEntry[] entries)
        {
            var asset = Asset<AttackPreset>(Data + "Attack/" + name + ".asset");
            var so = Edit(asset);
            var phases = P(so.FindProperty("attackTimeline"), "phases");
            phases.arraySize = 1;
            var phase = phases.GetArrayElementAtIndex(0);
            P(phase, "loop").boolValue = false;
            P(phase, "nextPhaseTrigger").managedReferenceValue = null;
            var list = P(phase, "entries");
            list.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                var entry = entries[i];
                P(item, "time").floatValue = entry.time;
                P(item, "signal").managedReferenceValue = entry.signal;
                P(item, "directionProvider").managedReferenceValue = entry.directionProvider;
                P(item, "rotationProvider").managedReferenceValue = entry.rotationProvider;
                P(item, "sourceIndexProvider").managedReferenceValue = entry.sourceIndexProvider;
                P(item, "seType").intValue = (int)entry.seType;
                P(item, "bulletMovementOverride").objectReferenceValue = entry.bulletMovementOverride;
            }
            Apply(so);
            return asset;
        }

        static MovementPreset BossBullet(string name, IMovementStep movement)
        {
            var asset = Asset<MovementPreset>(Data + "Bullet/" + name + ".asset");
            var so = Edit(asset);
            so.FindProperty("steps").arraySize = 1;
            so.FindProperty("steps").GetArrayElementAtIndex(0).managedReferenceValue = movement;
            Apply(so);
            return asset;
        }

        static BattleTimelineBuilderAsset BossPhase(string name, AttackPreset attack, MovementPreset movement,
            WindDirection direction)
        {
            var asset = Asset<BattleTimelineBuilderAsset>(Data + "Boss/" + name + ".asset");
            var so = Edit(asset);
            so.FindProperty("bossAttackPreset").objectReferenceValue = attack;
            so.FindProperty("bossMovementPreset").objectReferenceValue = movement;
            var cue = so.FindProperty("bossWindCue");
            P(cue, "enabled").boolValue = true;
            P(cue, "direction").intValue = (int)direction;
            P(cue, "cueSeconds").floatValue = 0.9f;
            P(cue, "playerSpeedRatio").floatValue = 0.2f;
            P(cue, "bulletAcceleration").floatValue = 1.5f;
            P(cue, "bulletDrag").floatValue = 0.25f;
            P(cue, "cueScale").vector2Value = new Vector2(-0.04f, 0.08f);
            Apply(so);
            return asset;
        }

        static void MigrateBossPresets()
        {
            var sequence = AssetDatabase.LoadAssetAtPath<BattleSequenceAsset>(Data + "Boss/Stage3Boss.asset");
            if (sequence == null) throw new InvalidOperationException("Stage3Boss.asset is missing");
            if (sequence.SequenceGroups.Count == 1 && sequence.SequenceGroups[0].Phases.Count == 12 &&
                sequence.SequenceGroups[0].Phases[0].TimelineBuilder.BossAttackPreset != null)
            {
                ValidateBossPresets();
                Selection.activeObject = sequence;
                return; // 移行後に手で調整したプリセットを再実行で上書きしない。
            }

            var up = BossBullet("3_Boss_ParabolaUp", new VertexParabolaMovementConfig(
                new Vector2(-5.5f, 2.6f), 8f, amplitude: new Vector2(2.5f, 1.2f),
                angularSpeed: new Vector2(1.5f, 0.8f), timeOffset: 0.9f));
            var down = BossBullet("3_Boss_ParabolaDown", new VertexParabolaMovementConfig(
                new Vector2(-5.5f, -2.6f), 8f, amplitude: new Vector2(2.5f, -1.2f),
                angularSpeed: new Vector2(1.5f, 0.8f), timeOffset: 0.9f));
            var normal = BossBullet("3_Boss_NormalSpeed3", new InfiniteMovementConfig(Vector3.left * 3f));
            var move = Asset<MovementPreset>(Data + "Movement/3_Boss_Sine.asset");
            var moveSo = Edit(move);
            moveSo.FindProperty("steps").arraySize = 1;
            moveSo.FindProperty("steps").GetArrayElementAtIndex(0).managedReferenceValue = new AnchoredSineMovementConfig();
            Apply(moveSo);

            var pair = BossAttack("3_Boss_ParabolaPair",
                BossEntry(0f, new SingleBulletSignal(), 1, down),
                BossEntry(0f, new SingleBulletSignal(), 1, up));
            var fan5 = BossAttack("3_Boss_Fan5", BossEntry(0f, new NWaySignal(5, 20f), bullet: normal,
                direction: new AimDirectionConfig()));
            var fan7 = BossAttack("3_Boss_Fan7", BossEntry(0f, new NWaySignal(7, 100f / 6f), bullet: normal,
                direction: new AimDirectionConfig()));
            var fan3 = BossAttack("3_Boss_Fan3", BossEntry(0f, new NWaySignal(3, 30f), bullet: normal));
            var edgeRight = BossAttack("3_Boss_EdgeRight", BossEntry(0f, new NWaySignal(3, 12f), bullet: normal,
                direction: new ScreenEdgeDirectionConfig(false)));
            var edgeLeft = BossAttack("3_Boss_EdgeLeft", BossEntry(0f, new NWaySignal(3, 12f), bullet: normal,
                direction: new ScreenEdgeDirectionConfig(true)));

            AttackTimelineEntry Repeat(AttackPreset part, int count, float interval)
                => BossEntry(0.9f, new PresetAttackSignal(part, count, interval));
            var parabola = BossAttack("3_Boss_Parabola", Repeat(pair, 21, 0.3f));
            var nway = BossAttack("3_Boss_NWay", Repeat(fan5, 21, 0.3f));
            var parabolaStrong = BossAttack("3_Boss_ParabolaStrong", Repeat(pair, 31, 0.2f),
                Repeat(fan3, 11, 0.6f), Repeat(edgeRight, 7, 1f));
            var nwayStrong = BossAttack("3_Boss_NWayStrong", Repeat(fan7, 31, 0.2f), Repeat(edgeRight, 7, 1f));
            var nwayStrongRightWind = BossAttack("3_Boss_NWayStrongRightWind", Repeat(fan7, 31, 0.2f),
                Repeat(edgeLeft, 7, 1f));

            string oldPath = Data + "Boss/3_Boss_WindCycle.asset";
            string firstPath = Data + "Boss/PhaseParabolaUp.asset";
            if (AssetDatabase.LoadAssetAtPath<BattleTimelineBuilderAsset>(firstPath) == null &&
                AssetDatabase.LoadAssetAtPath<BattleTimelineBuilderAsset>(oldPath) != null)
            {
                var error = AssetDatabase.MoveAsset(oldPath, firstPath);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            }
            var normalBuilders = new[]
            {
                BossPhase("PhaseParabolaUp", parabola, move, WindDirection.Up),
                BossPhase("PhaseNWayDown", nway, move, WindDirection.Down),
                BossPhase("PhaseParabolaRight", parabola, move, WindDirection.Right),
                BossPhase("PhaseNWayUp", nway, move, WindDirection.Up),
                BossPhase("PhaseParabolaDown", parabola, move, WindDirection.Down),
                BossPhase("PhaseNWayRight", nway, move, WindDirection.Right)
            };
            var strongBuilders = new[]
            {
                BossPhase("PhaseParabolaUpStrong", parabolaStrong, move, WindDirection.Up),
                BossPhase("PhaseNWayLeftStrong", nwayStrong, move, WindDirection.Left),
                BossPhase("PhaseParabolaDownStrong", parabolaStrong, move, WindDirection.Down),
                BossPhase("PhaseNWayRightStrong", nwayStrongRightWind, move, WindDirection.Right)
            };
            var so = Edit(sequence);
            var groups = so.FindProperty("sequenceGroups");
            groups.arraySize = 1;
            var group = groups.GetArrayElementAtIndex(0);
            P(group, "loop").boolValue = true;
            P(group, "loopCount").intValue = 0;
            P(group, "randomPick").boolValue = false;
            var phases = P(group, "phases");
            // 通常は6行動、強化は4行動で一巡。12個で両方の位相を維持する。
            phases.arraySize = 12;
            for (int i = 0; i < phases.arraySize; i++)
            {
                var phase = phases.GetArrayElementAtIndex(i);
                P(phase, "phaseId").stringValue = $"{i + 1:00}_{normalBuilders[i % 6].name}";
                P(phase, "timelineBuilder").objectReferenceValue = normalBuilders[i % 6];
                P(phase, "timelineBuilderStrong").objectReferenceValue = strongBuilders[i % 4];
                P(phase, "exitConditionConfig").managedReferenceValue = new TimeLimitExitConditionConfig(7f);
                P(phase, "overrideWind").boolValue = false;
            }
            Apply(so);
            // 既存PrefabのHP・サイズ・画像・Poolを保持して、廃止フィールドだけ取り除く。
            string prefabPath = AssetDatabase.GetAssetPath(sequence.BossPrefab);
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try { PrefabUtility.SaveAsPrefabAsset(root, prefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            ValidateBossPresets();
            Selection.activeObject = sequence;
        }

        static void UpdateWindParallel()
        {
            const string imagePath = "Assets/Project/Textures/Enemies/Stage3/Bullet_wind_purple.png";
            AssetDatabase.ImportAsset(imagePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            // 原画は緑弾と同じ絵柄の5倍解像度。表示される輪郭の大きさを揃える。
            importer.spritePixelsPerUnit = 500f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
            string bulletPath = Prefabs + "3_Bullet_WindPurple.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(bulletPath) == null &&
                !AssetDatabase.CopyAsset(Prefabs + "3_Bullet_ParabolaGreen.prefab", bulletPath))
                throw new InvalidOperationException("Cannot copy bullet template");
            var bulletRoot = PrefabUtility.LoadPrefabContents(bulletPath);
            try
            {
                bulletRoot.name = "3_Bullet_WindPurple";
                bulletRoot.GetComponent<SpriteRenderer>().sprite = sprite;
                var collider = bulletRoot.GetComponent<BoxCollider2D>();
                collider.size = new Vector2(0.83f, 0.83f);
                collider.offset = new Vector2(0f, 0.08f);
                var bulletSo = Edit(bulletRoot.GetComponent<BulletEntityPresenter>());
                var steps = bulletSo.FindProperty("movementSteps");
                steps.arraySize = 1;
                steps.GetArrayElementAtIndex(0).managedReferenceValue = new InfiniteMovementConfig(Vector3.left * 1.5f);
                Apply(bulletSo);
                PrefabUtility.SaveAsPrefabAsset(bulletRoot, bulletPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(bulletRoot); }
            var bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bulletPath).GetComponent<BulletEntityPresenter>();
            string bossPath = Prefabs + "3_Tatsumi.prefab";
            var bossRoot = PrefabUtility.LoadPrefabContents(bossPath);
            try
            {
                var pool = bossRoot.GetComponentsInChildren<BulletPool>(true).FirstOrDefault(p => p.name == "WindBulletPool");
                if (pool == null)
                {
                    var template = bossRoot.GetComponentsInChildren<BulletPool>(true).First(p => p.name == "LinearBulletPool");
                    pool = Instantiate(template.gameObject, bossRoot.transform).GetComponent<BulletPool>();
                    pool.name = "WindBulletPool";
                }
                var poolSo = Edit(pool);
                poolSo.FindProperty("bulletPrefab").objectReferenceValue = bulletPrefab;
                Apply(poolSo);
                var bossSo = Edit(bossRoot.GetComponent<BossEntityPresenter>());
                var pools = bossSo.FindProperty("bulletPools");
                if (pools.arraySize < 3) pools.arraySize = 3;
                pools.GetArrayElementAtIndex(2).objectReferenceValue = pool;
                Apply(bossSo);
                PrefabUtility.SaveAsPrefabAsset(bossRoot, bossPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(bossRoot); }

            MoveAttackIfNeeded("3_Boss_EdgeRight", "3_Boss_WindParallel");
            MoveAttackIfNeeded("3_Boss_EdgeLeft", "3_Boss_WindParallelRightWind");
            var movement = BossBullet("3_Boss_WindSpeed1_5", new InfiniteMovementConfig(Vector3.left * 1.5f));
            var parallel = BossAttack("3_Boss_WindParallel", new[] { -2f, 0f, 2f }
                .Select(offset => BossEntry(0f, new SingleBulletSignal(), 2, movement,
                    new ScreenEdgeDirectionConfig(offset, -0.15f, 1.4f))).ToArray());
            // 右風用の既存参照も、同じ並行弾設定を辿るようにする。
            BossAttack("3_Boss_WindParallelRightWind", BossEntry(0f, new PresetAttackSignal(parallel, 1, 1f)));
            AssetDatabase.SaveAssets();
            ValidateWindParallel(parallel);
            Selection.activeObject = parallel;
        }

        static void MoveAttackIfNeeded(string oldName, string newName)
        {
            string oldPath = Data + "Attack/" + oldName + ".asset", newPath = Data + "Attack/" + newName + ".asset";
            if (AssetDatabase.LoadAssetAtPath<AttackPreset>(newPath) != null ||
                AssetDatabase.LoadAssetAtPath<AttackPreset>(oldPath) == null) return;
            string error = AssetDatabase.MoveAsset(oldPath, newPath);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
        }

        static void TestWind(WindDirection direction) => BattleWind.Begin(new BattleWindSettings
        {
            enabled = true, bulletAcceleration = 1.5f, bulletDrag = 0.25f,
            intervals = new() { new WindInterval { direction = direction, seconds = 3600f } }
        });

        static void CheckParallelEvent(AttackEvent ev, float fireTime, int lane)
        {
            if (ev.SourceIndex != 2 || ev.Directions.Count != 1 || ev.SpawnSpace != AttackSpawnSpace.World)
                throw new Exception("Expected a single wind bullet in each parallel lane");
            var direction = BattleWind.Vector;
            if (Vector2.Distance(ev.Directions[0], direction) > 0.0001f) throw new Exception("Bullet is not parallel to wind");
            float wave = (Mathf.Sin(Mathf.Max(0f, fireTime - 0.9f) * 1.7f) + 1f) * 0.5f;
            float offset = (lane - 1) * 2f;
            Vector2 expected = Mathf.Abs(direction.y) > 0.5f
                ? new Vector2(Mathf.Lerp(ScreenBoundsCache.MinX + 1.4f, ScreenBoundsCache.MaxX - 1.4f, wave) + offset,
                    direction.y > 0f ? ScreenBoundsCache.MinY - 0.15f : ScreenBoundsCache.MaxY + 0.15f)
                : new Vector2(direction.x > 0f ? ScreenBoundsCache.MinX - 0.15f : ScreenBoundsCache.MaxX + 0.15f,
                    Mathf.Lerp(ScreenBoundsCache.MinY + 1.4f, ScreenBoundsCache.MaxY - 1.4f, wave) + offset);
            if (Vector2.Distance(ev.SpawnOffsets[0], expected) > 0.0001f) throw new Exception("Parallel lane position changed");
            using var so = new SerializedObject(ev.BulletMovementOverride);
            float speed = P(so.FindProperty("steps").GetArrayElementAtIndex(0), "velocity").vector3Value.magnitude;
            if (Mathf.Abs(speed - 1.5f) > 0.0001f) throw new Exception("Wind bullet speed must be 1.5");
        }

        static void ValidateWindParallel(AttackPreset preset)
        {
            try
            {
                foreach (var wind in new[] { WindDirection.Up, WindDirection.Down, WindDirection.Left, WindDirection.Right })
                {
                    TestWind(wind);
                    var timeline = preset.CreateTimeline();
                    timeline.InitializeProviders(() => Vector3.zero, () => Vector3.zero, () => Quaternion.identity);
                    timeline.Initialize();
                    int count = 0;
                    using var subscription = timeline.OnAttackTiming.Subscribe(ev => CheckParallelEvent(ev, 0f, count++));
                    try { timeline.Update(0.01f); if (count != 3) throw new Exception("Expected 3 parallel bullets"); }
                    finally { timeline.Dispose(); }
                    foreach (string name in new[] { "3_Boss_ParabolaStrong", "3_Boss_NWayStrong", "3_Boss_NWayStrongRightWind" })
                    {
                        var attack = AssetDatabase.LoadAssetAtPath<AttackPreset>(Data + "Attack/" + name + ".asset").CreateTimeline();
                        attack.InitializeProviders(() => new Vector3(-5f, 1f), () => new Vector3(5.7f, 0f), () => Quaternion.identity);
                        attack.Initialize();
                        int windCount = 0;
                        float clock = 0f;
                        using var listener = attack.OnAttackTiming.Subscribe(ev =>
                        {
                            if (ev.SourceIndex != 2) return;
                            float fireTime = 0.9f + windCount / 3;
                            if (Mathf.Abs(clock - fireTime) > 0.011f) throw new Exception("Wind volley timing changed");
                            CheckParallelEvent(ev, fireTime, windCount % 3);
                            windCount++;
                        });
                        try
                        {
                            for (int i = 0; i < 700; i++) { clock += 0.01f; attack.Update(0.01f); }
                            if (windCount != 21) throw new Exception("Expected 7 volleys of 3 wind bullets: " + name);
                        }
                        finally { attack.Dispose(); }
                    }
                }
                Debug.Log("[Stage3WindParallel] All 4 directions and 3 strong presets passed: 7 volleys of 3 parallel bullets, outside origin, spacing 2.0 and initial speed 1.5.");
            }
            finally { BattleWind.Reset(); }
        }

        static void ValidateBossPresets()
        {
            try { ValidateBossPresetsCore(); }
            finally { BattleWind.Reset(); }
        }

        static void ValidateBossPresetsCore()
        {
            var sequence = AssetDatabase.LoadAssetAtPath<BattleSequenceAsset>(Data + "Boss/Stage3Boss.asset");
            var phases = sequence.SequenceGroups[0].Phases;
            if (phases.Count != 12) throw new Exception("Expected 12 phase slots");
            var normalWind = new[] { WindDirection.Up, WindDirection.Down, WindDirection.Right };
            var strongWind = new[] { WindDirection.Up, WindDirection.Left, WindDirection.Down, WindDirection.Right };
            bool parallelEdges = AssetDatabase.LoadAssetAtPath<AttackPreset>(Data + "Attack/3_Boss_WindParallel.asset") != null;
            for (int i = 0; i < phases.Count; i++)
            {
                foreach (bool strong in new[] { false, true })
                {
                    var builder = strong ? phases[i].TimelineBuilderStrong : phases[i].TimelineBuilder;
                    if (builder.BossAttackPreset == null || builder.BossMovementPreset == null)
                        throw new Exception("Missing attack/movement: " + builder.name);
                    var expected = strong ? strongWind[i % 4] : normalWind[i % 3];
                    if (!builder.BossWindCue.enabled || builder.BossWindCue.direction != expected)
                        throw new Exception("Wind order changed: " + builder.name);
                    var wind = builder.BossWindCue.CreateWind(WindDirection.Right);
                    if (wind.intervals[0].direction != WindDirection.Right || wind.intervals[1].direction != expected)
                        throw new Exception("Wind must continue during cue: " + builder.name);
                }
            }

            foreach (string name in new[] { "3_Boss_Parabola", "3_Boss_NWay", "3_Boss_ParabolaStrong",
                "3_Boss_NWayStrong", "3_Boss_NWayStrongRightWind" })
            {
                var preset = AssetDatabase.LoadAssetAtPath<AttackPreset>(Data + "Attack/" + name + ".asset");
                bool strong = name.Contains("Strong");
                bool parabola = name.Contains("Parabola");
                // 移行後にInspectorで調整した開き角は、その現在値を使って方向を確認する。
                var fanPart = AssetDatabase.LoadAssetAtPath<AttackPreset>(Data + "Attack/" +
                    (parabola ? "3_Boss_Fan3" : strong ? "3_Boss_Fan7" : "3_Boss_Fan5") + ".asset");
                using var fanSo = new SerializedObject(fanPart);
                var fanPhase = P(fanSo.FindProperty("attackTimeline"), "phases").GetArrayElementAtIndex(0);
                var fanSignal = P(P(fanPhase, "entries").GetArrayElementAtIndex(0), "signal");
                float authoredSpread = P(fanSignal, "spreadAngle").floatValue * (P(fanSignal, "wayCount").intValue - 1);
                if (parallelEdges) TestWind(name.EndsWith("RightWind") ? WindDirection.Right : parabola ? WindDirection.Up : WindDirection.Left);
                float interval = strong ? 0.2f : 0.3f;
                int shots = strong ? 31 : 21;
                var expected = new List<(float time, int source, int count, Vector2? vertex, bool edge)>();
                for (int shot = 0; shot < shots; shot++)
                {
                    float time = shot * interval, fireTime = 0.9f + time;
                    if (parabola)
                    {
                        var vertex = new Vector2(-5.5f + 2.5f * Mathf.Sin(time * 1.5f),
                            2.6f + 1.2f * Mathf.Sin(time * 0.8f));
                        expected.Add((fireTime, 1, 1, new Vector2(vertex.x, -vertex.y), false));
                        expected.Add((fireTime, 1, 1, vertex, false));
                        if (strong && shot % 3 == 0) expected.Add((fireTime, 0, 3, null, false));
                    }
                    else expected.Add((fireTime, 0, strong ? 7 : 5, null, false));
                    if (strong && shot % 5 == 0 && !parallelEdges) expected.Add((fireTime, 0, 3, null, true));
                }
                var timeline = preset.CreateTimeline();
                timeline.InitializeProviders(() => new Vector3(-5f, 1f), () => new Vector3(5.7f, 0f), () => Quaternion.identity);
                timeline.Initialize();
                float clock = 0f;
                int bullets = 0;
                int windBullets = 0;
                using var subscription = timeline.OnAttackTiming.Subscribe(ev =>
                {
                    if (parallelEdges && ev.SourceIndex == 2)
                    {
                        float fireTime = 0.9f + windBullets / 3;
                        if (Mathf.Abs(clock - fireTime) > 0.011f) throw new Exception("Wind volley timing changed");
                        CheckParallelEvent(ev, fireTime, windBullets % 3);
                        windBullets++;
                        bullets++;
                        return;
                    }
                    // 同時刻の補助攻撃の順番に依存せず、すべての弾を照合する。
                    int index = expected.FindIndex(item => Mathf.Abs(item.time - clock) < 0.011f &&
                        item.source == ev.SourceIndex && item.count == ev.Directions.Count &&
                        item.edge == (ev.SpawnSpace == AttackSpawnSpace.World) &&
                        (!item.vertex.HasValue || ev.BulletMovement is VertexParabolaMovementConfig movement &&
                            Vector2.Distance(item.vertex.Value, movement.VertexOffset) < 0.0001f));
                    if (index < 0) throw new Exception($"Unexpected attack: {name} at {clock}");
                    if (ev.SourceIndex == 1 && ev.BulletMovement is VertexParabolaMovementConfig vertex &&
                        Mathf.Abs(vertex.InitialSpeed - 8f) > 0.0001f) throw new Exception("Parabola speed changed");
                    var match = expected[index];
                    bool fromLeft = name.EndsWith("RightWind");
                    Vector2 aim = match.edge ? (fromLeft ? Vector2.right : Vector2.left)
                        : parabola ? Vector2.left : new Vector2(-10.7f, 1f).normalized;
                    float spread = match.edge ? 24f : authoredSpread;
                    for (int i = 0; i < ev.Directions.Count; i++)
                    {
                        float angle = ev.Directions.Count > 1
                            ? Mathf.Lerp(-spread * 0.5f, spread * 0.5f, (float)i / (ev.Directions.Count - 1)) : 0f;
                        Vector2 direction = Quaternion.Euler(0f, 0f, angle) * aim;
                        if (Vector2.Distance(ev.Directions[i], direction) > 0.0001f)
                            throw new Exception("Fan direction changed: " + name);
                        if (ev.SourceIndex == 0)
                        {
                            var bullet = ev.BulletMovementOverride;
                            var bulletSo = new SerializedObject(bullet);
                            var velocity = P(bulletSo.FindProperty("steps").GetArrayElementAtIndex(0), "velocity").vector3Value;
                            if (Mathf.Abs(velocity.magnitude - 3f) > 0.0001f) throw new Exception("Normal speed changed");
                        }
                        if (match.edge)
                        {
                            float time = match.time - 0.9f;
                            Vector2 origin = new(fromLeft ? ScreenBoundsCache.MinX + 0.02f : ScreenBoundsCache.MaxX - 0.02f,
                                Mathf.Lerp(ScreenBoundsCache.MinY + 0.6f, ScreenBoundsCache.MaxY - 0.6f,
                                    (Mathf.Sin(time * 1.7f) + 1f) * 0.5f));
                            if (Vector2.Distance(origin, ev.SpawnOffsets[i]) > 0.0001f)
                                throw new Exception("Edge origin changed");
                        }
                    }
                    expected.RemoveAt(index);
                    bullets += ev.Directions.Count;
                });
                try
                {
                    for (int step = 0; step < 700; step++) { clock += 0.01f; timeline.Update(0.01f); }
                    if (expected.Count != 0) throw new Exception($"Missing attacks: {name}: {expected.Count}");
                    if (!timeline.IsCompleted) throw new Exception("Nested attack completion missing");
                    if (parallelEdges && windBullets != (strong ? 21 : 0)) throw new Exception("Wind bullet count changed");
                }
                finally { timeline.Dispose(); }
                Debug.Log($"[Stage3BossPresets] {name}: {bullets} bullets, main attack preserved; edge attack verified (parallel={parallelEdges}).");
            }
            var movementPreset = AssetDatabase.LoadAssetAtPath<MovementPreset>(Data + "Movement/3_Boss_Sine.asset");
            var sine = (AnchoredSineMovementConfig)movementPreset.Steps[0];
            for (int step = 0; step <= 700; step++)
            {
                float time = step * 0.01f;
                float y = time < 0.9f ? Mathf.Sin(Mathf.PI * time / 0.9f) * 0.18f
                    : Mathf.Sin((time - 0.9f) * 0.65f) * 1.4f;
                if (Vector3.Distance(sine.PositionAt(time), new Vector3(5.7f, y, 0f)) > 0.0001f)
                    throw new Exception("Boss movement changed");
            }
            Debug.Log("[Stage3BossPresets] All checks passed: 12 normal/strong slots, continuous wind, shot timing/counts/vertices and movement.");
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
