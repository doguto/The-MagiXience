using System;
using System.Linq;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scripts.Extensions.Message;
using UniRx;

namespace Project.Scenes.Battle.Editor
{
    // An explicit editor authoring operation: never runs on import or on entering Play mode.
    public class Stage3WindWorkshop : EditorWindow
    {
        const string Data = "Assets/Project/DataStore/Stage3/";
        const string Prefabs = "Assets/Project/Scenes/Battle/Prefabs/Stage3/";
        const string BulletName = "3_Bullet_WindAimed";
        float flightTime = 4.2f;
        float vertexDepth = 1.1f;
        float margin = 0.35f;
        string result = "Ready. Assets are changed only when a button is pressed.";
        static bool movementPlaytest;
        static bool testStarted;
        static float testStartTime;
        static PlayerEntityPresenter testPlayer;

        [InitializeOnLoadMethod]
        static void RegisterPlaytest()
        {
            EditorApplication.update -= UpdatePlaytest;
            EditorApplication.update += UpdatePlaytest;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode) StopPlaytest();
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Stage3WindPlaytest", false))
                {
                    SessionState.SetBool("Stage3WindPlaytest", false);
                    movementPlaytest = true;
                }
            };
        }

        static void StopPlaytest()
        {
            if (movementPlaytest && EditorApplication.isPlaying)
                MessageBroker.Default.Publish(new PlayerMoveMessage { value = Vector2.zero });
            movementPlaytest = testStarted = false;
            testPlayer = null;
        }

        static void UpdatePlaytest()
        {
            if (!movementPlaytest || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
            if (!BattleWind.Enabled)
            {
                if (testStarted)
                {
                    Debug.Log($"[Stage3WindPlaytest] Completed. HP={testPlayer?.Model.CurrentHp.Value}; curved={AimedParabola.CurvedShots}; calm={AimedParabola.CalmShots}; fallback={AimedParabola.FallbackShots}; axes={AimedParabola.WindDirectionsUsed}; aimError={AimedParabola.MaxAimError:F6}");
                    StopPlaytest();
                }
                return;
            }
            if (!testPlayer) testPlayer = FindFirstObjectByType<PlayerEntityPresenter>();
            if (!testPlayer || testPlayer.Model == null) return;
            if (!testStarted) { testStarted = true; testStartTime = Time.time; }
            if (!testPlayer.Model.IsAlive) { Debug.LogWarning("[Stage3WindPlaytest] Player died; stopped movement test."); StopPlaytest(); return; }
            float t = Time.time - testStartTime;
            Vector2 desired = new(-5f + Mathf.Sin(t * 0.55f) * 2f, -1f + Mathf.Sin(t * 0.91f) * 2.3f);
            Vector2 input = Vector2.ClampMagnitude((desired - (Vector2)testPlayer.transform.position) * 0.8f - BattleWind.Vector * BattleWind.PlayerSpeedRatio, 1f);
            MessageBroker.Default.Publish(new PlayerMoveMessage { value = input });
        }

        [MenuItem("Tools/Stage 3/Wind Workshop")]
        public static void Open() => GetWindow<Stage3WindWorkshop>("Stage 3 Wind");

        void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying) Repaint();
        }

        void OnGUI()
        {
            GUILayout.Label("Stage 3 / Wind & Aimed Parabolas", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Aim is sampled at launch. Wind is frozen for each bullet.\nCalm: straight aim. Up / down / left: vertex inside the screen.", MessageType.Info);
            flightTime = EditorGUILayout.Slider("Target flight time (s)", flightTime, 2.5f, 6f);
            vertexDepth = EditorGUILayout.Slider("Vertex depth", vertexDepth, 0.1f, 3f);
            margin = EditorGUILayout.Slider("Screen margin", margin, 0.05f, 0.8f);
            EditorGUILayout.Space();
            GUILayout.Label("4 phases: 9 / 18 / 9 / 9 enemies", EditorStyles.boldLabel);
            GUILayout.Label("33 wind enemies x 3 shots + 12 fan enemies x 6 = 171 bullets.");
            EditorGUILayout.HelpBox("Rebuild replaces Stage3 phases 1–4 and the generated Wind prefabs/presets.\nExisting original enemy and bullet prefabs are retained. No scene is saved.", MessageType.None);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Rebuild Stage 3 (4 phases + wind prefabs)", GUILayout.Height(34))) Run(Rebuild);
                if (GUILayout.Button("Apply parameters to Wind Aimed bullet only")) Run(ApplyBulletParameters);
                if (GUILayout.Button("Validate trajectories, wind and phase assets")) Run(ValidateAll);
            }
            if (GUILayout.Button("Select Stage3Way wind schedule")) Selection.activeObject = Load<BattleSequenceAsset>(Data + "Way/Stage3Way.asset");
            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling))
            {
                if (GUILayout.Button(movementPlaytest ? "Stop movement playtest" : "Start movement playtest (no shooting / no invincibility)"))
                {
                    if (movementPlaytest) StopPlaytest();
                    else
                    {
                        testStarted = false;
                        if (EditorApplication.isPlaying) movementPlaytest = true;
                        else
                        {
                            SessionState.SetBool("Stage3WindPlaytest", true);
                            EditorApplication.isPlaying = true;
                        }
                    }
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(result, MessageType.Info);
            if (Application.isPlaying)
                GUILayout.Label($"Wind: {BattleWind.Direction} / Curved: {AimedParabola.CurvedShots} / Calm: {AimedParabola.CalmShots} / Fallback: {AimedParabola.FallbackShots}");
        }

        void Run(Action action)
        {
            try { action(); Repaint(); }
            catch (Exception ex) { result = ex.Message; Debug.LogException(ex); }
        }

        static T Load<T>(string path) where T : UnityEngine.Object
            => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing asset: " + path);

        static GameObject CopyPrefab(string source, string name)
        {
            string path = Prefabs + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null && !AssetDatabase.CopyAsset(Prefabs + source + ".prefab", path))
                throw new InvalidOperationException("Could not copy prefab: " + source);
            return Load<GameObject>(path);
        }

        void ApplyBulletParameters()
        {
            string path = Prefabs + BulletName + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var so = new SerializedObject(root.GetComponent<BulletEntityPresenter>());
                var steps = so.FindProperty("movementSteps");
                steps.arraySize = 1;
                steps.GetArrayElementAtIndex(0).managedReferenceValue = new AcceleratedMovementConfig();
                so.ApplyModifiedPropertiesWithoutUndo();
                var step = so.FindProperty("movementSteps").GetArrayElementAtIndex(0);
                step.FindPropertyRelative("aimAtPlayer").boolValue = true;
                step.FindPropertyRelative("useBattleWind").boolValue = true;
                step.FindPropertyRelative("targetFlightTime").floatValue = flightTime;
                step.FindPropertyRelative("vertexDepth").floatValue = vertexDepth;
                step.FindPropertyRelative("vertexScreenMargin").floatValue = margin;
                so.FindProperty("lifetime").floatValue = 12f;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            result = "Wind Aimed bullet parameters saved.";
        }

        void Enemy(string source, string name, bool fan)
        {
            CopyPrefab(source, name);
            string path = Prefabs + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var presenter = root.GetComponent<EnemyEntityPresenter>();
                var so = new SerializedObject(presenter);
                var phase = so.FindProperty("attackTimeline.phases").GetArrayElementAtIndex(0);
                phase.FindPropertyRelative("loopStart").floatValue = 2.6f;
                phase.FindPropertyRelative("loopEnd").floatValue = fan ? 3.1f : 3.5f;
                phase.FindPropertyRelative("cycleDuration").floatValue = 0.4f;
                so.ApplyModifiedPropertiesWithoutUndo();
                if (!fan)
                {
                    var pool = root.transform.Find("BulletPool").GetComponent<MonoBehaviour>();
                    var poolSo = new SerializedObject(pool);
                    poolSo.FindProperty("bulletPrefab").objectReferenceValue = Load<GameObject>(Prefabs + BulletName + ".prefab").GetComponent<BulletEntityPresenter>();
                    poolSo.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static MovementPreset Movement(string name, bool hook, float sign)
        {
            string path = Data + "Movement/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<MovementPreset>(path);
            if (!asset)
            {
                asset = CreateInstance<MovementPreset>();
                AssetDatabase.CreateAsset(asset, path);
            }
            var so = new SerializedObject(asset);
            var steps = so.FindProperty("steps");
            steps.arraySize = hook ? 2 : 1;
            steps.GetArrayElementAtIndex(0).managedReferenceValue = new PathMovementConfig();
            if (hook) steps.GetArrayElementAtIndex(1).managedReferenceValue = new TweenMovementConfig();
            so.ApplyModifiedPropertiesWithoutUndo();
            var p = so.FindProperty("steps").GetArrayElementAtIndex(0);
            p.FindPropertyRelative("duration").floatValue = hook ? 4.6f : 7.6f;
            p.FindPropertyRelative("easeValue").intValue = (int)Ease.Linear;
            p.FindPropertyRelative("isRelative").boolValue = true;
            Vector3[] points = hook
                ? new[] { new Vector3(-5, -sign), new Vector3(-6, -2 * sign), new Vector3(-4, -3 * sign) }
                : new[] { new Vector3(-4, 1.1f), new Vector3(-7, -1.2f), new Vector3(-11, 1.1f), new Vector3(-20, 0) };
            var waypoints = p.FindPropertyRelative("waypoints");
            waypoints.arraySize = points.Length;
            for (int i = 0; i < points.Length; i++) waypoints.GetArrayElementAtIndex(i).vector3Value = points[i];
            if (hook)
            {
                var exit = so.FindProperty("steps").GetArrayElementAtIndex(1);
                exit.FindPropertyRelative("targetOffset").vector3Value = new Vector3(6, 0);
                exit.FindPropertyRelative("duration").floatValue = 1.6f;
                exit.FindPropertyRelative("easeValue").intValue = (int)Ease.InSine;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        readonly struct Spawn
        {
            public readonly float time, y;
            public readonly int enemy;
            public readonly string movement;
            public Spawn(float t, float y, int enemy, string movement)
            { time = t; this.y = y; this.enemy = enemy; this.movement = movement; }
        }

        // Each phase has its own rhythm. Pauses, near-pairs and side-swapping flankers are intentional.
        static Spawn[][] Layout => new[]
        {
            new[] {
                new Spawn(1, 2.6f, 0, "WindHookUpper"), new Spawn(3.8f,-1.8f,1,"Straight2"),
                new Spawn(6.2f,0.6f,2,"Straight2"), new Spawn(10.5f,-2.6f,1,"WindHookLower"),
                new Spawn(12,1.6f,0,"WindWeave"), new Spawn(14.8f,-0.8f,2,"Straight1"),
                new Spawn(19.3f,2.3f,0,"Straight2"), new Spawn(22,-2.2f,2,"WindWeave"),
                new Spawn(26,0.3f,1,"Straight1") },
            new[] {
                new Spawn(1,2.6f,0,"WindHookUpper"), new Spawn(1.7f,-2.6f,1,"WindHookLower"),
                new Spawn(3.9f,0,2,"Straight2"), new Spawn(5.1f,1.6f,0,"WindWeave"),
                new Spawn(7.3f,-2,1,"Straight1"), new Spawn(8.2f,2.4f,0,"Straight2"),
                new Spawn(10.8f,2.6f,1,"WindHookUpper"), new Spawn(11,-1,2,"WindWeave"),
                new Spawn(13.6f,-2.6f,0,"WindHookLower"), new Spawn(14.9f,0.8f,1,"Straight2"),
                new Spawn(17.8f,-1.7f,0,"WindWeave"), new Spawn(19,2.2f,2,"Straight2"),
                new Spawn(20.4f,2.6f,1,"WindHookUpper"), new Spawn(22.1f,-2.2f,0,"Straight2"),
                new Spawn(23,0.4f,1,"Straight1"), new Spawn(25.2f,-2.6f,2,"WindHookLower"),
                new Spawn(26.4f,1.4f,0,"WindWeave"), new Spawn(27.5f,-1.2f,1,"Straight2") },
            new[] {
                new Spawn(0.8f,-2.6f,1,"WindHookLower"), new Spawn(1.4f,2.1f,2,"Straight2"),
                new Spawn(3.2f,2.6f,0,"WindHookUpper"), new Spawn(4.7f,-1.7f,0,"WindWeave"),
                new Spawn(5.1f,0.8f,2,"Straight1"), new Spawn(7,-2.4f,1,"Straight2"),
                new Spawn(8.4f,1.8f,0,"WindWeave"), new Spawn(10,-1.6f,2,"Straight2"),
                new Spawn(11.7f,2.6f,1,"WindHookUpper") },
            new[] {
                new Spawn(0.8f,0.2f,0,"Straight2"), new Spawn(2.2f,-2.6f,1,"WindHookLower"),
                new Spawn(3.5f,2.2f,2,"WindWeave"), new Spawn(5.8f,2.6f,1,"WindHookUpper"),
                new Spawn(7.1f,-1.7f,0,"WindWeave"), new Spawn(8,1.3f,1,"Straight1"),
                new Spawn(10.6f,-2.1f,2,"Straight2"), new Spawn(12.2f,2.6f,0,"WindHookUpper"),
                new Spawn(14.4f,-2.6f,1,"WindHookLower") }
        };

        void Rebuild()
        {
            // Check all source dependencies before touching assets.
            foreach (var name in new[] { "3_Phase1A", "3_Phase1B", "3_Enemy_CommonBrake", "3_Bullet_ParabolaDownUp" })
                Load<GameObject>(Prefabs + name + ".prefab");
            Load<BattleSequenceAsset>(Data + "Way/Stage3Way.asset");
            Load<MovementPreset>(Data + "Movement/Straight1.asset");
            Load<MovementPreset>(Data + "Movement/Straight2.asset");
            CopyPrefab("3_Bullet_ParabolaDownUp", BulletName);
            ApplyBulletParameters();
            Enemy("3_Phase1A", "3_Enemy_WindBlue", false);
            Enemy("3_Phase1B", "3_Enemy_WindPink", false);
            Enemy("3_Enemy_CommonBrake", "3_Enemy_CommonFan", true);
            Movement("WindHookUpper", true, 1);
            Movement("WindHookLower", true, -1);
            Movement("WindWeave", false, 1);
            var enemies = new[] { "3_Enemy_WindBlue", "3_Enemy_WindPink", "3_Enemy_CommonFan" }
                .Select(n => Load<GameObject>(Prefabs + n + ".prefab")).ToArray();
            var layout = Layout;
            for (int i = 0; i < layout.Length; i++)
            {
                var asset = Load<BattleTimelineBuilderAsset>(Data + $"Way/3_Phase{i + 1}.asset");
                Undo.RecordObject(asset, "Rebuild Stage 3 wind phase");
                var so = new SerializedObject(asset);
                var tracks = so.FindProperty("enemySpawnTracks");
                tracks.arraySize = 1;
                var track = tracks.GetArrayElementAtIndex(0);
                track.FindPropertyRelative("trackName").stringValue = "EnemySpawn";
                var clips = track.FindPropertyRelative("clips");
                clips.arraySize = layout[i].Length;
                for (int j = 0; j < layout[i].Length; j++)
                {
                    var spawn = layout[i][j];
                    var clip = clips.GetArrayElementAtIndex(j);
                    clip.FindPropertyRelative("time").doubleValue = spawn.time;
                    clip.FindPropertyRelative("spawnPosition").vector3Value = new Vector3(10, spawn.y);
                    clip.FindPropertyRelative("prefab").objectReferenceValue = enemies[spawn.enemy];
                    clip.FindPropertyRelative("movementOverride").objectReferenceValue = Load<MovementPreset>(Data + "Movement/" + spawn.movement + ".asset");
                    clip.FindPropertyRelative("attackOverride").objectReferenceValue = null;
                }
                so.ApplyModifiedProperties();
            }
            var sequence = Load<BattleSequenceAsset>(Data + "Way/Stage3Way.asset");
            Undo.RecordObject(sequence, "Configure Stage 3 wind");
            var sequenceSo = new SerializedObject(sequence);
            sequenceSo.FindProperty("wind.enabled").boolValue = true;
            sequenceSo.FindProperty("wind.playerSpeedRatio").floatValue = 0.2f;
            var intervals = sequenceSo.FindProperty("wind.intervals");
            var directions = new[] { 0, 1, 2, 0, 3, 1, 0, 2, 3 };
            var seconds = new[] { 4f, 9f, 7f, 3f, 8f, 6f, 4f, 9f, 6f };
            intervals.arraySize = directions.Length;
            for (int i = 0; i < directions.Length; i++)
            {
                var interval = intervals.GetArrayElementAtIndex(i);
                interval.FindPropertyRelative("direction").enumValueIndex = directions[i];
                interval.FindPropertyRelative("seconds").floatValue = seconds[i];
            }
            sequenceSo.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            ValidateAll();
            result = "Saved: 4 phases, 45 enemies, 171 planned bullets, wind schedule and 3 movement presets.\nValidation passed. Play Battle to try Stage 3.";
            Debug.Log("[Stage3WindWorkshop] " + result);
        }

        void ValidateAll()
        {
            int trajectories = 0;
            var screen = Rect.MinMaxRect(-8.8889f, -5f, 8.8889f, 5f);
            foreach (var axis in new[] { Vector2.up, Vector2.down, Vector2.left })
            foreach (float sx in new[] { -6f, 0f, 7f, 8.7f })
            foreach (float sy in new[] { -4.8f, -2f, 0f, 4.8f })
            foreach (float tx in new[] { -8.6f, -2f, 6f })
            foreach (float ty in new[] { -4.99f, -1f, 2.6f, 4.99f })
            {
                Vector2 start = new(sx, sy), target = new(tx, ty);
                Check(AimedParabola.TrySolve(start, target, axis, screen, flightTime, vertexDepth, margin,
                    out var v, out var a, out var vertex), "No valid parabola");
                Check(Vector2.Distance(AimedParabola.Position(start, v, a, flightTime), target) < 0.001f, "Aim error");
                Check(screen.Contains(vertex), "Vertex outside screen");
                Check(Vector2.Dot(a.normalized, axis) > 0.9999f, "Acceleration direction");
                for (int sample = 0; sample <= 32; sample++)
                    Check(screen.Contains(AimedParabola.Position(start, v, a, flightTime * sample / 32f)), "Path outside screen");
                trajectories++;
            }
            var wind = new BattleWindSettings { enabled = true, playerSpeedRatio = 0.2f };
            wind.intervals.Add(new WindInterval { direction = WindDirection.Up, seconds = 2 });
            wind.intervals.Add(new WindInterval { direction = WindDirection.Left, seconds = 3 });
            BattleWind.Begin(wind);
            Check(Mathf.Abs(BattleWind.PlayerVelocity(Vector2.up, 5).y - 6) < 0.001f, "Up speed");
            Check(Mathf.Abs(BattleWind.PlayerVelocity(Vector2.down, 5).y + 4) < 0.001f, "Down speed");
            Check(Mathf.Abs(BattleWind.PlayerVelocity(Vector2.zero, 5).y - 1) < 0.001f, "Idle drift");
            BattleWind.Tick(2.1f);
            Check(BattleWind.Direction == WindDirection.Left, "Wind timing");
            BattleWind.Tick(0);
            Check(BattleWind.Direction == WindDirection.Left, "Pause");
            BattleWind.Begin(wind);
            Check(BattleWind.Direction == WindDirection.Up, "Retry reset");
            BattleWind.Reset();
            Check(BattleWind.PlayerVelocity(Vector2.up, 5) == Vector2.up * 5, "Other stages unaffected");
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                var asset = Load<BattleTimelineBuilderAsset>(Data + $"Way/3_Phase{i + 1}.asset");
                Check(asset.TotalEnemySpawnCount == Layout[i].Length, "Phase enemy count");
                var tracks = new SerializedObject(asset).FindProperty("enemySpawnTracks");
                Check(tracks.arraySize == 1, "Only one bound spawn track allowed");
                count += asset.TotalEnemySpawnCount;
            }
            result = $"Passed: {trajectories} trajectories, 1.2x/0.8x/idle wind, timing, reset, {count} enemies.";
            Debug.Log("[Stage3WindWorkshop] " + result);
        }

        static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Validation failed: " + message); }
    }
}
