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
using Project.Scripts.Extensions;
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
        [SerializeField] bool prepareWayPreview;
        [SerializeField] string wayPreviewRestoreJson;

        [MenuItem("Tools/Stage3/Wind Stage Setup")]
        static void Open()
        {
            var window = GetWindow<Stage3WindSetupWindow>("Stage3 風ステージ作成");
            window.Show();
            window.Focus();
        }

        void OnEnable()
        {
            LoadSprites();
            EditorApplication.playModeStateChanged += RestoreWayPreviewStart;
        }

        void OnDisable() => EditorApplication.playModeStateChanged -= RestoreWayPreviewStart;

        void RestoreWayPreviewStart(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            prepareWayPreview = false;
            if (string.IsNullOrEmpty(wayPreviewRestoreJson)) return;
            var runtime = FindFirstObjectByType<DebugRuntimeModelInitializer>();
            if (runtime == null) return;
            EditorJsonUtility.FromJsonOverwrite(wayPreviewRestoreJson, runtime);
            wayPreviewRestoreJson = null;
        }

        void LoadSprites()
        {
            bossSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "Tatsumi_Dot.png");
            armSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "Tatsumi_WindCue_Dot.png");
        }

        void Update()
        {
            if (prepareWayPreview && Application.isPlaying)
            {
                var previewPlayer = FindFirstObjectByType<PlayerEntityPresenter>();
                if (previewPlayer?.Model != null)
                {
                    previewPlayer.Model.SetMaxHpAndRefill(100000);
                    prepareWayPreview = false;
                }
            }
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
            {
                if (GUILayout.Button("Phase6〜7の放物弾を下辺基準に・出現Xを分散して保存", GUILayout.Height(28)))
                {
                    try { ConfigureBottomVerticesAndSpawnX(); result = "放物弾の頂点を下辺＋0.35（風補正なし）、敵の出現Xを9.4〜12.4へ分散して保存しました。"; }
                    catch (Exception ex) { result = ex.Message; Debug.LogException(ex); }
                }
                if (GUILayout.Button("Phase6〜7の3wayを2回発射にして保存", GUILayout.Height(28)))
                {
                    try { ReduceWindShotDensity(); result = "3way：1.6秒間隔で2回。保存・発射回数の確認完了。"; }
                    catch (Exception ex) { result = ex.Message; Debug.LogException(ex); }
                }
                if (GUILayout.Button("Phase5.5から風の導入を確認再生（自機HP補助）")) StartWayPreviewAt("5.5");
                if (GUILayout.Button("Phase6から確認再生（自機HP補助）")) StartWayPreviewAt("6");
                if (GUILayout.Button("統合したPhase7の配置・攻撃設定を保存", GUILayout.Height(28)))
                {
                    try { ConfigurePhase7And8(); result = "Phase7の統合設定を保存しました。"; }
                    catch (Exception ex) { result = ex.Message; Debug.LogException(ex); }
                }
                if (GUILayout.Button("Phase7から確認再生（自機HP補助）")) StartWayPreviewAt("7");
            }
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("Phase1〜7の出現秒数を小数第1位へ丸めて保存"))
                {
                    try { RoundPhaseSpawnTimes(); result = "Phase1〜7の出現秒数を小数第1位に揃えました。"; }
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
            EditorGUILayout.LabelField("Phase6〜7・本ボスの試作", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Phase6〜7、専用プリセット、Stage3Boss、3_Tatsumiを作成・更新します。\n道中：中ボス→Phase5.5で風の導入→Phase6→統合したPhase7（26体）。風は上固定。\nボス：HP1000、残り30%で強化。7秒ごとに放物弾／N-wayを交互に使用。", MessageType.Info);
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
                if (GUILayout.Button("Phase6〜7 とボスを作成・保存", GUILayout.Height(36)))
                {
                    try { Build(); result = "作成完了。参照と風の数値チェック成功。詳細はConsole。"; }
                    catch (Exception ex) { result = ex.Message; Debug.LogException(ex); }
                }
            if (!string.IsNullOrEmpty(result)) EditorGUILayout.HelpBox(result, MessageType.Info);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("Phase6から道中を確認再生"))
                {
                    StartWayPreviewAt("6");
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

        [MenuItem("Tools/Stage3/Preview Wind Introduction")]
        static void PreviewWindIntroduction()
        {
            var window = GetWindow<Stage3WindSetupWindow>("Stage3 風ステージ作成");
            window.StartWayPreviewAt("5.5");
        }

        [MenuItem("Tools/Stage3/Set Wind Introduction Sound")]
        static void SetWindIntroductionSound()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode first");
            const string path = "Assets/Project/Textures/Sounds/Se/wind_effect.wav";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (!clip) throw new InvalidOperationException("Missing wind_effect.wav");
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var reference = (AudioImporter)AssetImporter.GetAtPath("Assets/Project/Textures/Sounds/Se/EnemyShotShort.mp3");
            importer.defaultSampleSettings = reference.defaultSampleSettings;
            importer.forceToMono = reference.forceToMono;
            importer.loadInBackground = reference.loadInBackground;
            importer.SaveAndReimport();

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), settings.DefaultGroup);
            entry.address = "Assets/Project/Textures/Sounds/Se/WindEffect";
            EditorUtility.SetDirty(settings);

            var seData = AssetDatabase.LoadAssetAtPath<SeDataObject>("Assets/Project/DataStore/SeData.asset");
            var sound = seData.seData.Find(item => item.seType == SeType.WindEffect);
            if (sound == null)
            {
                sound = new SeData { seType = SeType.WindEffect };
                seData.seData.Add(sound);
            }
            sound.name = "wind_effect";
            EditorUtility.SetDirty(seData);
            var sequence = AssetDatabase.LoadAssetAtPath<BattleSequenceAsset>(Data + "Way/Stage3Way.asset");
            var intro = sequence.SequenceGroups[0].Phases.Single(phase => phase.PhaseId == "5.5");
            intro.WindOverride.startSe = SeType.WindEffect;
            EditorUtility.SetDirty(sequence);
            AssetDatabase.SaveAssets();
            try
            {
                BattleWind.Begin(intro.WindOverride);
                BattleWind.Tick(1.49f);
                if (BattleWind.Direction != WindDirection.None) throw new Exception("Wind started too early");
                BattleWind.Tick(0.02f);
                if (BattleWind.Direction != WindDirection.Up || BattleWind.StartSe != SeType.WindEffect)
                    throw new Exception("Wind sound does not match wind start");
            }
            finally { BattleWind.Reset(); }
            Selection.activeObject = sequence;
            Debug.Log($"[Stage3WindSound] Saved WindEffect SE ({clip.length:F2}s) with standard SE import settings and Addressables. Phase5.5: calm 1.5s, then Up wind + one-shot sound; normal SE volume applies, playback continues across Phase6.");
        }

        void StartWayPreviewAt(string phaseId)
        {
            var phases = AssetDatabase.LoadAssetAtPath<BattleSequenceAsset>(Data + "Way/Stage3Way.asset").SequenceGroups[0].Phases;
            int index = Enumerable.Range(0, phases.Count).First(i => phases[i].PhaseId == phaseId);
            StartWayPreview(index);
        }

        void StartWayPreview(int phaseIndex)
        {
            var initializer = FindFirstObjectByType<DebugRuntimeModelInitializer>();
            if (initializer == null) throw new InvalidOperationException("Open Battle scene first");
            // Awakeより前に開始設定を変更し、再生終了時に元へ戻す。シーンは保存しない。
            wayPreviewRestoreJson = EditorJsonUtility.ToJson(initializer);
            using (var so = new SerializedObject(initializer))
            {
                so.FindProperty("stageNumber").intValue = 3;
                so.FindProperty("situation").enumValueIndex = (int)BattleSituation.Way;
                so.FindProperty("startPhaseIndex").intValue = phaseIndex;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            prepareWayPreview = true;
            EditorApplication.isPlaying = true;
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
            UpdateWay();
            ConfigurePhase7And8();
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
            Debug.Log("[Stage3WindSetup] Phase6:16 enemies/Up, Phase7:20 enemies/Up, Phase8:24 enemies/Up. Boss HP1000/50% strong. Sprite: " + AssetDatabase.GetAssetPath(bossSprite));
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
            var asset = Asset<AttackPreset>(Data + "Attack/3_Wind_Fan3.asset", Data + "Attack/Normal_Fan3.asset");
            var so = Edit(asset);
            var phase = P(so.FindProperty("attackTimeline"), "phases").GetArrayElementAtIndex(0);
            P(phase, "loopStart").floatValue = 1.5f;
            P(phase, "loopEnd").floatValue = 5.4f;
            P(phase, "cycleDuration").floatValue = 0.8f;
            var entry = P(phase, "entries").GetArrayElementAtIndex(0);
            P(entry, "directionProvider").managedReferenceValue = new AimDirectionConfig();
            P(P(entry, "signal"), "wayCount").intValue = 3;
            P(P(entry, "signal"), "spreadAngle").floatValue = 20f;
            P(entry, "bulletMovementOverride").objectReferenceValue = BossBullet("3_Wind_NormalSpeed4", new InfiniteMovementConfig(Vector3.left * 4f));
            Apply(so);
            return asset;
        }

        static AttackPreset MakeShower(string name, int sign)
        {
            var movement = Asset<MovementPreset>(Data + "Bullet/" + name + ".asset");
            var movementSo = Edit(movement);
            var steps = movementSo.FindProperty("steps");
            steps.arraySize = 1;
            steps.GetArrayElementAtIndex(0).managedReferenceValue = new VertexParabolaMovementConfig(new Vector2(-7.5f, sign * 3.8f), 7f);
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

        static MovementPreset WindFormationMovement(string name, float entryX, float wait, Vector3 bend)
        {
            var movement = Asset<MovementPreset>(Data + "Movement/" + name + ".asset");
            using var so = Edit(movement);
            var steps = so.FindProperty("steps");
            bool hasBend = bend != Vector3.zero;
            steps.arraySize = hasBend ? 4 : 3;
            steps.GetArrayElementAtIndex(0).managedReferenceValue = new TweenMovementConfig(new Vector3(entryX, 0f), 1.5f, (int)Ease.OutSine, true);
            steps.GetArrayElementAtIndex(1).managedReferenceValue = new WaitMovementConfig();
            if (hasBend)
                steps.GetArrayElementAtIndex(2).managedReferenceValue = new TweenMovementConfig(bend, 2f, (int)Ease.InOutSine, true);
            steps.GetArrayElementAtIndex(hasBend ? 3 : 2).managedReferenceValue = new TweenMovementConfig(new Vector3(-18f, 0f), 4f, (int)Ease.InSine, true);
            Apply(so);
            so.Update();
            P(so.FindProperty("steps").GetArrayElementAtIndex(1), "duration").floatValue = wait;
            Apply(so);
            return movement;
        }

        static AttackPreset WindFormationAttack(string name, int ways, float spread, Vector2 direction,
            MovementPreset bullet, float interval, bool parabola = false)
        {
            var attack = BossAttack(name, BossEntry(0f, parabola ? (IAttackSignal)new SingleBulletSignal() : new NWaySignal(ways, spread),
                parabola ? 1 : 0, bullet));
            using var so = Edit(attack);
            var phase = P(so.FindProperty("attackTimeline"), "phases").GetArrayElementAtIndex(0);
            P(phase, "loop").boolValue = true;
            P(phase, "loopStart").floatValue = 1.6f;
            P(phase, "loopEnd").floatValue = 5.2f;
            P(phase, "cycleDuration").floatValue = interval;
            var entry = P(phase, "entries").GetArrayElementAtIndex(0);
            P(P(entry, "directionProvider"), "direction").vector2Value = direction;
            Apply(so);
            return attack;
        }

        static void ConfigurePhase7And8()
        {
            if (HasMergedPhase7()) { RevisePhaseTwoAndWindSequence(); return; }
            var advance = WindFormationMovement("3_Wind_AdvanceHold", -8f, 4.5f, Vector3.zero);
            var back = WindFormationMovement("3_Wind_BackHold", -4.4f, 4.5f, Vector3.zero);
            var rise = WindFormationMovement("3_Wind_RiseAndExit", -6.3f, 1.7f, new Vector3(-1.8f, 1.3f));
            var dip = WindFormationMovement("3_Wind_DipAndExit", -5.7f, 1.7f, new Vector3(-2.2f, -0.8f));
            var slow = BossBullet("3_Wind_NormalSpeed4", new InfiniteMovementConfig(Vector3.left * 4f));
            var fast = slow;
            var lowDown = BossBullet("3_Phase7_ParabolaDown", new VertexParabolaMovementConfig(new Vector2(-5f, -1.6f), 7f));
            var lowUp = BossBullet("3_Phase7_ParabolaUp", new VertexParabolaMovementConfig(new Vector2(-6f, 2.6f), 7f));
            var deep = BossBullet("3_Phase7_ParabolaDownDeep", new VertexParabolaMovementConfig(new Vector2(-5.5f, -3.4f), 7f));
            var shallow = BossBullet("3_Phase7_ParabolaDownShallow", new VertexParabolaMovementConfig(new Vector2(-6f, -1.8f), 7f));
            var fanLeft = WindFormationAttack("3_Phase7_FanLeft3", 3, 12f, new Vector2(-1f, -0.2f), slow, 1.1f);
            var fanUp = WindFormationAttack("3_Phase7_FanUpLeft3", 3, 18f, new Vector2(-1f, 0.25f), slow, 0.9f);
            var fanDown3 = WindFormationAttack("3_Phase7_NWayDownLeft3", 3, 15f, new Vector2(-1f, -0.8f), fast, 0.9f);
            var fanDown5 = WindFormationAttack("3_Phase7_NWayDownLeft3_Narrow", 3, 12f, new Vector2(-1f, -1f), fast, 1.2f);
            var shotLowDown = WindFormationAttack("3_Phase7_ParabolaDown", 1, 0f, Vector2.left, lowDown, 0.6f, true);
            var shotLowUp = WindFormationAttack("3_Phase7_ParabolaUp", 1, 0f, Vector2.left, lowUp, 0.7f, true);
            var shotDeep = WindFormationAttack("3_Phase7_ParabolaDownDeep", 1, 0f, Vector2.left, deep, 0.6f, true);
            var shotShallow = WindFormationAttack("3_Phase7_ParabolaDownShallow", 1, 0f, Vector2.left, shallow, 0.7f, true);
            var blue = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "3_Enemy_skyblue.prefab");
            var pink = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "3_Enemy_pink.prefab");
            var movements = new[] { advance, back, rise, dip };
            for (int number = 7; number <= 8; number++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<BattleTimelineBuilderAsset>(Data + $"Way/3_Phase{number}.asset");
                using var so = Edit(asset);
                var tracks = so.FindProperty("enemySpawnTracks");
                tracks.arraySize = 1;
                var track = tracks.GetArrayElementAtIndex(0);
                // BattleTimelineBindingMapはこの名前でEnemySpawnReceiverへ接続する。
                P(track, "trackName").stringValue = "EnemySpawn";
                var clips = P(track, "clips");
                int slots = number == 7 ? 5 : 6;
                clips.arraySize = 4 * slots;
                float[] lowY = { -3.4f, -2.3f, -1.1f, -2.9f, 2f };
                float[,] scatterY = { { 3.3f, 1.6f, -0.5f, -2.4f, 2.6f, -1.5f },
                    { -2.8f, 2.8f, 0.5f, -1.2f, 3.1f, -0.2f },
                    { 1.2f, -0.8f, 3.4f, -2.5f, -1.6f, 2.3f },
                    { -1.8f, 3f, -0.1f, 1.5f, -2.9f, 0.7f } };
                for (int wave = 0; wave < 4; wave++)
                    for (int slot = 0; slot < slots; slot++)
                    {
                        bool parabola = number == 7 ? slot == 2 || slot == 3 : slot % 2 == 1;
                        AttackPreset attack = number == 7
                            ? new[] { fanLeft, fanUp, shotLowDown, shotLowUp, fanDown3 }[slot]
                            : slot % 2 == 0 ? (slot == 2 ? fanDown5 : fanDown3) : (slot == 3 ? shotShallow : shotDeep);
                        var clip = clips.GetArrayElementAtIndex(wave * slots + slot);
                        P(clip, "time").doubleValue = Math.Round(wave * (number == 7 ? 3.6d : 3.4d) + slot * 0.4d, 1);
                        float y = number == 7 ? lowY[slot] + (wave % 2 == 0 ? 0f : 0.2f) : scatterY[wave, slot];
                        P(clip, "spawnPosition").vector3Value = new Vector3(10f + 0.3f * ((slot + wave) % 3), y, 0f);
                        P(clip, "prefab").objectReferenceValue = parabola ? blue : pink;
                        // 下側の放物弾は下へ退場させず、風による折り返しを画面内で見せる。
                        int movementIndex = number == 7 ? new[] { 0, 1, 1, 2, 3 }[slot]
                            : parabola && y < 0f ? 1 : (slot + wave) % 4;
                        P(clip, "movementOverride").objectReferenceValue = movements[movementIndex];
                        P(clip, "attackOverride").objectReferenceValue = attack;
                        P(clip, "bulletMovementOverride").objectReferenceValue = null;
                    }
                Apply(so);
            }
            var sequence = AssetDatabase.LoadAssetAtPath<BattleSequenceAsset>(Data + "Way/Stage3Way.asset");
            using (var so = Edit(sequence))
            {
                var phases = P(so.FindProperty("sequenceGroups").GetArrayElementAtIndex(0), "phases");
                for (int index = 6; index <= 7; index++)
                {
                    var phase = phases.GetArrayElementAtIndex(index);
                    P(phase, "overrideWind").boolValue = true;
                    var wind = P(phase, "wind");
                    P(wind, "enabled").boolValue = true;
                    var intervals = P(wind, "intervals");
                    intervals.arraySize = 1;
                    P(intervals.GetArrayElementAtIndex(0), "direction").enumValueIndex = (int)WindDirection.Up;
                    P(intervals.GetArrayElementAtIndex(0), "seconds").floatValue = 3600f;
                }
                Apply(so);
            }
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<BattleTimelineBuilderAsset>(Data + "Way/3_Phase7.asset");
            Debug.Log("[Stage3WindFormations] Saved Phase7: 20 enemies (16 lower), Phase8: 24 scattered enemies; both Up wind. New Attack/Movement/Bullet presets only; Phase1–6 and boss unchanged.");
        }

        static bool HasMergedPhase7() =>
            AssetDatabase.LoadAssetAtPath<BattleTimelineBuilderAsset>(Data + "Way/3_Phase7.asset")?.TotalEnemySpawnCount == 27;

        [MenuItem("Tools/Stage3/Organize Merged Phase Seven")]
        static void OrganizeMergedPhaseSeven()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode first");
            if (!HasMergedPhase7()) throw new InvalidOperationException("Merge Phase7 and Phase8 first");
            var sequence = AssetDatabase.LoadAssetAtPath<BattleSequenceAsset>(Data + "Way/Stage3Way.asset");
            if (sequence.SequenceGroups.SelectMany(g => g.Phases).Any(p => p.PhaseId == "8"))
                throw new InvalidOperationException("Phase8 is still in the active sequence");
            foreach (string folder in new[] { "Attack", "Bullet" })
                foreach (string suffix in new[] { "NWayDownLeft3", "NWayDownLeft3_Narrow", "ParabolaDownDeep", "ParabolaDownShallow" })
                {
                    string oldPath = Data + folder + "/3_Phase8_" + suffix + ".asset";
                    if (AssetDatabase.LoadMainAssetAtPath(oldPath) == null) continue;
                    string error = AssetDatabase.MoveAsset(oldPath, Data + folder + "/3_Phase7_" + suffix + ".asset");
                    if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                }
            const string archive = "Assets/Project/DeprecatedResources/Stage3BeforeMerge";
            if (!AssetDatabase.IsValidFolder(archive))
                AssetDatabase.CreateFolder("Assets/Project/DeprecatedResources", "Stage3BeforeMerge");
            foreach (var item in new[] { ("Way/3_Phase8.asset", "Phase8_Obsolete.asset"),
                         ("Attack/3_Phase8_WindParallel.asset", "Phase8_WindParallel_Obsolete.asset") })
            {
                if (AssetDatabase.LoadMainAssetAtPath(Data + item.Item1) == null) continue;
                string error = AssetDatabase.MoveAsset(Data + item.Item1, archive + "/" + item.Item2);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            }
            AssetDatabase.SaveAssets();
            Selection.activeObject = sequence;
            Debug.Log("[Stage3MergedPhase7] Active progression ends at Phase7. Renamed merged attack/bullet presets from Phase8 to Phase7 with GUIDs preserved; moved unused Phase8 wave and leaf attack to DeprecatedResources/Stage3BeforeMerge. No active Phase8 remains in DataStore/Stage3.");
        }

        [MenuItem("Tools/Stage3/Set Boss Parabola Vertices")]
        static void SetBossParabolaVertices()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode first");
            foreach (var item in new[] { ("3_Boss_ParabolaUp", 2.6f), ("3_Boss_ParabolaDown", -2.6f) })
            {
                var movement = AssetDatabase.LoadAssetAtPath<MovementPreset>(Data + "Bullet/" + item.Item1 + ".asset");
                using var so = Edit(movement);
                var step = so.FindProperty("steps").GetArrayElementAtIndex(0);
                if (step.managedReferenceValue is not VertexParabolaMovementConfig)
                    throw new InvalidOperationException("Expected boss parabola movement: " + item.Item1);
                Vector2 vertex = P(step, "vertex").vector2Value;
                vertex.y = item.Item2;
                P(step, "vertex").vector2Value = vertex;
                Apply(so);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Stage3BossParabolaTune] Set normal/strong boss parabola base relative vertex Y +2.6/-2.6. Speed, X, vertex oscillation, wind and shot counts preserved.");
        }

        [MenuItem("Tools/Stage3/Set Early Way Aimed Speed Four")]
        static void SetEarlyWayAimedSpeedFour()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode first");
            foreach (var pair in new[] { ("3_Phase2_AimedSpeed5", "3_AimedSpeed4"), ("3_AimedSpeed2", "3_AimedSpeed4") })
            {
                string oldPath = Data + "Bullet/" + pair.Item1 + ".asset";
                string newPath = Data + "Bullet/" + pair.Item2 + ".asset";
                if (AssetDatabase.LoadAssetAtPath<MovementPreset>(oldPath) == null) continue;
                if (AssetDatabase.LoadAssetAtPath<MovementPreset>(newPath) != null)
                    throw new InvalidOperationException("Both old and new aimed presets exist; review references first");
                string error = AssetDatabase.MoveAsset(oldPath, newPath);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            }
            string[] names = { "3_AimedSpeed4" };
            foreach (string name in names)
            {
                var movement = AssetDatabase.LoadAssetAtPath<MovementPreset>(Data + "Bullet/" + name + ".asset");
                if (movement == null) throw new InvalidOperationException("Missing way bullet preset: " + name);
                using var so = Edit(movement);
                var steps = so.FindProperty("steps");
                if (steps.arraySize != 1) throw new InvalidOperationException("Expected one bullet movement step: " + name);
                var step = steps.GetArrayElementAtIndex(0);
                if (step.managedReferenceValue is InfiniteMovementConfig)
                {
                    Vector3 velocity = P(step, "velocity").vector3Value;
                    if (velocity == Vector3.zero) throw new InvalidOperationException("Do not change wind-only bullet speed");
                    P(step, "velocity").vector3Value = velocity.normalized * 4f;
                }
                else throw new InvalidOperationException("Unexpected bullet movement: " + name);
                Apply(so);
            }
            foreach (string name in new[] { "3_MidBoss_blue", "3_MidBoss_red" })
            {
                string path = Prefabs + name + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    using var so = Edit(root.GetComponent<TwinParabolaMidBossController>());
                    so.FindProperty("aimedShotSpeed").floatValue = 4f;
                    Apply(so);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            foreach (string name in names)
            {
                using var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<MovementPreset>(Data + "Bullet/" + name + ".asset"));
                var step = so.FindProperty("steps").GetArrayElementAtIndex(0);
                float speed = P(step, "velocity").vector3Value.magnitude;
                float expected = 4f;
                if (Mathf.Abs(speed - expected) > 0.0001f) throw new Exception("Incorrect way bullet speed: " + name);
            }
            foreach (string name in new[] { "3_MidBoss_blue", "3_MidBoss_red" })
            {
                using var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + ".prefab").GetComponent<TwinParabolaMidBossController>());
                if (so.FindProperty("aimedShotSpeed").floatValue != 4f)
                    throw new Exception("Incorrect midboss bullet speed: " + name);
            }
            Debug.Log("[Stage3WaySpeeds] Saved and verified Phase1–5 aimed initial speed 4: Phase2 pink enemies and both Phase5 midbosses. Renamed aimed movement preset with GUID preserved. Parabolas, later phases and main boss unchanged.");
        }

        [MenuItem("Tools/Stage3/Revise Phase Two And Wind Sequence")]
        static void RevisePhaseTwoAndWindSequence()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode first");
            if (Camera.main == null) throw new InvalidOperationException("Open Battle scene first");
            ScreenBoundsCache.Initialize(Camera.main);
            var aimedMovement = BossBullet("3_AimedSpeed4", new InfiniteMovementConfig(Vector3.left * 4f));
            var aimed = AssetDatabase.LoadAssetAtPath<AttackPreset>(Data + "Attack/3_Phase2.asset");
            using (var so = Edit(aimed))
            {
                var entry = P(P(so.FindProperty("attackTimeline"), "phases").GetArrayElementAtIndex(0), "entries").GetArrayElementAtIndex(0);
                P(entry, "bulletMovementOverride").objectReferenceValue = aimedMovement;
                Apply(so);
            }
            var lowerParabola = Asset<AttackPreset>(Data + "Attack/3_Phase2_LowerParabola.asset", Data + "Attack/3_Phase1.asset");
            using (var so = Edit(lowerParabola))
            {
                var entry = P(P(so.FindProperty("attackTimeline"), "phases").GetArrayElementAtIndex(0), "entries").GetArrayElementAtIndex(0);
                P(entry, "bulletMovementOverride").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MovementPreset>(Data + "Bullet/ParabolaUp.asset");
                Apply(so);
            }
            var blue = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "3_Enemy_skyblue.prefab");
            var pink = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "3_Enemy_pink.prefab");
            var phase2 = AssetDatabase.LoadAssetAtPath<BattleTimelineBuilderAsset>(Data + "Way/3_Phase2.asset");
            using (var so = Edit(phase2))
            {
                var clips = P(so.FindProperty("enemySpawnTracks").GetArrayElementAtIndex(0), "clips");
                if (clips.arraySize != 8 && clips.arraySize != 12) throw new Exception("Unexpected Phase2 formation; review before replacing it");
                clips.arraySize = 12;
                var movement = AssetDatabase.LoadAssetAtPath<MovementPreset>(Data + "Movement/3_Phase1.asset");
                for (int wave = 0; wave < 2; wave++)
                    for (int slot = 0; slot < 2; slot++)
                        SetRevisionSpawn(clips.GetArrayElementAtIndex(8 + wave * 2 + slot),
                            (wave == 0 ? 2d : 8.5d) + slot * 0.5d,
                            new Vector3(10f, slot == 0 ? -2.7f - wave * 0.2f : -3.5f, 0f),
                            slot == 0 ? blue : pink, movement, slot == 0 ? lowerParabola : aimed);
                Apply(so);
            }

            var breakBuilder = Asset<BattleTimelineBuilderAsset>(Data + "Way/3_Phase5_5.asset");
            var phase7 = AssetDatabase.LoadAssetAtPath<BattleTimelineBuilderAsset>(Data + "Way/3_Phase7.asset");
            var phase8 = AssetDatabase.LoadAssetAtPath<BattleTimelineBuilderAsset>(Data + "Way/3_Phase8.asset");
            // 設定の再適用で統合済みデータを再び倍増させない。
            if (phase7.TotalEnemySpawnCount != 27)
            {
                if (phase7.TotalEnemySpawnCount != 21 || phase8.TotalEnemySpawnCount != 25)
                    throw new Exception("Unexpected Phase7/8 formations; review before merging them");
                var sources = new List<(double time, Vector3 position, GameObject prefab, MovementPreset movement, AttackPreset attack)>();
                using var source7 = new SerializedObject(phase7);
                using var source8 = new SerializedObject(phase8);
                var clips7 = P(source7.FindProperty("enemySpawnTracks").GetArrayElementAtIndex(0), "clips");
                var clips8 = P(source8.FindProperty("enemySpawnTracks").GetArrayElementAtIndex(0), "clips");
                void AddSource(SerializedProperty clip)
                {
                    sources.Add((Math.Round(sources.Count * 16.1d / 25d, 1), P(clip, "spawnPosition").vector3Value,
                        (GameObject)P(clip, "prefab").objectReferenceValue, (MovementPreset)P(clip, "movementOverride").objectReferenceValue,
                        (AttackPreset)P(clip, "attackOverride").objectReferenceValue));
                }
                int[][] accents = { new[] { 1 }, new[] { 2, 3 }, new[] { 7 }, new[] { 8, 9 } };
                for (int wave = 0; wave < 4; wave++)
                {
                    for (int slot = 0; slot < 5; slot++) AddSource(clips7.GetArrayElementAtIndex(wave * 5 + slot));
                    foreach (int index in accents[wave]) AddSource(clips8.GetArrayElementAtIndex(index));
                }
                using var target = Edit(phase7);
                var clips = P(target.FindProperty("enemySpawnTracks").GetArrayElementAtIndex(0), "clips");
                clips.arraySize = 27;
                for (int i = 0; i < sources.Count; i++)
                {
                    var source = sources[i];
                    SetRevisionSpawn(clips.GetArrayElementAtIndex(i), source.time, source.position, source.prefab, source.movement, source.attack);
                }
                SetRevisionSpawn(clips.GetArrayElementAtIndex(26), 0d, new Vector3(20f, 0f),
                    AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "3_WindParallelEmitter.prefab"), null,
                    AssetDatabase.LoadAssetAtPath<AttackPreset>(Data + "Attack/3_Phase7_WindParallel.asset"));
                Apply(target);
            }
            // 発射元の寿命15秒は維持し、最後のセットだけ遅れて出すために発射元を1.5秒遅らせる。
            using (var so = Edit(phase7))
            {
                var clips = P(so.FindProperty("enemySpawnTracks").GetArrayElementAtIndex(0), "clips");
                P(clips.GetArrayElementAtIndex(26), "time").doubleValue = 1.5d;
                Apply(so);
            }
            var parallel = AssetDatabase.LoadAssetAtPath<AttackPreset>(Data + "Attack/3_Phase7_WindParallel.asset");
            using (var so = Edit(parallel))
            {
                var entries = P(P(so.FindProperty("attackTimeline"), "phases").GetArrayElementAtIndex(0), "entries");
                entries.arraySize = 12;
                float[] times = { 1.5f, 5.8f, 10.1f, 14.4f }; // フェーズ内では3.0/7.3/11.6/15.9秒。
                float[] centers = { -5.5f, 0f, 5.5f, -2f };
                var movement = AssetDatabase.LoadAssetAtPath<MovementPreset>(Data + "Bullet/3_Wind_ParallelWindOnly.asset");
                for (int i = 0; i < entries.arraySize; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    P(entry, "time").floatValue = times[i / 3];
                    P(entry, "signal").managedReferenceValue = new NWaySignal(1, 0f);
                    P(entry, "sourceIndexProvider").managedReferenceValue = new ConstantSourceIndex(0);
                    P(entry, "rotationProvider").managedReferenceValue = new FixedRotationConfig();
                    P(entry, "directionProvider").managedReferenceValue = new ScreenEdgeDirectionConfig(centers[i / 3] + (i % 3 - 1) * 2f, 0f, 2.4f);
                    P(P(entry, "directionProvider"), "angularSpeed").floatValue = 0f;
                    P(entry, "bulletMovementOverride").objectReferenceValue = movement;
                }
                Apply(so);
            }
            var sequence = AssetDatabase.LoadAssetAtPath<BattleSequenceAsset>(Data + "Way/Stage3Way.asset");
            using (var so = Edit(sequence))
            {
                var phases = P(so.FindProperty("sequenceGroups").GetArrayElementAtIndex(0), "phases");
                // 先頭1〜5を保持し、後半を5.5→6→7にする。Phase8のアセットは参照を外すだけ。
                phases.arraySize = 8;
                string[] ids = { "5.5", "6", "7" };
                for (int i = 0; i < 3; i++)
                {
                    var phase = phases.GetArrayElementAtIndex(5 + i);
                    P(phase, "phaseId").stringValue = ids[i];
                    P(phase, "timelineBuilder").objectReferenceValue = i == 0 ? breakBuilder :
                        AssetDatabase.LoadAssetAtPath<BattleTimelineBuilderAsset>(Data + $"Way/3_Phase{ids[i]}.asset");
                    P(phase, "timelineBuilderStrong").objectReferenceValue = null;
                    P(phase, "exitConditionConfig").managedReferenceValue = i == 0
                        ? (IExitConditionConfig)new TimeLimitExitConditionConfig(4f) : new AllEnemiesDefeatedExitConditionConfig();
                    P(phase, "overrideWind").boolValue = true;
                    var wind = P(phase, "wind");
                    Wind(wind, WindDirection.Up);
                    if (i == 0)
                    {
                        var intervals = P(wind, "intervals");
                        intervals.InsertArrayElementAtIndex(0);
                        P(intervals.GetArrayElementAtIndex(0), "direction").enumValueIndex = (int)WindDirection.None;
                        P(intervals.GetArrayElementAtIndex(0), "seconds").floatValue = 1.5f;
                        // 上風は次のPhase6まで継続。4秒で退出するため、このフェーズ内の有風時間は2.5秒。
                        P(intervals.GetArrayElementAtIndex(1), "seconds").floatValue = 3600f;
                    }
                }
                Apply(so);
            }
            AssetDatabase.SaveAssets();
            ValidateRevisedWay(sequence, phase2, phase7, breakBuilder);
            Selection.activeObject = sequence;
            Debug.Log("[Stage3WayRevision] Saved Phase2: aimed speed 4, two lower parabola/aimed pairs (12 enemies total). Sequence 1,2,3,4,5,5.5,6,7. Phase5.5: no enemies, calm 1.5s then Up wind 2.5s. Merged Phase7: 26 enemies over 16.1s plus 4 leaf sets at 3/7.3/11.6/15.9s; old Phase8 removed from progression. Boss settings unchanged.");
        }

        static void SetRevisionSpawn(SerializedProperty clip, double time, Vector3 position, GameObject prefab, MovementPreset movement, AttackPreset attack)
        {
            P(clip, "time").doubleValue = time;
            P(clip, "spawnPosition").vector3Value = position;
            P(clip, "prefab").objectReferenceValue = prefab;
            P(clip, "movementOverride").objectReferenceValue = movement;
            P(clip, "attackOverride").objectReferenceValue = attack;
            P(clip, "bulletMovementOverride").objectReferenceValue = null;
        }

        static void ValidateRevisedWay(BattleSequenceAsset sequence, BattleTimelineBuilderAsset phase2,
            BattleTimelineBuilderAsset phase7, BattleTimelineBuilderAsset breakBuilder)
        {
            var phases = sequence.SequenceGroups[0].Phases;
            if (string.Join(",", phases.Select(p => p.PhaseId)) != "1,2,3,4,5,5.5,6,7" ||
                phase2.TotalEnemySpawnCount != 12 || phase7.TotalEnemySpawnCount != 27 || breakBuilder.TotalEnemySpawnCount != 0 ||
                phases[4].ExitConditionConfig is not AllEnemiesDefeatedExitConditionConfig ||
                phases[5].ExitConditionConfig is not TimeLimitExitConditionConfig ||
                phases[6].WindOverride.intervals[0].direction != WindDirection.Up ||
                phases[7].WindOverride.intervals[0].direction != WindDirection.Up)
                throw new Exception("Revised stage sequence or enemy count is incorrect");
            using (var so = new SerializedObject(sequence))
            {
                var rest = P(so.FindProperty("sequenceGroups").GetArrayElementAtIndex(0), "phases").GetArrayElementAtIndex(5);
                if (Mathf.Abs(P(P(rest, "exitConditionConfig"), "timeLimitSeconds").floatValue - 4f) > 0.0001f)
                    throw new Exception("Phase5.5 must last exactly 4 seconds");
            }
            try
            {
                BattleWind.Begin(phases[5].WindOverride);
                if (BattleWind.Direction != WindDirection.None) throw new Exception("Phase5.5 must start calm");
                BattleWind.Tick(1.49f);
                if (BattleWind.Direction != WindDirection.None) throw new Exception("Wind started too early");
                BattleWind.Tick(0.02f);
                if (BattleWind.Direction != WindDirection.Up) throw new Exception("Wind did not start after 1.5s");
                BattleWind.Tick(2.49f);
                if (BattleWind.Direction != WindDirection.Up) throw new Exception("Wind must stay Up through the handoff");
            }
            finally { BattleWind.Reset(); }
            var phase7Attack = AssetDatabase.LoadAssetAtPath<AttackPreset>(Data + "Attack/3_Phase7_WindParallel.asset");
            ValidateWayWindParallel(phase7Attack, new[] { 1.5f, 5.8f, 10.1f, 14.4f }, new[] { -5.5f, 0f, 5.5f, -2f });
            // 実際のTimeline生成でも、新しい出現Clipが欠けないことを確認する。
            foreach (var builder in new[] { phase2, breakBuilder, phase7 })
            {
                var timeline = builder.BuildTimeline();
                try
                {
                    int markers = timeline.GetOutputTracks().Sum(track => track.GetMarkers().Count());
                    if (markers != builder.TotalEnemySpawnCount) throw new Exception("Spawn marker count mismatch: " + builder.name);
                }
                finally { DestroyImmediate(timeline); }
            }
            Debug.Log("[Stage3WayRevision] Verified progression, calm/Up wind boundaries and 4s break, enemy counts, runtime spawn markers and 12 wind-only leaf bullets.");
        }

        [MenuItem("Tools/Stage3/Set Bottom Vertices And Spread X")]
        static void ConfigureBottomVerticesAndSpawnX()
        {
            var camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("Open Battle scene with its Main Camera first");
            ScreenBoundsCache.Initialize(camera);
            var bullets = new HashSet<MovementPreset>();
            float[] spawnX = { 9.4f, 11.2f, 10f, 12.4f, 10.6f, 11.8f };
            for (int number = 6; number <= 7; number++)
            {
                var builder = AssetDatabase.LoadAssetAtPath<BattleTimelineBuilderAsset>(Data + $"Way/3_Phase{number}.asset");
                using var so = Edit(builder);
                var tracks = so.FindProperty("enemySpawnTracks");
                int spawnIndex = 0;
                for (int i = 0; i < tracks.arraySize; i++)
                {
                    var clips = P(tracks.GetArrayElementAtIndex(i), "clips");
                    for (int j = 0; j < clips.arraySize; j++)
                    {
                        var clip = clips.GetArrayElementAtIndex(j);
                        var position = P(clip, "spawnPosition").vector3Value;
                        position.x = spawnX[(spawnIndex++ * 5 + number) % spawnX.Length];
                        P(clip, "spawnPosition").vector3Value = position;
                        if (P(clip, "attackOverride").objectReferenceValue is not AttackPreset attack) continue;
                        using var attackSo = new SerializedObject(attack);
                        var phases = P(attackSo.FindProperty("attackTimeline"), "phases");
                        for (int p = 0; p < phases.arraySize; p++)
                        {
                            var entries = P(phases.GetArrayElementAtIndex(p), "entries");
                            for (int e = 0; e < entries.arraySize; e++)
                                if (P(entries.GetArrayElementAtIndex(e), "bulletMovementOverride").objectReferenceValue is MovementPreset bullet
                                    && bullet.Steps.Any(step => step is VertexParabolaMovementConfig)) bullets.Add(bullet);
                        }
                    }
                }
                Apply(so);
            }
            foreach (var bullet in bullets)
            {
                using var so = Edit(bullet);
                var steps = so.FindProperty("steps");
                for (int i = 0; i < steps.arraySize; i++)
                {
                    var step = steps.GetArrayElementAtIndex(i);
                    if (step.managedReferenceValue is not VertexParabolaMovementConfig) continue;
                    // 既存の相対頂点Xを、右側の基準発射位置X=5から見た絶対座標へ移す。
                    // 再実行しても頂点Xを重ねて変換しない。
                    if (P(step, "isRelative").boolValue)
                    {
                        var vertex = P(step, "vertex").vector2Value;
                        vertex.x += 5f;
                        P(step, "vertex").vector2Value = vertex;
                    }
                    P(step, "isRelative").boolValue = false;
                    var bottomVertex = P(step, "vertex").vector2Value;
                    bottomVertex.y = ScreenBoundsCache.MinY + 0.35f;
                    P(step, "vertex").vector2Value = bottomVertex;
                }
                Apply(so);
                foreach (var step in bullet.Steps.OfType<VertexParabolaMovementConfig>())
                    foreach (float height in new[] { -3.4f, -1f, 0f, 2.6f, 3.4f })
                    {
                        var start = new Vector2(6f, height);
                        var sampled = (VertexParabolaMovementConfig)step.SampleForAttackTime(5.2f);
                        var vertex = sampled.GetWorldVertex(start);
                        if (Mathf.Abs(vertex.y - (ScreenBoundsCache.MinY + 0.35f)) > 0.0001f ||
                            Mathf.Abs(vertex.x - step.VertexOffset.x) > 0.0001f ||
                            !VertexParabola.TrySolve(start, vertex, ParabolaOrientation.Vertical, step.InitialSpeed,
                                out var velocity, out var acceleration, out float time) ||
                            Vector2.Distance(VertexParabola.Position(start, velocity, acceleration, time), vertex) > 0.001f)
                            throw new Exception("Invalid bottom vertex: " + bullet.name);
                    }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Stage3BottomVertices] Saved {bullets.Count} parabola presets: absolute vertex, bottom + 0.35, no wind compensation; enemy spawn X 9.4–12.4 in Phase6–8. Verified vertices at 5 launch heights; speed unchanged.");
        }

        [MenuItem("Tools/Stage3/Spread Wind Enemies And Vertices Left")]
        static void SpreadWindEnemiesAndVerticesLeft()
        {
            foreach (var item in new[] { ("3_Wind_ShowerDown", -6f), ("3_Phase7_ParabolaDown", -5.5f),
                         ("3_Phase7_ParabolaDownDeep", -6f) })
            {
                var asset = AssetDatabase.LoadAssetAtPath<MovementPreset>(Data + "Bullet/" + item.Item1 + ".asset");
                using var so = Edit(asset);
                var step = so.FindProperty("steps").GetArrayElementAtIndex(0);
                var vertex = P(step, "vertex").vector2Value;
                vertex.x = item.Item2;
                P(step, "vertex").vector2Value = vertex;
                Apply(so);
            }
            foreach (var item in new[] { ("3_Wind_HoldAndExit", -11.8f), ("3_Wind_AdvanceHold", -12.8f) })
            {
                var asset = AssetDatabase.LoadAssetAtPath<MovementPreset>(Data + "Movement/" + item.Item1 + ".asset");
                using var so = Edit(asset);
                var step = so.FindProperty("steps").GetArrayElementAtIndex(0);
                var offset = P(step, "targetOffset").vector3Value;
                offset.x = item.Item2;
                P(step, "targetOffset").vector3Value = offset;
                Apply(so);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Stage3LeftSpread] Saved Phase6–8: left entry destinations X -3.4–0.6 mixed with existing right formations; left vertices X -5.5/-6 mixed with existing central vertices. Vertex Y, shot speed and shot counts unchanged.");
        }

        [MenuItem("Tools/Stage3/Use Three Way And Slow Left Entry")]
        static void UseThreeWayAndSlowLeftEntry()
        {
            foreach (var item in new[] { ("3_Wind_Fan5", "3_Wind_Fan3"),
                         ("3_Phase7_FanLeft5", "3_Phase7_FanLeft3"),
                         ("3_Phase8_NWayDownLeft5", "3_Phase7_NWayDownLeft3_Narrow") })
            {
                string oldPath = Data + "Attack/" + item.Item1 + ".asset";
                string newPath = Data + "Attack/" + item.Item2 + ".asset";
                var attack = AssetDatabase.LoadAssetAtPath<AttackPreset>(oldPath)
                    ?? AssetDatabase.LoadAssetAtPath<AttackPreset>(newPath);
                using (var so = Edit(attack))
                {
                    var phases = P(so.FindProperty("attackTimeline"), "phases");
                    for (int p = 0; p < phases.arraySize; p++)
                    {
                        var entries = P(phases.GetArrayElementAtIndex(p), "entries");
                        for (int e = 0; e < entries.arraySize; e++)
                        {
                            var signal = P(entries.GetArrayElementAtIndex(e), "signal");
                            if (signal.managedReferenceValue is NWaySignal) P(signal, "wayCount").intValue = 3;
                        }
                    }
                    Apply(so);
                }
                if (AssetDatabase.GetAssetPath(attack) != newPath)
                {
                    string error = AssetDatabase.MoveAsset(oldPath, newPath);
                    if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                }
            }
            foreach (var item in new[] { ("3_Wind_HoldAndExit", 2.8f), ("3_Wind_AdvanceHold", 3f) })
            {
                var movement = AssetDatabase.LoadAssetAtPath<MovementPreset>(Data + "Movement/" + item.Item1 + ".asset");
                using var so = Edit(movement);
                P(so.FindProperty("steps").GetArrayElementAtIndex(0), "duration").floatValue = item.Item2;
                Apply(so);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Stage3ThreeWaySlowEntry] Saved 3 former five-way presets as three-way, preserving GUID references; left entry durations 1.4->2.8 / 1.5->3 seconds (half speed). Other movement, shot speed and firing counts unchanged.");
        }

        [MenuItem("Tools/Stage3/Set Wind Three Way Speeds")]
        static void SetWindThreeWaySpeeds()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode first");
            string oldPath = Data + "Bullet/3_Wind_NormalSpeed2.asset";
            string newPath = Data + "Bullet/3_Wind_NormalSpeed4.asset";
            if (AssetDatabase.LoadAssetAtPath<MovementPreset>(oldPath) != null &&
                AssetDatabase.LoadAssetAtPath<MovementPreset>(newPath) == null)
            {
                string error = AssetDatabase.MoveAsset(oldPath, newPath);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            }
            var bullet = BossBullet("3_Wind_NormalSpeed4", new InfiniteMovementConfig(Vector3.left * 4f));
            int entriesUpdated = 0;
            foreach (string name in new[] { "3_Wind_Fan3", "3_Phase7_FanLeft3", "3_Phase7_FanUpLeft3",
                         "3_Phase7_NWayDownLeft3", "3_Phase7_NWayDownLeft3_Narrow" })
            {
                var attack = AssetDatabase.LoadAssetAtPath<AttackPreset>(Data + "Attack/" + name + ".asset");
                // 統合後の改名整理が未実施でも、現在参照している元Phase8のプリセットを調整する。
                if (attack == null && name.StartsWith("3_Phase7_NWay"))
                    attack = AssetDatabase.LoadAssetAtPath<AttackPreset>(Data + "Attack/" + name.Replace("3_Phase7_", "3_Phase8_") + ".asset");
                if (attack == null) throw new InvalidOperationException("Missing three-way attack: " + name);
                using var so = Edit(attack);
                var phases = P(so.FindProperty("attackTimeline"), "phases");
                for (int p = 0; p < phases.arraySize; p++)
                {
                    var entries = P(phases.GetArrayElementAtIndex(p), "entries");
                    for (int e = 0; e < entries.arraySize; e++)
                    {
                        var entry = entries.GetArrayElementAtIndex(e);
                        var signal = P(entry, "signal");
                        if (signal.managedReferenceValue is not NWaySignal || P(signal, "wayCount").intValue != 3)
                            throw new InvalidOperationException("Expected three-way attack: " + name);
                        P(entry, "bulletMovementOverride").objectReferenceValue = bullet;
                        entriesUpdated++;
                    }
                }
                Apply(so);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Stage3ThreeWaySpeed] Saved {entriesUpdated} three-way attacks: aimed and fixed initial speed 4 throughout Phase6/7. Phase2 aimed speed 4, midboss, directions, shot counts, parabola speed 7, leaf wind-only motion and main boss unchanged.");
        }

        [MenuItem("Tools/Stage3/Set Wind Three Way Two Volleys")]
        static void ReduceWindShotDensity()
        {
            var attacks = new HashSet<AttackPreset>();
            for (int number = 6; number <= 7; number++)
            {
                var builder = AssetDatabase.LoadAssetAtPath<BattleTimelineBuilderAsset>(Data + $"Way/3_Phase{number}.asset");
                using var so = new SerializedObject(builder);
                var tracks = so.FindProperty("enemySpawnTracks");
                for (int i = 0; i < tracks.arraySize; i++)
                {
                    var clips = P(tracks.GetArrayElementAtIndex(i), "clips");
                    for (int j = 0; j < clips.arraySize; j++)
                        if (P(clips.GetArrayElementAtIndex(j), "attackOverride").objectReferenceValue is AttackPreset attack)
                            attacks.Add(attack);
                }
            }
            // 画面外の並行弾など、3way以外の攻撃は変更しない。
            attacks.RemoveWhere(attack =>
            {
                using var so = new SerializedObject(attack);
                var phases = P(so.FindProperty("attackTimeline"), "phases");
                if (phases.arraySize != 1) return true;
                var entries = P(phases.GetArrayElementAtIndex(0), "entries");
                if (entries.arraySize != 1) return true;
                var signal = P(entries.GetArrayElementAtIndex(0), "signal");
                return signal.managedReferenceValue is not NWaySignal || P(signal, "wayCount").intValue != 3;
            });
            // この調整は、出現Clipから直接参照する単一攻撃の連射プリセットに限定する。
            foreach (var attack in attacks)
            {
                using var so = new SerializedObject(attack);
                var phases = P(so.FindProperty("attackTimeline"), "phases");
                if (phases.arraySize != 1) throw new InvalidOperationException("Expected one attack phase: " + attack.name);
                var phase = phases.GetArrayElementAtIndex(0);
                var entries = P(phase, "entries");
                if (!P(phase, "loop").boolValue || entries.arraySize != 1 ||
                    P(entries.GetArrayElementAtIndex(0), "time").floatValue != 0f ||
                    P(entries.GetArrayElementAtIndex(0), "signal").managedReferenceValue is PresetAttackSignal)
                    throw new InvalidOperationException("Expected one looping shot: " + attack.name);
            }
            int updated = 0;
            foreach (var attack in attacks)
            {
                using var so = Edit(attack);
                var phase = P(so.FindProperty("attackTimeline"), "phases").GetArrayElementAtIndex(0);
                var entry = P(phase, "entries").GetArrayElementAtIndex(0);
                var signal = P(entry, "signal");
                if (signal.managedReferenceValue is not NWaySignal || P(signal, "wayCount").intValue != 3) continue;
                int expected = 2;
                float interval = P(phase, "cycleDuration").floatValue;
                float start = P(phase, "loopStart").floatValue;
                float end = Mathf.Round((start + interval * (expected - 1) + 0.1f) * 10f) / 10f;
                P(phase, "cycleDuration").floatValue = interval;
                // 最終弾の直後で終える。境界の浮動小数点誤差で1回欠けないよう0.1秒余裕を置く。
                P(phase, "loopEnd").floatValue = end;
                Apply(so);
                var timeline = attack.CreateTimeline();
                timeline.InitializeProviders(() => new Vector3(-6f, 0f, 0f), () => new Vector3(4f, 0f, 0f), () => Quaternion.identity);
                timeline.Initialize();
                int actual = 0;
                using var subscription = timeline.OnAttackTiming.Subscribe(_ => actual++);
                try { timeline.Update(end + 1f); }
                finally { timeline.Dispose(); }
                if (actual != expected) throw new Exception($"{attack.name}: expected {expected} shots, got {actual}");
                updated++;
                Debug.Log($"[Stage3WindDensity] {attack.name}: {actual} volleys, interval {interval:0.0}s, first {start:0.0}s, last {start + interval * (expected - 1):0.0}s.");
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Stage3WindDensity] Saved and verified {updated} three-way presets: 2 volleys. Initial speed, intervals and parabolas unchanged.");
        }

        [MenuItem("Tools/Stage3/Add Phase Seven And Eight Wind Parallel")]
        static void AddWayWindParallel()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode first");
            if (HasMergedPhase7()) { RevisePhaseTwoAndWindSequence(); return; }
            var camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("Open Battle scene first");
            ScreenBoundsCache.Initialize(camera);
            var bullet = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "3_Bullet_WindPurple.prefab")?.GetComponent<BulletEntityPresenter>();
            var movement = WindParallelWaitingMovement();
            if (bullet == null || movement == null) throw new InvalidOperationException("Boss wind bullet assets are missing");

            // 既存の敵の攻撃機能を、非表示・接触なしの画面外発射元として使う。
            string emitterPath = Prefabs + "3_WindParallelEmitter.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(emitterPath) == null &&
                !AssetDatabase.CopyAsset(Prefabs + "3_Enemy_pink.prefab", emitterPath))
                throw new InvalidOperationException("Cannot copy emitter template");
            var root = PrefabUtility.LoadPrefabContents(emitterPath);
            try
            {
                root.name = "3_WindParallelEmitter";
                root.transform.position = new Vector3(20f, 0f, 0f);
                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = false;
                foreach (var collider in root.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
                foreach (var body in root.GetComponentsInChildren<Rigidbody2D>(true)) body.simulated = false;
                var enemy = root.GetComponent<EnemyEntityPresenter>();
                using var so = Edit(enemy);
                so.FindProperty("contactDamage").intValue = 0;
                so.FindProperty("lifetime").floatValue = 15f;
                so.FindProperty("movementPreset").objectReferenceValue = null;
                var steps = so.FindProperty("movementSteps");
                steps.arraySize = 1;
                steps.GetArrayElementAtIndex(0).managedReferenceValue = new InfiniteMovementConfig(Vector3.zero);
                so.FindProperty("attackPreset").objectReferenceValue = null;
                so.FindProperty("attackTimeline").FindPropertyRelative("phases").arraySize = 0;
                P(so.FindProperty("deathAttackEntry"), "signal").managedReferenceValue = null;
                var pool = root.GetComponentsInChildren<BulletPool>(true).First();
                using (var poolSo = Edit(pool))
                {
                    poolSo.FindProperty("bulletPrefab").objectReferenceValue = bullet;
                    Apply(poolSo);
                }
                var pools = so.FindProperty("bulletPools");
                pools.GetArrayElementAtIndex(0).objectReferenceValue = pool;
                Apply(so);
                PrefabUtility.SaveAsPrefabAsset(root, emitterPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var emitter = AssetDatabase.LoadAssetAtPath<GameObject>(emitterPath);

            for (int number = 7; number <= 8; number++)
            {
                float[] times = number == 7 ? new[] { 3f, 7f, 11f } : new[] { 2.5f, 6f, 9.5f, 13f };
                float[] centers = number == 7 ? new[] { -5.5f, 0f, 5.5f } : new[] { 5.5f, -2f, 2f, -5.5f };
                var entries = new List<AttackTimelineEntry>();
                for (int set = 0; set < times.Length; set++)
                    foreach (float lane in new[] { -2f, 0f, 2f })
                        entries.Add(BossEntry(times[set], new NWaySignal(1, 0f), 0, movement,
                            new ScreenEdgeDirectionConfig(centers[set] + lane, 0f, 2.4f)));
                var attack = BossAttack($"3_Phase{number}_WindParallel", entries.ToArray());
                using (var so = Edit(attack))
                {
                    var list = P(so.FindProperty("attackTimeline"), "phases").GetArrayElementAtIndex(0).FindPropertyRelative("entries");
                    for (int i = 0; i < list.arraySize; i++)
                    {
                        P(P(list.GetArrayElementAtIndex(i), "directionProvider"), "angularSpeed").floatValue = 0f;
                        P(P(list.GetArrayElementAtIndex(i), "signal"), "startDelay").floatValue = 0f;
                    }
                    Apply(so);
                }
                ValidateWayWindParallel(attack, times, centers);
                var builder = AssetDatabase.LoadAssetAtPath<BattleTimelineBuilderAsset>(Data + $"Way/3_Phase{number}.asset");
                using (var so = Edit(builder))
                {
                    var tracks = so.FindProperty("enemySpawnTracks");
                    var track = Enumerable.Range(0, tracks.arraySize).Select(tracks.GetArrayElementAtIndex)
                        .First(t => P(t, "trackName").stringValue == "EnemySpawn");
                    var clips = P(track, "clips");
                    int index = -1;
                    for (int i = 0; i < clips.arraySize; i++)
                        if (P(clips.GetArrayElementAtIndex(i), "prefab").objectReferenceValue == emitter) index = i;
                    if (index < 0) { index = clips.arraySize; clips.arraySize++; }
                    var clip = clips.GetArrayElementAtIndex(index);
                    P(clip, "time").doubleValue = 0d;
                    P(clip, "spawnPosition").vector3Value = new Vector3(20f, 0f, 0f);
                    P(clip, "prefab").objectReferenceValue = emitter;
                    P(clip, "attackOverride").objectReferenceValue = attack;
                    P(clip, "movementOverride").objectReferenceValue = null;
                    P(clip, "bulletMovementOverride").objectReferenceValue = null;
                    Apply(so);
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Stage3WayWindParallel] Saved Phase7: 3 sets / 9 purple bullets; Phase8: 4 sets / 12. Zero authored velocity, wind-only movement, spacing 2, edge-centered origin, Up wind. Invisible noncolliding emitter expires at 15s; existing enemies and boss unchanged.");
        }

        static void ValidateCurrentPhase7WindParallel()
        {
            var attack = AssetDatabase.LoadAssetAtPath<AttackPreset>(Data + "Attack/3_Phase7_WindParallel.asset");
            if (attack == null) return;
            using var so = new SerializedObject(attack);
            bool merged = P(P(so.FindProperty("attackTimeline"), "phases").GetArrayElementAtIndex(0), "entries").arraySize == 12;
            ValidateWayWindParallel(attack, merged ? new[] { 1.5f, 5.8f, 10.1f, 14.4f } : new[] { 3f, 7f, 11f },
                merged ? new[] { -5.5f, 0f, 5.5f, -2f } : new[] { -5.5f, 0f, 5.5f });
        }

        static void ValidateWayWindParallel(AttackPreset preset, float[] times, float[] centers)
        {
            try
            {
                TestWind(WindDirection.Up);
                var timeline = preset.CreateTimeline();
                timeline.InitializeProviders(() => Vector3.zero, () => new Vector3(20f, 0f), () => Quaternion.identity);
                timeline.Initialize();
                int count = 0;
                float clock = 0f;
                using var listener = timeline.OnAttackTiming.Subscribe(ev =>
                {
                    int set = count / 3, lane = count % 3;
                    var expected = new Vector2(centers[set] + (lane - 1) * 2f, ScreenBoundsCache.MinY);
                    if (ev.Type != AttackEventType.Bullet || ev.SourceIndex != 0 || ev.SpawnSpace != AttackSpawnSpace.World ||
                        ev.Directions.Count != 1 || Vector2.Distance(ev.Directions[0], Vector2.up) > 0.001f ||
                        Vector2.Distance(ev.SpawnOffsets[0], expected) > 0.001f || Mathf.Abs(clock - times[set]) > 0.02f ||
                        Mathf.Abs(ev.GetStartDelayAt(0)) > 0.001f ||
                        expected.x < ScreenBoundsCache.MinX || expected.x > ScreenBoundsCache.MaxX)
                        throw new Exception("Invalid wind parallel event: " + preset.name);
                    CheckParallelWaitingMovement(ev);
                    count++;
                });
                try
                {
                    int frames = Mathf.CeilToInt((times.Max() + 1f) * 100f);
                    for (int i = 1; i <= frames; i++) { clock = i * 0.01f; timeline.Update(0.01f); }
                    if (count != times.Length * 3) throw new Exception("Wrong parallel bullet count: " + preset.name);
                }
                finally { timeline.Dispose(); }
                Debug.Log($"[Stage3WayWindParallel] Verified {preset.name}: {times.Length} sets / {count} bullets, timing, spacing, edge origins and wind-only movement.");
            }
            finally { BattleWind.Reset(); }
        }

        static MovementPreset WindParallelWaitingMovement()
        {
            string path = Data + "Bullet/3_Wind_ParallelWindOnly.asset";
            string oldPath = Data + "Bullet/3_Wind_ParallelWait1_5Speed1_5.asset";
            if (AssetDatabase.LoadAssetAtPath<MovementPreset>(path) == null &&
                AssetDatabase.LoadAssetAtPath<MovementPreset>(oldPath) != null)
            {
                string error = AssetDatabase.MoveAsset(oldPath, path);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            }
            var asset = Asset<MovementPreset>(path);
            using var so = Edit(asset);
            var steps = so.FindProperty("steps");
            steps.arraySize = 1;
            steps.GetArrayElementAtIndex(0).managedReferenceValue = new InfiniteMovementConfig(Vector3.zero);
            Apply(so);
            return asset;
        }

        static void CheckParallelWaitingMovement(AttackEvent ev)
        {
            if (Mathf.Abs(ev.GetStartDelayAt(0)) > 0.0001f || ev.BulletMovementOverride == null)
                throw new Exception("Wind-only movement must use movement steps without Start Delay");
            using var so = new SerializedObject(ev.BulletMovementOverride);
            var steps = so.FindProperty("steps");
            if (steps.arraySize != 1 || steps.GetArrayElementAtIndex(0).managedReferenceValue is not InfiniteMovementConfig ||
                P(steps.GetArrayElementAtIndex(0), "velocity").vector3Value.sqrMagnitude > 0.0000001f)
                throw new Exception("Expected zero authored velocity indefinitely; wind alone moves the bullet");
        }

        [MenuItem("Tools/Stage3/Restore Boss Wind Parallel Motion")]
        static void RestoreBossWindParallelMotion()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode first");
            if (Camera.main == null) throw new InvalidOperationException("Open Battle scene first");
            ScreenBoundsCache.Initialize(Camera.main);
            var movement = AssetDatabase.LoadAssetAtPath<MovementPreset>(Data + "Bullet/3_Boss_WindSpeed1_5.asset");
            var attack = AssetDatabase.LoadAssetAtPath<AttackPreset>(Data + "Attack/3_Boss_WindParallel.asset");
            using (var so = Edit(attack))
            {
                var entries = P(so.FindProperty("attackTimeline"), "phases").GetArrayElementAtIndex(0).FindPropertyRelative("entries");
                for (int i = 0; i < entries.arraySize; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    P(entry, "signal").managedReferenceValue = new SingleBulletSignal();
                    P(entry, "bulletMovementOverride").objectReferenceValue = movement;
                    var direction = P(entry, "directionProvider");
                    P(direction, "edgeInset").floatValue = -0.15f;
                    P(direction, "verticalMargin").floatValue = 1.4f;
                }
                Apply(so);
            }
            AssetDatabase.SaveAssets();
            ValidateWindParallel(attack);
            ValidateCurrentPhase7WindParallel();
            var oldPhase8 = AssetDatabase.LoadAssetAtPath<AttackPreset>(Data + "Attack/3_Phase8_WindParallel.asset");
            if (oldPhase8 != null) ValidateWayWindParallel(oldPhase8,
                new[] { 2.5f, 6f, 9.5f, 13f }, new[] { 5.5f, -2f, 2f, -5.5f });
            Debug.Log("[Stage3BossWindRestore] Restored boss-only parallel motion: immediate initial speed 1.5 along wind, edge inset -0.15 and sweep margin 1.4. Phase7/8 remain wind-only; shared leaf visuals and collider untouched.");
        }

        [MenuItem("Tools/Stage3/Set Wind Parallel Edge Warning")]
        static void ConfigureWindParallelEdgeWarning()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode first");
            if (Camera.main == null) throw new InvalidOperationException("Open Battle scene first");
            ScreenBoundsCache.Initialize(Camera.main);
            var waitingMovement = WindParallelWaitingMovement();
            foreach (string name in new[] { "3_Phase7_WindParallel", "3_Phase8_WindParallel" })
            {
                var attack = AssetDatabase.LoadAssetAtPath<AttackPreset>(Data + "Attack/" + name + ".asset");
                if (attack == null) continue;
                using var so = Edit(attack);
                var entries = P(so.FindProperty("attackTimeline"), "phases").GetArrayElementAtIndex(0).FindPropertyRelative("entries");
                for (int i = 0; i < entries.arraySize; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    var direction = P(entry, "directionProvider");
                    if (direction.managedReferenceValue is not ScreenEdgeDirectionConfig)
                        throw new InvalidOperationException("Expected screen-edge origin: " + name);
                    // 自走速度は常に0。開始遅延を使わず、生成直後から風だけで動かす。
                    P(entry, "signal").managedReferenceValue = new NWaySignal(1, 0f);
                    P(P(entry, "signal"), "startDelay").floatValue = 0f;
                    P(entry, "bulletMovementOverride").objectReferenceValue = waitingMovement;
                    P(direction, "edgeInset").floatValue = 0f;
                    P(direction, "verticalMargin").floatValue = 2.4f;
                }
                Apply(so);
            }
            AssetDatabase.SaveAssets();
            ValidateWindParallel(AssetDatabase.LoadAssetAtPath<AttackPreset>(Data + "Attack/3_Boss_WindParallel.asset"));
            var phase7 = AssetDatabase.LoadAssetAtPath<AttackPreset>(Data + "Attack/3_Phase7_WindParallel.asset");
            var phase8 = AssetDatabase.LoadAssetAtPath<AttackPreset>(Data + "Attack/3_Phase8_WindParallel.asset");
            if (phase7 != null) ValidateCurrentPhase7WindParallel();
            if (phase8 != null) ValidateWayWindParallel(phase8, new[] { 2.5f, 6f, 9.5f, 13f }, new[] { 5.5f, -2f, 2f, -5.5f });
            try
            {
                foreach (var wind in new[] { WindDirection.Up, WindDirection.Down, WindDirection.Left, WindDirection.Right })
                {
                    TestWind(wind);
                    Vector2 velocity = Vector2.zero, position = Vector2.zero;
                    for (int i = 0; i < 1200; i++) BattleWind.IntegrateBullet(Vector2.zero, 0.01f, ref velocity, ref position);
                    if (Vector2.Dot(position, BattleWind.Vector) <= 0f || Vector2.Dot(velocity, BattleWind.Vector) <= 0f)
                        throw new Exception("Expected wind-only drift throughout bullet lifetime: " + wind);
                }
            }
            finally { BattleWind.Reset(); }
            Debug.Log("[Stage3ParallelWarning] Saved Phase7/8: edge-centered spawn, zero authored velocity indefinitely; wind-only movement. Verified 12s drift in all 4 directions. Counts and spacing unchanged; boss motion preserved.");
        }

        static void RoundPhaseSpawnTimes()
        {
            int changed = 0;
            for (int number = 1; number <= 7; number++)
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
            if (HasMergedPhase7()) { RevisePhaseTwoAndWindSequence(); return; }
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
                Wind(P(phase, "wind"), WindDirection.Up);
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
                bossSo.FindProperty("strongHpRatio").floatValue = 0.3f;
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

        [MenuItem("Tools/Stage3/Set Wind Leaf Circle Collider")]
        static void ConfigureWindLeafCircleCollider()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode first");
            string path = Prefabs + "3_Bullet_WindPurple.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                SetWindLeafCircleCollider(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var circle = saved.GetComponent<CircleCollider2D>();
            if (saved.GetComponent<BoxCollider2D>() != null || circle == null || !circle.isTrigger ||
                Mathf.Abs(circle.radius - 0.79f) > 0.0001f ||
                Vector2.Distance(circle.offset, saved.transform.Find("SpinningVisual").localPosition) > 0.0001f)
                throw new Exception("Wind leaf circle collider validation failed");
            Debug.Log("[Stage3WindLeafCollider] Saved shared boss/Phase7/Phase8 prefab: CircleCollider2D radius 0.79, centered on leaf ring, about 90% of visible ring radius. BoxCollider2D removed. Existing scale and visual settings preserved.");
        }

        static void SetWindLeafCircleCollider(GameObject root)
        {
            var circle = root.GetComponent<CircleCollider2D>();
            if (circle == null) circle = root.AddComponent<CircleCollider2D>();
            // EntityViewBaseのCollider2D依存を満たしてから既存の形状を取り除く。
            foreach (var box in root.GetComponents<BoxCollider2D>()) DestroyImmediate(box);
            circle.isTrigger = true;
            circle.radius = 0.79f; // 輪の外径1.76に対して、当たり判定の直径を約90%にする。
            circle.offset = root.transform.Find("SpinningVisual").localPosition;
        }

        [MenuItem("Tools/Stage3/Set Wind Parallel Leaf Visual")]
        static void ConfigureWindParallelLeafVisual()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode first");
            const string imagePath = "Assets/Project/Textures/Enemies/Stage3/Bullet_wind_leaf.png";
            AssetDatabase.ImportAsset(imagePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            // 可視部分570pxを旧画像の880px / 500PPUと同じ大きさにする。
            importer.spritePixelsPerUnit = 570f / (880f / 500f);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            textureSettings.spritePivot = new Vector2(390f / 720f, 375f / 720f);
            importer.SetTextureSettings(textureSettings);
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);

            string clipPath = Prefabs + "3_WindLeafSpin.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "3_WindLeafSpin" };
                AssetDatabase.CreateAsset(clip, clipPath);
            }
            clip.frameRate = 60f;
            var binding = EditorCurveBinding.FloatCurve("", typeof(Transform), "localEulerAnglesRaw.z");
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Linear(0f, 0f, 1f, 360f));
            var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
            EditorUtility.SetDirty(clip);
            string controllerPath = Prefabs + "3_WindLeafSpin.controller";
            var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
            if (controller == null)
                controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath, clip);
            var state = controller.layers[0].stateMachine.defaultState;
            state.motion = clip;
            state.speed = 1f;
            EditorUtility.SetDirty(controller);

            string bulletPath = Prefabs + "3_Bullet_WindPurple.prefab";
            var root = PrefabUtility.LoadPrefabContents(bulletPath);
            try
            {
                var rootRenderer = root.GetComponent<SpriteRenderer>();
                var visual = root.transform.Find("SpinningVisual");
                if (visual == null)
                {
                    visual = new GameObject("SpinningVisual").transform;
                    visual.SetParent(root.transform, false);
                }
                visual.localPosition = new Vector3(0f, 0.08f, 0f);
                visual.localRotation = Quaternion.identity;
                visual.localScale = Vector3.one;
                visual.gameObject.layer = root.layer;
                var renderer = visual.GetComponent<SpriteRenderer>();
                if (renderer == null) renderer = visual.gameObject.AddComponent<SpriteRenderer>();
                EditorUtility.CopySerialized(rootRenderer, renderer);
                renderer.sprite = sprite;
                renderer.enabled = true;
                rootRenderer.enabled = false;
                rootRenderer.sprite = null;
                var animator = visual.GetComponent<Animator>();
                if (animator == null) animator = visual.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var presenter = root.GetComponent<BulletEntityPresenter>();
                using (var so = Edit(presenter))
                {
                    so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
                    var steps = so.FindProperty("movementSteps");
                    steps.arraySize = 1;
                    steps.GetArrayElementAtIndex(0).managedReferenceValue = new InfiniteMovementConfig(Vector3.zero);
                    Apply(so);
                }
                using (var so = Edit(root.GetComponent<Project.Scenes.Battle.Scripts.View.Entity.BulletEntityView>()))
                {
                    so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
                    Apply(so);
                }
                // 風が制御する親の位置・回転とは独立した子の自転を検証する。
                foreach (float time in new[] { 0.25f, 0.5f, 0.75f, 1f })
                {
                    clip.SampleAnimation(visual.gameObject, time);
                    if (Quaternion.Angle(visual.localRotation, Quaternion.Euler(0f, 0f, 360f * time)) > 0.01f)
                        throw new Exception("Wind leaf must rotate once per second");
                }
                visual.localRotation = Quaternion.identity;
                SetWindLeafCircleCollider(root);
                PrefabUtility.SaveAsPrefabAsset(root, bulletPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("[Stage3WindLeaf] Saved shared boss/Phase7/Phase8 bullet: untitled_50 leaf texture, preserved visible size, circle collider radius 0.79, 360 degrees per second on child visual, zero authored velocity. Quarter-turn animation samples verified.");
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
                if (collider != null)
                {
                    collider.size = new Vector2(0.83f, 0.83f);
                    collider.offset = new Vector2(0f, 0.08f);
                }
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
            var movement = AssetDatabase.LoadAssetAtPath<MovementPreset>(Data + "Bullet/3_Boss_WindSpeed1_5.asset");
            var parallel = BossAttack("3_Boss_WindParallel", new[] { -2f, 0f, 2f }
                .Select(offset => BossEntry(0f, new SingleBulletSignal(), 2, movement,
                    new ScreenEdgeDirectionConfig(offset, -0.15f, 1.4f))).ToArray());
            // 右風用の既存参照も、同じ並行弾設定を辿るようにする。
            BossAttack("3_Boss_WindParallelRightWind", BossEntry(0f, new PresetAttackSignal(parallel, 1, 1f)));
            AssetDatabase.SaveAssets();
            ConfigureWindParallelEdgeWarning();
            if (System.IO.File.Exists("Assets/Project/Textures/Enemies/Stage3/Bullet_wind_leaf.png"))
                ConfigureWindParallelLeafVisual();
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
            if (Mathf.Abs(ev.GetStartDelayAt(0)) > 0.0001f || ev.BulletMovementOverride == null)
                throw new Exception("Boss parallel bullets must launch immediately");
            using var so = new SerializedObject(ev.BulletMovementOverride);
            var steps = so.FindProperty("steps");
            if (steps.arraySize != 1 || steps.GetArrayElementAtIndex(0).managedReferenceValue is not InfiniteMovementConfig ||
                Mathf.Abs(P(steps.GetArrayElementAtIndex(0), "velocity").vector3Value.magnitude - 1.5f) > 0.0001f)
                throw new Exception("Expected boss parallel bullet initial speed 1.5");
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
                Debug.Log("[Stage3WindParallel] All 4 directions and 3 strong presets passed: 7 volleys of 3 parallel bullets, original offscreen origin, immediate initial speed 1.5 along wind, spacing 2.0.");
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
