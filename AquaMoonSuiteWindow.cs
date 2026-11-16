#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AquaMoonSuiteWindow : EditorWindow
{
    private enum ActiveTab { PhysicsSandbox, ScatterBrush, AlignDistribute }
    private ActiveTab currentTab = ActiveTab.PhysicsSandbox;

    // --- Tab 1: Physics Sandbox State ---
    private enum GravityType { Directional, PointSpherical }
    private GravityType gravityType = GravityType.Directional;
    private Vector3 directionalGravity = new Vector3(0f, -9.81f, 0f);
    private Transform sphericalGravityCenter;
    private float sphericalGravityStrength = 9.81f;

    private bool randomRotationPreDrop = true;
    private Vector3 randomRotationAngles = new Vector3(360f, 360f, 360f);
    private bool randomScalePreDrop = false;
    private float minDropScale = 0.8f;
    private float maxDropScale = 1.2f;

    private bool autoAddColliders = true;
    private bool enableCharacterPushing = true;
    private float characterPushVelocity = 2.5f;

    private bool isPhysicsRunning = false;
    private List<Rigidbody> sandboxBodies = new List<Rigidbody>();
    private List<Collider> sandboxColliders = new List<Collider>();
    private Rigidbody activeDraggedBody = null;
    private float dragPlaneDistance = 0f;

    // --- Tab 2: Scatter Brush Pro State ---
    private ScatterProfile currentProfile;
    private bool isBrushActive = false;
    private readonly Collider[] overlapBuffer = new Collider[16];

    // --- Tab 3: Alignment Axis ---
    private enum AlignAxis { X = 0, Y = 1, Z = 2 }

    [MenuItem("Tools/AquaMoon/Level Suite Pro")]
    public static void OpenSuite()
    {
        var window = GetWindow<AquaMoonSuiteWindow>("AquaMoon Suite");
        window.minSize = new Vector2(360f, 520f);
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        if (isPhysicsRunning) StopPhysicsSandbox(commit: true);
        isBrushActive = false;
    }

    private void OnGUI()
    {
        DrawBanner();
        currentTab = (ActiveTab)GUILayout.Toolbar((int)currentTab, new string[] { "Physics Sandbox", "Scatter Brush", "Align & Distribute" }, GUILayout.Height(28));
        EditorGUILayout.Space(8);

        switch (currentTab)
        {
            case ActiveTab.PhysicsSandbox:
                DrawPhysicsTab();
                break;
            case ActiveTab.ScatterBrush:
                DrawScatterTab();
                break;
            case ActiveTab.AlignDistribute:
                DrawAlignTab();
                break;
        }
    }

    private void DrawBanner()
    {
        EditorGUILayout.Space(4);
        var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField("AQUAMOON // LEVEL SUITE PRO", titleStyle);
        EditorGUILayout.LabelField("Professional Tooling Architecture", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(4);
    }

    // ==========================================
    // TAB 1: PHYSICS SANDBOX
    // ==========================================
    private void DrawPhysicsTab()
    {
        EditorGUILayout.LabelField("Gravity Simulation", EditorStyles.boldLabel);
        gravityType = (GravityType)EditorGUILayout.EnumPopup("Gravity System", gravityType);

        if (gravityType == GravityType.Directional)
        {
            directionalGravity = EditorGUILayout.Vector3Field("Vector Direction", directionalGravity);
        }
        else
        {
            sphericalGravityCenter = (Transform)EditorGUILayout.ObjectField("Center Transform", sphericalGravityCenter, typeof(Transform), true);
            sphericalGravityStrength = EditorGUILayout.FloatField("Gravity Field Power", sphericalGravityStrength);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Procedural Jitter (On Drop)", EditorStyles.boldLabel);
        randomRotationPreDrop = EditorGUILayout.Toggle("Randomize Rotation", randomRotationPreDrop);
        if (randomRotationPreDrop)
        {
            randomRotationAngles = EditorGUILayout.Vector3Field("Max Angles", randomRotationAngles);
        }

        randomScalePreDrop = EditorGUILayout.Toggle("Randomize Scale", randomScalePreDrop);
        if (randomScalePreDrop)
        {
            minDropScale = EditorGUILayout.FloatField("Min Scale", minDropScale);
            maxDropScale = EditorGUILayout.FloatField("Max Scale", maxDropScale);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Safety & Interactivity", EditorStyles.boldLabel);
        autoAddColliders = EditorGUILayout.Toggle("Auto Colliders", autoAddColliders);
        enableCharacterPushing = EditorGUILayout.Toggle("Player Collision Pusher", enableCharacterPushing);
        if (enableCharacterPushing)
        {
            characterPushVelocity = EditorGUILayout.FloatField("Push Power", characterPushVelocity);
        }

        EditorGUILayout.Space(12);

        if (!isPhysicsRunning)
        {
            GUI.backgroundColor = new Color(0.25f, 0.75f, 0.4f);
            if (GUILayout.Button("START PHYSICS ENGINE", GUILayout.Height(36)))
            {
                StartPhysicsSandbox();
            }
        }
        else
        {
            GUI.backgroundColor = new Color(0.9f, 0.35f, 0.35f);
            if (GUILayout.Button("COMMIT POSITIONS (BAKE)", GUILayout.Height(32)))
            {
                StopPhysicsSandbox(commit: true);
            }

            GUI.backgroundColor = new Color(0.95f, 0.7f, 0.2f);
            if (GUILayout.Button("ABORT (RESET TRANSFORMS)", GUILayout.Height(24)))
            {
                StopPhysicsSandbox(commit: false);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("HOLD [CTRL + LEFT CLICK] to grab and fling dynamic bodies.\nUse the button below to lock stationary items.", MessageType.Info);

            if (GUILayout.Button("Toggle Kinematic Lock (Selected)"))
            {
                foreach (GameObject go in Selection.gameObjects)
                {
                    if (go.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = !rb.isKinematic;
                }
            }
        }
        GUI.backgroundColor = Color.white;
    }

    private void StartPhysicsSandbox()
    {
        GameObject[] targets = Selection.gameObjects;
        if (targets == null || targets.Length == 0)
        {
            EditorUtility.DisplayDialog("Physics Sandbox", "Select at least one object in the Hierarchy.", "OK");
            return;
        }

        isPhysicsRunning = true;
        sandboxBodies.Clear();
        sandboxColliders.Clear();
        Physics.simulationMode = SimulationMode.Script;

        foreach (GameObject go in targets)
        {
            if (go == null) continue;
            Undo.RegisterFullObjectHierarchyUndo(go, "Start Sandbox");

            if (randomRotationPreDrop)
            {
                Vector3 rot = go.transform.eulerAngles;
                rot.x += UnityEngine.Random.Range(-randomRotationAngles.x, randomRotationAngles.x);
                rot.y += UnityEngine.Random.Range(-randomRotationAngles.y, randomRotationAngles.y);
                rot.z += UnityEngine.Random.Range(-randomRotationAngles.z, randomRotationAngles.z);
                go.transform.eulerAngles = rot;
            }

            if (randomScalePreDrop)
            {
                go.transform.localScale *= UnityEngine.Random.Range(minDropScale, maxDropScale);
            }

            if (!go.TryGetComponent<Collider>(out _))
            {
                if (autoAddColliders && go.TryGetComponent<MeshFilter>(out _))
                {
                    var mc = go.AddComponent<MeshCollider>();
                    mc.convex = true;
                    sandboxColliders.Add(mc);
                }
                else
                {
                    var bc = go.AddComponent<BoxCollider>();
                    sandboxColliders.Add(bc);
                }
            }

            if (!go.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb = go.AddComponent<Rigidbody>();
                sandboxBodies.Add(rb);
            }

            rb.isKinematic = false;
            rb.useGravity = (gravityType == GravityType.Directional);
        }

        if (enableCharacterPushing)
        {
            var controllers = UnityEngine.Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None);
            foreach (var cc in controllers)
            {
                if (!cc.TryGetComponent<CharacterPhysicsPusher>(out var p))
                {
                    p = cc.gameObject.AddComponent<CharacterPhysicsPusher>();
                    p.pushPower = characterPushVelocity;
                }
            }
        }

        EditorApplication.update += StepPhysicsSandbox;
    }

    private void StepPhysicsSandbox()
    {
        if (!isPhysicsRunning) return;

        if (gravityType == GravityType.PointSpherical && sphericalGravityCenter != null)
        {
            Vector3 center = sphericalGravityCenter.position;
            foreach (var rb in sandboxBodies)
            {
                if (rb != null && !rb.isKinematic)
                {
                    Vector3 pullDir = (center - rb.position).normalized;
                    rb.AddForce(pullDir * sphericalGravityStrength, ForceMode.Acceleration);
                }
            }
        }
        else if (gravityType == GravityType.Directional)
        {
            Physics.gravity = directionalGravity;
        }

        Physics.Simulate(Time.fixedDeltaTime);
        SceneView.RepaintAll();
    }

    private void StopPhysicsSandbox(bool commit)
    {
        EditorApplication.update -= StepPhysicsSandbox;
        isPhysicsRunning = false;
        Physics.simulationMode = SimulationMode.FixedUpdate;

        foreach (var rb in sandboxBodies) if (rb != null) DestroyImmediate(rb);
        foreach (var col in sandboxColliders) if (col != null) DestroyImmediate(col);

        sandboxBodies.Clear();
        sandboxColliders.Clear();
        activeDraggedBody = null;

        if (!commit) Undo.PerformUndo();

        SceneView.RepaintAll();
        Repaint();
    }

    // ==========================================
    // TAB 2: SCATTER BRUSH PRO
    // ==========================================
    private void DrawScatterTab()
    {
        EditorGUILayout.LabelField("Brush Configuration Profile", EditorStyles.boldLabel);
        currentProfile = (ScatterProfile)EditorGUILayout.ObjectField("Active Profile", currentProfile, typeof(ScatterProfile), false);

        if (currentProfile == null)
        {
            EditorGUILayout.HelpBox("Assign a ScatterProfile asset to use the multi-prefab brush with weight controls.", MessageType.Warning);
            if (GUILayout.Button("Create New Scatter Profile"))
            {
                var profile = ScriptableObject.CreateInstance<ScatterProfile>();
                AssetDatabase.CreateAsset(profile, "Assets/NewScatterProfile.asset");
                AssetDatabase.SaveAssets();
                currentProfile = profile;
            }
            return;
        }

        var editor = Editor.CreateEditor(currentProfile);
        editor.OnInspectorGUI();

        EditorGUILayout.Space(10);
        if (!isBrushActive)
        {
            GUI.backgroundColor = new Color(0.3f, 0.65f, 0.95f);
            if (GUILayout.Button("ACTIVATE SURFACE BRUSH", GUILayout.Height(34))) isBrushActive = true;
        }
        else
        {
            GUI.backgroundColor = new Color(0.9f, 0.35f, 0.35f);
            if (GUILayout.Button("DEACTIVATE BRUSH", GUILayout.Height(34))) isBrushActive = false;

            EditorGUILayout.HelpBox("SHORTCUTS:\n- [SHIFT + LEFT DRAG]: Paint prefabs on surfaces.\n- [CTRL + SCROLL]: Dynamic brush radius.\n- [CTRL + Z]: Step-by-step undo.", MessageType.Info);
        }
        GUI.backgroundColor = Color.white;
    }

    // ==========================================
    // TAB 3: ALIGN & DISTRIBUTE
    // ==========================================
    private void DrawAlignTab()
    {
        EditorGUILayout.LabelField("Precision Coordinate Snapping", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select at least 2 objects for Align, or 3+ for Distribute.", MessageType.Info);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Align Positions (Pivot based):", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Align X Axis")) ExecuteAlign(AlignAxis.X);
        if (GUILayout.Button("Align Y Axis")) ExecuteAlign(AlignAxis.Y);
        if (GUILayout.Button("Align Z Axis")) ExecuteAlign(AlignAxis.Z);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Distribute Spacing (Even intervals):", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Distribute X")) ExecuteDistribute(AlignAxis.X);
        if (GUILayout.Button("Distribute Y")) ExecuteDistribute(AlignAxis.Y);
        if (GUILayout.Button("Distribute Z")) ExecuteDistribute(AlignAxis.Z);
        EditorGUILayout.EndHorizontal();
    }

    private void ExecuteAlign(AlignAxis axis)
    {
        var targets = Selection.gameObjects;
        if (targets.Length < 2) return;

        Undo.RecordObjects(Array.ConvertAll(targets, t => t.transform), "Align Batch");
        int idx = (int)axis;
        float referencePos = targets[0].transform.position[idx];

        foreach (var go in targets)
        {
            Vector3 pos = go.transform.position;
            pos[idx] = referencePos;
            go.transform.position = pos;
        }
    }

    private void ExecuteDistribute(AlignAxis axis)
    {
        var targets = Selection.gameObjects;
        if (targets.Length < 3) return;

        Undo.RecordObjects(Array.ConvertAll(targets, t => t.transform), "Distribute Batch");
        int idx = (int)axis;

        Array.Sort(targets, (a, b) => a.transform.position[idx].CompareTo(b.transform.position[idx]));

        float minPos = targets[0].transform.position[idx];
        float maxPos = targets[targets.Length - 1].transform.position[idx];
        float interval = (maxPos - minPos) / (targets.Length - 1);

        for (int i = 0; i < targets.Length; i++)
        {
            Vector3 pos = targets[i].transform.position;
            pos[idx] = minPos + (interval * i);
            targets[i].transform.position = pos;
        }
    }

    // ==========================================
    // INTERACTION ENGINE (SCENE GUI)
    // ==========================================
    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        // Dynamic Drag in Sandbox
        if (isPhysicsRunning)
        {
            HandleSandboxDrag(sceneView, e);
        }

        // Scatter Paint & Radius Hotkey
        if (isBrushActive && currentProfile != null)
        {
            HandleScatterBrush(sceneView, e);
        }
    }

    private void HandleSandboxDrag(SceneView view, Event e)
    {
        if (e.control && e.type == EventType.MouseDown && e.button == 0)
        {
            Ray r = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(r, out RaycastHit hit))
            {
                if (hit.rigidbody != null && sandboxBodies.Contains(hit.rigidbody))
                {
                    activeDraggedBody = hit.rigidbody;
                    dragPlaneDistance = Vector3.Dot(view.camera.transform.forward, hit.point - view.camera.transform.position);
                    e.Use();
                }
            }
        }

        if (activeDraggedBody != null)
        {
            if (e.type == EventType.MouseDrag && e.button == 0)
            {
                Ray r = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Vector3 target = r.origin + r.direction * dragPlaneDistance;
                Vector3 delta = (target - activeDraggedBody.position) * 40f;
                activeDraggedBody.linearVelocity = Vector3.ClampMagnitude(delta, 30f);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                activeDraggedBody = null;
                e.Use();
            }
        }
    }

    private void HandleScatterBrush(SceneView view, Event e)
    {
        // Ctrl + Scroll Wheel modifies radius
        if (e.control && e.type == EventType.ScrollWheel)
        {
            currentProfile.radius = Mathf.Clamp(currentProfile.radius - e.delta.y * 0.4f, 0.5f, 50f);
            e.Use();
            Repaint();
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit surfaceHit, Mathf.Infinity, currentProfile.targetLayers))
        {
            // Custom Gradient Disc Handle
            Handles.color = new Color(0.2f, 0.8f, 0.4f, 0.25f);
            Handles.DrawSolidDisc(surfaceHit.point, surfaceHit.normal, currentProfile.radius);
            Handles.color = new Color(0.2f, 0.8f, 0.4f, 0.9f);
            Handles.DrawWireDisc(surfaceHit.point, surfaceHit.normal, currentProfile.radius);
            SceneView.RepaintAll();

            if (e.shift && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Brush Stroke Batch");
                int strokeGroup = Undo.GetCurrentGroup();

                for (int i = 0; i < currentProfile.burstCount; i++)
                {
                    Vector2 offset2D = UnityEngine.Random.insideUnitCircle * currentProfile.radius;
                    Vector3 samplePoint = surfaceHit.point + new Vector3(offset2D.x, 0f, offset2D.y);

                    if (Physics.Raycast(samplePoint + Vector3.up * 8f, Vector3.down, out RaycastHit hit, 20f, currentProfile.targetLayers))
                    {
                        float slope = Vector3.Angle(Vector3.up, hit.normal);
                        if (slope > currentProfile.maxSlopeAngle) continue;

                        int collisions = Physics.OverlapSphereNonAlloc(hit.point, currentProfile.minDistanceBetweenObjects, overlapBuffer, currentProfile.targetLayers);
                        if (collisions > 0) continue;

                        GameObject chosenPrefab = currentProfile.GetRandomPrefab();
                        if (chosenPrefab == null) continue;

                        GameObject spawned = (GameObject)PrefabUtility.InstantiatePrefab(chosenPrefab);
                        Undo.RegisterCreatedObjectUndo(spawned, "Spawn Prefab");

                        spawned.transform.position = hit.point;

                        if (currentProfile.alignToNormal)
                        {
                            Quaternion surfaceAlign = Quaternion.FromToRotation(Vector3.up, hit.normal);
                            float rotY = currentProfile.randomRotationY ? UnityEngine.Random.Range(0f, 360f) : 0f;
                            spawned.transform.rotation = surfaceAlign * Quaternion.Euler(currentProfile.rotationOffset.x, rotY, currentProfile.rotationOffset.z);
                        }
                        else
                        {
                            float rotY = currentProfile.randomRotationY ? UnityEngine.Random.Range(0f, 360f) : 0f;
                            spawned.transform.rotation = Quaternion.Euler(currentProfile.rotationOffset.x, rotY, currentProfile.rotationOffset.z);
                        }

                        spawned.transform.localScale = chosenPrefab.transform.localScale * UnityEngine.Random.Range(currentProfile.minScale, currentProfile.maxScale);
                    }
                }

                Undo.CollapseUndoOperations(strokeGroup);
                e.Use();
            }
        }
    }
}
#endif