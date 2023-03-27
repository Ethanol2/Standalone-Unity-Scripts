#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace VRVision.Toolkit.TransformAnimator
{
    public partial class TransformAnimator
    {
        [SerializeField] protected bool showAnchors = true;
        [SerializeField] protected bool showLines = true;
        [SerializeField] protected bool showHandles = true;

        [SerializeField] protected List<Transform> pathToRoot;

        #region Editor Functions

        protected virtual void EditorInitializer()
        {
            if (!visualizer)
            {
                visualizer = FindObjectOfType<TransformAnimatorVisualizer>();
                if (!visualizer)
                {
                    int response = EditorUtility.DisplayDialogComplex(
                        "Missing the TransformAnimatorVisualizer",
                        "The TransformAnimator could not find the Transform Animator Visualizer",
                        "Instantiate One",
                        "Scour the Scene",
                        "Cancel"
                        );

                    if (response == 0)
                    {
                        var results = AssetDatabase.FindAssets("Transform Animator Anchor t:prefab");

                        GameObject anchorPrefab = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(results[0]), typeof(GameObject)) as GameObject;
                        visualizer = (PrefabUtility.InstantiatePrefab(anchorPrefab) as GameObject).GetComponent<TransformAnimatorVisualizer>();
                        if (!visualizer)
                        {
                            Debug.LogError($"{this.name}: The transform animator failed to instantiate a new Anchor. Is there an issue with the Transform Animator resources folder?");
                            DestroyImmediate(this);
                            return;
                        }
                    }
                    else if (response == 1)
                    {
                        visualizer = LocateTheVisualizer();

                        if (!visualizer)
                        {
                            if (EditorUtility.DisplayDialog("Still Missing the TransformAnimatorVisualizer", "There is no Visualizer in this scene", "Instantiate One", "Cancel"))
                            {
                                var results = AssetDatabase.FindAssets("Transform Animator Anchor t:prefab");

                                GameObject anchorPrefab = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(results[0]), typeof(GameObject)) as GameObject;
                                visualizer = (PrefabUtility.InstantiatePrefab(anchorPrefab) as GameObject).GetComponent<TransformAnimatorVisualizer>();
                                if (!visualizer)
                                {
                                    Debug.LogError($"{this.name}: The transform animator failed to instantiate a new Anchor. Is there an issue with the Transform Animator resources folder?");
                                    DestroyImmediate(this);
                                    return;
                                }
                            }
                            else
                            {
                                Debug.LogError($"{this.name}: The transform animator failed to find an Anchor. Are you sure there's one in the scene?");
                                DestroyImmediate(this);
                                return;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError($"{this.name}: No GameObject of type TransformAnimatorVisulizer found.\n Make sure the Anchor prefab exists in the scene.");
                        DestroyImmediate(this);
                        return;
                    }
                }
            }

            if (pathToRoot is null)
            {
                pathToRoot = BuildPathToRoot(this.transform.parent, visualizer.transform);
            }
            else if (pathToRoot.Count == 0)
            {
                pathToRoot = BuildPathToRoot(this.transform.parent, visualizer.transform);
            }

            if (!startTransform && !endTransform)
            {
                midpoints.Clear();
                Transform lastObj = null;
                foreach (Transform obj in pathToRoot)
                {
                    if (!lastObj) 
                    { 
                        lastObj = obj;
                        continue;
                    }

                    if (lastObj.childCount > 0)
                    {
                        bool match = false;
                        foreach (Transform child in lastObj)
                        {
                            if (child.name == obj.name)
                            {
                                lastObj = child;
                                match = true;
                                break;
                            }
                        }
                        if (match) continue;
                    }

                    GameObject node = new GameObject(obj.name);
                    node.transform.SetParent(lastObj);
                    node.transform.SetSiblingIndex(obj.GetSiblingIndex());
                    node.transform.localPosition = obj.localPosition;
                    node.transform.localRotation = obj.localRotation;
                    node.transform.localScale    = obj.localScale;
                    lastObj = node.transform;
                }

                Mesh mesh = this.GetComponent<MeshFilter>() ? this.GetComponent<MeshFilter>().sharedMesh : visualizer.tempMesh;

                startTransform = GenerateAnchor(lastObj, mesh, visualizer.startMat, $"{this.name} Start");
                startingAnchor = startTransform;
                endTransform = GenerateAnchor(lastObj, mesh, visualizer.endMat, $"{this.name} End");

                evenSegmentSize = 1.0f / (NumOfMidpointAnchors + 1.0f);
            }
        }
        protected virtual Transform AddMidpoint(Transform prior)
        {
            Mesh mesh = this.GetComponent<MeshFilter>() ? this.GetComponent<MeshFilter>().sharedMesh : visualizer.tempMesh;
            Transform newPoint = GenerateAnchor(startTransform.parent, mesh, visualizer.midMat, $"{this.name} Midpoint {midpoints.Count + 1}");

            if (midpoints.Contains(prior))
            {
                int newIndex = midpoints.IndexOf(prior) + 1;
                midpoints.Insert(newIndex, newPoint);
                for (int k = 0; k < midpoints.Count; k++)
                {
                    midpoints[k].name = $"{this.name} Midpoint {k + 1}";
                }
            }
            else if (prior == startTransform)
            {
                midpoints.Insert(0, newPoint);
                for (int k = 0; k < midpoints.Count; k++)
                {
                    midpoints[k].name = $"{this.name} Midpoint {k + 1}";
                }
            }
            else
            {
                midpoints.Add(newPoint);
            }


            CalculatePathLength();

            EditorUtility.SetDirty(newPoint);
            EditorUtility.SetDirty(this);
            return newPoint;
        }
        protected virtual void RemoveMidpoint()
        {
            if (midpoints.Count == 0) { return; }
            Transform toDestroy = midpoints[midpoints.Count - 1];
            RemoveMidpoint(toDestroy);
        }
        protected virtual void RemoveMidpoint(Transform toDestroy)
        {
            midpoints.Remove(toDestroy);

            EditorUtility.SetDirty(this);
            DestroyImmediate(toDestroy.gameObject);
        }
        private TransformAnimatorVisualizer LocateTheVisualizer()
        {
            TransformAnimatorVisualizer TAV = GameObject.FindObjectOfType<TransformAnimatorVisualizer>();

            if (TAV)
            {
                Debug.Log($"TAV Located => {TAV.name}");
                Selection.activeGameObject = TAV.gameObject;
                return TAV;
            }

            GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject rootObject in rootObjects)
            {
                TAV = RecursivelyFindTheVisualizer(rootObject.transform);
                if (TAV) break;
            }

            if (TAV)
            {
                Debug.Log($"{this.name}: TAV Located => {TAV.name}");
                TAV.gameObject.SetActive(true);
                Selection.activeGameObject = TAV.gameObject;
            }
            return TAV;
        }
        private TransformAnimatorVisualizer RecursivelyFindTheVisualizer(Transform parent)
        {
            TransformAnimatorVisualizer TAV = parent.GetComponent<TransformAnimatorVisualizer>();
            if (TAV) { return TAV; }
            if (parent.childCount == 0) { return null; }

            foreach (Transform child in parent)
            {
                TAV = RecursivelyFindTheVisualizer(child);
                if (TAV) break;
            }

            return TAV;
        }
        protected List<Transform> BuildPathToRoot(Transform parent, Transform visualizer, Transform finish = null)
        {
            List<Transform> path = new List<Transform>();
            while (parent != finish)
            {
                path.Add(parent);
                parent = parent.parent;
            }
            path.Add(visualizer);
            path.Reverse();
            return path;
        }
        private Transform GenerateAnchor(Transform parent, Mesh mesh, Material mat, string name = "Anchor")
        {
            GameObject anchor = new GameObject(name);
            MeshFilter meshFilter = anchor.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = anchor.AddComponent<MeshRenderer>();
            Transform transform = anchor.transform;

            transform.SetParent(parent);
            transform.localPosition = this.transform.localPosition;
            transform.localRotation = this.transform.localRotation;
            transform.localScale = this.transform.localScale;

            meshFilter.sharedMesh = mesh;
            meshRenderer.material = mat;

            return transform;
        }

        protected virtual void Reset()
        {
            visualizer = null;
            startTransform = endTransform = null;
            midpoints.Clear();

            EditorInitializer();
        }

        #endregion

        [CustomEditor(typeof(TransformAnimator))]
        public class TransformAnimatorEditor : Editor
        {
            protected TransformAnimator TA { get { return target as TransformAnimator; } }

            // Editor Variables

            static protected bool testingPath = false;
            static protected Transform selectedAnchor = null;

            static private Dictionary<Transform, string> anchorLabels;
            static private List<string> stateNamesList;

            public const float HANDLE_SIZE = 0.1f;

            // Play Settings
            static private bool subbedToUpdate = false;
            static private bool isPlaying = false;
            static private bool playingForward = true;

            static private Vector2 playModeSettings = new Vector2(0f, 1f);

            // Foldouts
            static private bool readOnlyFoldout = false;
            static private bool unityEventsFoldout = false;

            #region Serialized Properties

            protected SerializedProperty timeValue;
            protected SerializedProperty showLines;
            protected SerializedProperty showAnchors;
            protected SerializedProperty showHandles;

            protected SerializedProperty playForwardOnAwake, playReverseOnAwake;
            protected SerializedProperty trueDistances;
            protected SerializedProperty startDelay;
            protected SerializedProperty animationTime;
            protected SerializedProperty startPoint;
            protected SerializedProperty easeMode;
            protected SerializedProperty easeSetting;
            protected SerializedProperty repeatMode;
            protected SerializedProperty pingPongDelay;

            protected SerializedProperty constraintsObject;

            protected SerializedProperty midpoints;
            protected SerializedProperty segmentSizes;
            protected SerializedProperty segmentPositions;
            protected SerializedProperty splineSegments;

            protected SerializedProperty onPlayStarted;
            protected SerializedProperty onPlayFinished;

            #endregion

            // Logic Properties
            private int StartPoint
            {
                get
                {
                    return TA.GetAllAnchors().IndexOf(TA.startingAnchor);
                }
                set
                {
                    startPoint.objectReferenceValue = TA.GetAllAnchors()[value];
                }
            }

            #region Callback Functions

            private void OnDisable()
            {
                try
                {
                    ShowAnchors(false);
                    anchorLabels.Clear();
                    isPlaying = false;
                    testingPath = false;

                    if (!Application.isPlaying)
                    {
                        TA.transform.localPosition = TA.startingAnchor.localPosition;
                        TA.transform.localRotation = TA.startingAnchor.localRotation;
                        TA.transform.localScale = TA.startingAnchor.localScale;
                    }
                }
                catch { }
            }
            private void OnDestroy()
            {
                ShowAnchors(false);
            }
            protected virtual void OnSceneGUI()
            {
                if (TA.showLines)
                {
                    Quaternion up = Quaternion.LookRotation(Vector3.up);
                    if (Application.isPlaying)
                    {
                        List<Transform> anchors = TA.GetAllAnchors();
                        SplineSegment spline;

                        for (int i = 0; i < anchors.Count - 1; i++)
                        {
                            if (TA.CheckIfAnchorHasASpline(anchors[i], out spline, out int index))
                            {
                                DrawSplineLines(spline);
                            }
                            else
                            {
                                Handles.DrawLine(anchors[i].position, anchors[i + 1].position);
                            }

                            Handles.RectangleHandleCap(
                                i, 
                                anchors[i].position,
                                anchors[i].rotation * up, 
                                HandleUtility.GetHandleSize(anchors[i].position) * HANDLE_SIZE, 
                                EventType.Repaint);
                        }

                        Handles.RectangleHandleCap(
                            anchors.Count,
                            TA.endTransform.position,
                            TA.endTransform.rotation * up,
                            HandleUtility.GetHandleSize(TA.endTransform.position) * HANDLE_SIZE,
                            EventType.Repaint);
                    }
                    else
                    {
                        List<Transform> anchors = TA.GetAllAnchors();
                        SplineSegment spline;

                        for (int i = 0; i < anchors.Count - 1; i++)
                        {
                            Handles.RectangleHandleCap(
                                i, 
                                anchors[i].position,
                                anchors[i].rotation * up, 
                                HandleUtility.GetHandleSize(anchors[i].position) * HANDLE_SIZE, 
                                EventType.Repaint);

                            if (TA.CheckIfAnchorHasASpline(anchors[i], out spline, out int index))
                            {
                                DrawSplineLines(spline);
                                if (anchors[i] == selectedAnchor)
                                {
                                    EditSplineHandles(spline, index);
                                }
                            }
                            else
                            {
                                Handles.DrawLine(anchors[i].position, anchors[i + 1].position);
                            }
                        }

                        Handles.RectangleHandleCap(
                            anchors.Count, 
                            TA.endTransform.position, 
                            TA.endTransform.rotation * up, 
                            HandleUtility.GetHandleSize(TA.endTransform.position) * HANDLE_SIZE, 
                            EventType.Repaint);
                    }
                }

                if (!Application.isPlaying)
                {
                    if (!testingPath && selectedAnchor) UpdateAnchorPositions(selectedAnchor);
                }
            }

            public override VisualElement CreateInspectorGUI()
            {
                GetSerializedProperties();
                TA.EditorInitializer();

                selectedAnchor = InferAnchorFromTime(TA.timeValue);
                if (!testingPath) { SetToSelection(); }

                UpdateAnchorLabels();
                ShowAnchors(TA.showAnchors);

                if (!TA.startingAnchor)
                {
                    StartPoint = 0;
                }

                testingPath = false;

                UnsubscribeToUpdate();

                return base.CreateInspectorGUI();
            }
            public override void OnInspectorGUI()
            {
                if (Application.isPlaying)
                {
                    PlayModeFunctions();

                    GUILayout.Space(5f);
                    Constraints();

                    GUILayout.Space(5f);
                    UnityEvents();

                    GUILayout.Space(5f);
                    ReadOnlyInfo();
                }
                else
                {
                    EditorTools();

                    GUILayout.Space(5f);
                    AnimationSettings();

                    GUILayout.Space(5f);
                    Constraints();

                    GUILayout.Space(5f);
                    SplineSettings();

                    GUILayout.Space(5f);
                    UnityEvents();

                    GUILayout.Space(5f);
                    ReadOnlyInfo();

                }
                serializedObject.ApplyModifiedProperties();
            }

            private void Update()
            {
                if (TA is null) { Destroy(this); }
                if (!selectedAnchor) { selectedAnchor = TA.startTransform; }
                if (!testingPath) { UnsubscribeToUpdate(); return; }

                if (Application.isPlaying) 
                {
                    testingPath = false;
                    UnsubscribeToUpdate();
                    return; 
                }

                if (isPlaying)
                {
                    float timeAdd = (Time.deltaTime / TA.animationTime) * (playingForward ? 1f : -1f);
                    timeValue.floatValue = Mathf.Clamp01(TA.timeValue + timeAdd);

                    if (Mathf.Approximately(TA.timeValue, 1f) && playingForward)
                    {
                        if (TA.repeatMode == RepeatMode.PingPong)
                        {
                            playingForward = false;
                        }
                        else if (TA.repeatMode == RepeatMode.Loop)
                        {
                            timeValue.floatValue = 0f;
                        }
                        else
                        {
                            isPlaying = false;
                            selectedAnchor = InferAnchorFromTime(TA.timeValue);
                        }
                    }
                    else if (Mathf.Approximately(TA.timeValue, 0f) && !playingForward)
                    {
                        if (TA.repeatMode == RepeatMode.PingPong)
                        {
                            playingForward = true;
                        }
                        else if (TA.repeatMode == RepeatMode.Loop)
                        {
                            timeValue.floatValue = 1f;
                        }
                        else
                        {
                            isPlaying = false;
                            selectedAnchor = InferAnchorFromTime(TA.timeValue);
                        }
                    }
                }

                TA.SetPositionBasedOnTime(timeValue.floatValue, TA.GetAllAnchors().ToArray());

                serializedObject.ApplyModifiedProperties();
            }
            private void UpdateAnchorPositions(Transform anchor)
            {
                bool changed = false;
                if (anchor.localPosition != TA.transform.localPosition)
                {
                    anchor.localPosition = TA.transform.localPosition;
                    changed = true;
                }
                if (anchor.localEulerAngles != TA.transform.localEulerAngles)
                {
                    anchor.localEulerAngles = TA.transform.localEulerAngles;
                    changed = true;
                }
                if (anchor.localScale != TA.transform.localScale)
                {
                    anchor.localScale = TA.transform.localScale;
                    changed = true;
                }

                if (changed) 
                {
                    if (TA.UseTrueDistances)
                    {
                        TA.SetTimeBasedOnAnchor(selectedAnchor);
                    }
                    else
                    {
                        TA.CalculatePathLength();
                    }
                    EditorUtility.SetDirty(TA);
                    EditorUtility.SetDirty(anchor);
                }
            }

            #endregion

            #region Inspector GUI

            private void PlayModeFunctions()
            {
                EditorGUILayout.Slider("Time Value", TA.timeValue, 0.0f, 1.0f);

                GUILayout.Space(5f);

                showLines.boolValue = GUILayout.Toggle(TA.showLines, "Show Lines");
                float aTime = EditorGUILayout.FloatField(new GUIContent("Animation Time (s)", "Time in seconds it takes for the animation to complete"), TA.animationTime);
                animationTime.floatValue = aTime;
                EditorGUILayout.PropertyField(repeatMode); 
                EditorGUILayout.PropertyField(easeMode);

                GUILayout.Space(5f);

                EditorGUILayout.BeginHorizontal();
                playModeSettings.x = EditorGUILayout.FloatField("Start", playModeSettings.x);
                playModeSettings.y = EditorGUILayout.FloatField("End", playModeSettings.y);
                EditorGUILayout.EndHorizontal();
                
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Play"))
                {
                    TA.Play(playModeSettings.x, playModeSettings.y);
                }
                if (GUILayout.Button("Stop"))
                {
                    TA.Stop();
                }

                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10f);

                if (GUILayout.Button("Toggle Play"))
                {
                    TA.PlayToggle();
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Play Forward"))
                {
                    TA.PlayForward();
                }
                if (GUILayout.Button("Play Forward From Start"))
                {
                    TA.PlayForward();
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Play Reverse"))
                {
                    TA.PlayReverse();
                }
                if (GUILayout.Button("Play Reverse From End"))
                {
                    TA.PlayReverseFromEnd();
                }
                GUILayout.EndHorizontal();

            }

            private void EditorTools()
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("EDITOR - " + (testingPath ? "Lerp Testing" : $"Anchor Modification ({anchorLabels[selectedAnchor]})"), EditorStyles.boldLabel);

                if (GUILayout.Button("Toggle", GUILayout.Width(50)))
                {
                    testingPath = !testingPath;
                    selectedAnchor = InferAnchorFromTime(TA.timeValue);
                    SetToTransform(selectedAnchor);

                    if (testingPath) { SubscribeToUpdate(); }
                    else { UnsubscribeToUpdate(); }
                }
                EditorGUILayout.EndHorizontal();

                #region TIMELINE
                EditorGUI.BeginDisabledGroup(!testingPath);
                timeValue.floatValue = EditorGUILayout.Slider(timeValue.floatValue, 0.0f, 1.0f);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.BeginHorizontal();

                // Go to start
                if (GUILayout.Button("|<<"))
                {
                    selectedAnchor = TA.startTransform;
                    timeValue.floatValue = 0f;
                    SetToSelection();
                }

                // Back a midpoint
                if (GUILayout.Button("<<"))
                {
                    ChangeSelection(false);
                }

                if (isPlaying && testingPath)
                {
                    // Stop
                    if (GUILayout.Button("Stop"))
                    {
                        isPlaying = false;
                    }
                }
                else
                {
                    // Play Reverse
                    if (GUILayout.Button("Play Reverse"))
                    {
                        testingPath = true;
                        isPlaying = true;
                        playingForward = false;
                        SubscribeToUpdate();
                    }

                    // Play Forward
                    if (GUILayout.Button("Play Forward"))
                    {
                        testingPath = true;
                        isPlaying = true;
                        playingForward = true;
                        SubscribeToUpdate();
                    }
                }

                // Forward a midpoint
                if (GUILayout.Button(">>"))
                {
                    ChangeSelection(true);
                }
                // Go to end
                if (GUILayout.Button(">>|"))
                {
                    selectedAnchor = TA.endTransform;
                    timeValue.floatValue = 1f;
                    SetToSelection();
                }

                GUILayout.EndHorizontal();
                                
                #endregion

                GUILayout.Space(15f);
                showLines.boolValue = GUILayout.Toggle(TA.showLines, "Show Lines");

                if (TA.visualizer.gameObject.activeInHierarchy)
                {
                    bool _ShowAnchors = GUILayout.Toggle(showAnchors.boolValue, "Show Anchors");
                    if (TA.showAnchors != _ShowAnchors)
                    {
                        showAnchors.boolValue = _ShowAnchors;
                        ShowAnchors(showAnchors.boolValue);
                    }
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);

                    GUILayout.Toggle(showAnchors.boolValue, new GUIContent("Show Anchors", "Transform Animator Anchor is disabled"));

                    EditorGUI.EndDisabledGroup();
                }

                if (TA.CheckIfAnchorHasASpline(selectedAnchor))
                {
                    showHandles.boolValue = GUILayout.Toggle(TA.showHandles, "Show Spline Handles");
                }
                EditorUtility.SetDirty(TA.gameObject);
                SceneView.RepaintAll();
            }
            private void AnimationSettings()
            {
                EditorGUILayout.LabelField("ANIMATION SETTINGS", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal();
                bool playOnAwake = EditorGUILayout.Toggle("Play On Awake", playForwardOnAwake.boolValue || playReverseOnAwake.boolValue);

                if (playOnAwake)
                {
                    int dir = GUILayout.Toolbar(playReverseOnAwake.boolValue ? 1 : 0, new string[] { "Forward", "Reverse" });
                    playReverseOnAwake.boolValue = dir == 1;
                    playForwardOnAwake.boolValue = dir == 0;
                }
                else
                {
                    playReverseOnAwake.boolValue = false;
                    playForwardOnAwake.boolValue = false;
                }
                GUILayout.EndHorizontal();

                bool trueD = EditorGUILayout.Toggle(new GUIContent(trueDistances.displayName, trueDistances.tooltip), trueDistances.boolValue);
                if (trueD != trueDistances.boolValue)
                {
                    if (trueD)
                    {
                        List<Transform> anchors = TA.GetAllAnchors();
                        bool dupPositions = false;
                        foreach (Transform anchor1 in anchors)
                        {
                            foreach (Transform anchor2 in anchors)
                            {
                                if (anchor1 == anchor2) { continue; }
                                if (anchor1.localPosition == anchor2.localPosition)
                                {
                                    dupPositions = true;
                                    break;
                                }
                            }
                            if (dupPositions) break;
                        }
                        if (dupPositions)
                        {
                            EditorUtility.DisplayDialog("Can't Enable True Distances", "True distances can't be enabled if any of the anchors share a position", "ok");
                        }
                        else
                        {
                            trueDistances.boolValue = true;
                            TA.SetTimeBasedOnAnchor(selectedAnchor);
                        }
                    }
                    else
                    {
                        trueDistances.boolValue = trueD;
                    }
                }

                EditorGUILayout.PropertyField(startDelay);
                float aTime = EditorGUILayout.FloatField(new GUIContent("Animation Time (s)", "Time in seconds it takes for the animation to complete"), TA.animationTime);
                animationTime.floatValue = aTime;


                #region Midpoint Logic

                EditorGUI.BeginDisabledGroup(testingPath);

                GUILayout.BeginHorizontal();
                int newMidcount = EditorGUILayout.IntField("Midpoints", TA.NumOfMidpointAnchors);
                if (newMidcount > TA.midpoints.Count)
                {
                    int oldCount = TA.midpoints.Count;
                    for (int k = 0; k < newMidcount - oldCount; k++)
                    {
                        TA.AddMidpoint(selectedAnchor);
                    }
                    UpdateAnchorLabels();
                    TA.SetTimeBasedOnAnchor(selectedAnchor);
                    serializedObject.Update();
                }
                else if (newMidcount < TA.midpoints.Count)
                {
                    int oldCount = TA.midpoints.Count;
                    for (int k = 0; k < oldCount - newMidcount; k++)
                    {
                        TA.RemoveMidpoint();
                    }
                    UpdateAnchorLabels();
                    TA.CalculatePathLength();

                    if (selectedAnchor == null)
                    {
                        selectedAnchor = TA.startTransform;
                        timeValue.floatValue = 0f;
                        SetToSelection();
                    }
                    TA.SetTimeBasedOnAnchor(selectedAnchor);
                }
                if (GUILayout.Button("-"))
                {
                    if (selectedAnchor != TA.startTransform && selectedAnchor != TA.endTransform)
                    {
                        TA.RemoveMidpoint(selectedAnchor);
                    }
                    else
                    {
                        TA.RemoveMidpoint();
                    }
                    UpdateAnchorLabels();
                    TA.CalculatePathLength();

                    if (selectedAnchor == null)
                    {
                        selectedAnchor = TA.startTransform;
                        timeValue.floatValue = 0f;
                        SetToSelection();
                    }
                    TA.SetTimeBasedOnAnchor(selectedAnchor);
                    serializedObject.Update();
                }
                if (GUILayout.Button("+"))
                {
                    selectedAnchor = TA.AddMidpoint(selectedAnchor);
                    UpdateAnchorLabels();
                    TA.SetTimeBasedOnAnchor(selectedAnchor);
                    serializedObject.Update();
                }
                GUILayout.EndHorizontal();

                EditorGUI.EndDisabledGroup();

                #endregion

                StartPoint = EditorGUILayout.Popup("Start Point", StartPoint, stateNamesList.ToArray());

                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(easeMode);
                easeSetting.boolValue = EditorGUILayout.Popup(easeSetting.boolValue ? 1 : 0, new string[] { "Ease at Ends", "Ease at all Points" }, GUILayout.Width(150f)) == 1;
                GUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(repeatMode);
                if (TA.repeatMode == RepeatMode.PingPong)
                {
                    EditorGUI.indentLevel += 3;
                    EditorGUILayout.PropertyField(pingPongDelay);
                    EditorGUI.indentLevel -= 3;
                }
            }
            private void SplineSettings()
            {
                GUILayout.Label("SPLINE SETTINGS", EditorStyles.boldLabel);

                if (selectedAnchor == TA.endTransform) 
                {
                    GUILayout.Label("Spline can't be created from the end of a path");
                    return; 
                }

                if (TA.CheckIfAnchorHasASpline(selectedAnchor, out SplineSegment spline, out int index))
                {
                    GUILayout.BeginHorizontal();

                    bool isSingle = spline.SingleHandle;

                    spline.SingleHandle = GUILayout.Toolbar(spline.SingleHandle ? 0 : 1, new string[] { "Single Handle", "Double Handle" }) == 0;

                    if (spline.SingleHandle != isSingle)
                    {
                        if (spline.SingleHandle)
                        {
                            spline.Handle2 = Vector3.zero;
                        }
                        else
                        {
                            spline.Handle2 = Vector3.Lerp(spline.Handle1, spline.End.position, 0.5f);
                        }

                        TA.splineSegments[index] = spline;
                        EditorUtility.SetDirty(TA);
                        serializedObject.Update();
                    }

                    GUILayout.EndHorizontal();

                    if (GUILayout.Button("Remove Spline"))
                    {
                        splineSegments.DeleteArrayElementAtIndex(index);
                        EditorUtility.SetDirty(TA);
                    }
                }
                else
                {
                    if (GUILayout.Button($"Create a Spline from {selectedAnchor.name}"))
                    {
                        int newIndex = TA.splineSegments.Count;
                        splineSegments.InsertArrayElementAtIndex(newIndex);

                        serializedObject.ApplyModifiedProperties();

                        List<Transform> anchors = TA.GetAllAnchors();

                        Transform end = anchors[anchors.IndexOf(selectedAnchor) + 1];
                        Vector3 handle1;

                        handle1 = Vector3.Lerp(selectedAnchor.position, end.position, 0.5f);

                        TA.splineSegments[newIndex] = new SplineSegment(selectedAnchor, end, handle1);

                        EditorUtility.SetDirty(TA);
                    }
                }

            }
            private void Constraints()
            {
                EditorGUILayout.LabelField("LERP CONSTRAINTS", EditorStyles.boldLabel);

                TAConstraints constraints = TA.Constraints; 

                //Position
                EditorGUILayout.BeginHorizontal();
                bool status;
                bool displayToggle = status = constraints.TranslateX && constraints.TranslateY && constraints.TranslateZ;
                displayToggle = !EditorGUILayout.Toggle("Freeze Position", !displayToggle);

                if (status != displayToggle)
                {
                    constraints.TranslateX =
                    constraints.TranslateY =
                    constraints.TranslateZ = displayToggle;
                }

                constraints.TranslateX = !GUILayout.Toggle(!constraints.TranslateX, "X", GUI.skin.button);
                constraints.TranslateY = !GUILayout.Toggle(!constraints.TranslateY, "Y", GUI.skin.button);
                constraints.TranslateZ = !GUILayout.Toggle(!constraints.TranslateZ, "Z", GUI.skin.button);
                EditorGUILayout.EndHorizontal();
                //--------

                //Rotation

                EditorGUI.BeginDisabledGroup(constraints.UseQuaternions);
                EditorGUILayout.BeginHorizontal();
                displayToggle = status = constraints.RotateX && constraints.RotateY && constraints.RotateZ;
                displayToggle = !EditorGUILayout.Toggle("Freeze Rotation", !displayToggle);

                if (status != displayToggle)
                {
                    constraints.RotateX =
                    constraints.RotateY =
                    constraints.RotateZ = displayToggle;
                }

                constraints.RotateX = !GUILayout.Toggle(!constraints.RotateX, "X", GUI.skin.button);
                constraints.RotateY = !GUILayout.Toggle(!constraints.RotateY, "Y", GUI.skin.button);
                constraints.RotateZ = !GUILayout.Toggle(!constraints.RotateZ, "Z", GUI.skin.button);
                EditorGUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
                //--------

                //Scale
                EditorGUILayout.BeginHorizontal();
                displayToggle = status = constraints.ScaleX && constraints.ScaleY && constraints.ScaleZ;
                displayToggle = !EditorGUILayout.Toggle("Freeze Scale", !displayToggle);

                if (status != displayToggle)
                {
                    constraints.ScaleX =
                    constraints.ScaleY =
                    constraints.ScaleZ = displayToggle;
                }

                constraints.ScaleX = !GUILayout.Toggle(!constraints.ScaleX, "X", GUI.skin.button);
                constraints.ScaleY = !GUILayout.Toggle(!constraints.ScaleY, "Y", GUI.skin.button);
                constraints.ScaleZ = !GUILayout.Toggle(!constraints.ScaleZ, "Z", GUI.skin.button);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(3f);
                constraints.UseQuaternions = EditorGUILayout.Toggle("Rotate Using Quaternions", constraints.UseQuaternions);

                constraintsObject.managedReferenceValue = constraints;
            }
            private void UnityEvents()
            {
                unityEventsFoldout = EditorGUILayout.Foldout(unityEventsFoldout, "EVENTS", true);
                if (unityEventsFoldout)
                {
                    EditorGUILayout.PropertyField(onPlayStarted);
                    EditorGUILayout.PropertyField(onPlayFinished);

                    if (!TA.GetComponent<TransformAnimatorEventExtension>())
                    {
                        GUILayout.Space(5f);
                        if (GUILayout.Button("Add Event Extension Component"))
                        {
                            TA.gameObject.AddComponent<TransformAnimatorEventExtension>();
                        }
                    }
                }
            }
            private void ReadOnlyInfo()
            {
                readOnlyFoldout = EditorGUILayout.Foldout(readOnlyFoldout, "READ ONLY", true);
                if (readOnlyFoldout)
                {
                    EditorGUI.indentLevel += 2;

                    EditorGUILayout.FloatField("Path Length", TA.segments.PathLength);
                    EditorGUILayout.FloatField("Even Segment Size", TA.evenSegmentSize);
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(segmentSizes);
                    EditorGUILayout.PropertyField(segmentPositions);
                    EditorGUILayout.PropertyField(splineSegments);

                    EditorGUI.EndDisabledGroup();
                    EditorGUI.indentLevel -= 2;
                    EditorGUILayout.LabelField("ANCHORS", EditorStyles.boldLabel);
                    EditorGUI.indentLevel += 2;
                    EditorGUI.BeginDisabledGroup(true);

                    EditorGUILayout.ObjectField(TA.startTransform, typeof(Transform), true);
                    EditorGUILayout.PropertyField(midpoints);
                    EditorGUILayout.ObjectField(TA.endTransform, typeof(Transform), true);

                    EditorGUI.EndDisabledGroup();


                    EditorGUI.indentLevel -= 2;
                }
            }

            #endregion

            #region Utility Functions

            private void GetSerializedProperties()
            {
                timeValue = serializedObject.FindProperty("timeValue");
                showLines = serializedObject.FindProperty("showLines");
                showAnchors = serializedObject.FindProperty("showAnchors");
                showHandles = serializedObject.FindProperty("showHandles");

                playForwardOnAwake = serializedObject.FindProperty("PlayForwardOnAwake");
                playReverseOnAwake = serializedObject.FindProperty("PlayReverseOnAwake");
                trueDistances = serializedObject.FindProperty("UseTrueDistances");
                startDelay = serializedObject.FindProperty("StartDelay");
                animationTime = serializedObject.FindProperty("animationTime");
                startPoint = serializedObject.FindProperty("startingAnchor");
                easeMode = serializedObject.FindProperty("EaseMode");
                easeSetting = serializedObject.FindProperty("EaseAtEveryPoint");
                repeatMode = serializedObject.FindProperty("repeatMode");
                pingPongDelay = serializedObject.FindProperty("PingPongDelay");

                constraintsObject = serializedObject.FindProperty("Constraints");

                midpoints = serializedObject.FindProperty("midpoints");
                segmentPositions = serializedObject.FindProperty("segments.MidpointNormalizedPositions");
                segmentSizes = serializedObject.FindProperty("segments.SegmentNormalizedSizes");
                splineSegments = serializedObject.FindProperty("splineSegments");

                // Events
                onPlayStarted = serializedObject.FindProperty("onPlayStarted");
                onPlayFinished = serializedObject.FindProperty("onPlayFinished");
            }
            private void SubscribeToUpdate()
            {
                if (subbedToUpdate) { return; }
                EditorApplication.update += Update;
                subbedToUpdate = true;
            }
            private void UnsubscribeToUpdate()
            {
                EditorApplication.update -= Update;
                subbedToUpdate = false;
            }

            private void ChangeSelection(bool nextAnchor)
            {
                if (nextAnchor)
                {
                    if (selectedAnchor == TA.startTransform)
                    {
                        if (TA.NumOfMidpointAnchors == 0)
                        {
                            selectedAnchor = TA.endTransform;
                            timeValue.floatValue = 1f;
                        }
                        else
                        {
                            selectedAnchor = TA.midpoints[0];
                            timeValue.floatValue = TA.SetTimeBasedOnAnchor(selectedAnchor);
                        }
                    }
                    else if (selectedAnchor != TA.endTransform)
                    {
                        int index = TA.midpoints.IndexOf(selectedAnchor);
                        index++;

                        if (index >= TA.midpoints.Count)
                        {
                            selectedAnchor = TA.endTransform;
                            timeValue.floatValue = 1f;
                        }
                        else
                        {
                            selectedAnchor = TA.midpoints[index];
                            timeValue.floatValue = TA.SetTimeBasedOnAnchor(selectedAnchor);
                        }
                    }
                }
                else
                {
                    if (selectedAnchor == TA.endTransform)
                    {
                        if (TA.NumOfMidpointAnchors == 0)
                        {
                            selectedAnchor = TA.startTransform;
                            timeValue.floatValue = 0f;
                        }
                        else
                        {
                            selectedAnchor = TA.midpoints[TA.midpoints.Count - 1];
                            timeValue.floatValue = TA.SetTimeBasedOnAnchor(selectedAnchor);
                        }
                    }
                    else if (selectedAnchor != TA.startTransform)
                    {
                        int index = TA.midpoints.IndexOf(selectedAnchor);
                        index--;

                        if (index < 0)
                        {
                            selectedAnchor = TA.startTransform;
                            timeValue.floatValue = 0f;
                        }
                        else
                        {
                            selectedAnchor = TA.midpoints[index];
                            timeValue.floatValue = TA.SetTimeBasedOnAnchor(selectedAnchor);
                        }
                    }
                }

                SetToSelection();
            }
            private void SetToTransform(Transform target)
            {
                if (isPlaying)
                {
                    return;
                }

                TA.transform.localPosition = 
                    new Vector3( 
                        TA.Constraints.TranslateX ? target.localPosition.x : TA.startingAnchor.localPosition.x,
                        TA.Constraints.TranslateY ? target.localPosition.y : TA.startingAnchor.localPosition.y,
                        TA.Constraints.TranslateZ ? target.localPosition.z : TA.startingAnchor.localPosition.z);

                if (TA.Constraints.UseQuaternions) TA.transform.localRotation = target.localRotation;
                else
                {
                    TA.transform.localEulerAngles =
                        new Vector3(
                            TA.Constraints.RotateX ? target.localEulerAngles.x : TA.startingAnchor.localEulerAngles.x,
                            TA.Constraints.RotateY ? target.localEulerAngles.y : TA.startingAnchor.localEulerAngles.y,
                            TA.Constraints.RotateZ ? target.localEulerAngles.z : TA.startingAnchor.localEulerAngles.z
                            );
                }

                TA.transform.localScale =
                    new Vector3(
                        TA.Constraints.ScaleX ? target.localScale.x : TA.startingAnchor.localScale.x,
                        TA.Constraints.ScaleY ? target.localScale.y : TA.startingAnchor.localScale.y,
                        TA.Constraints.ScaleZ ? target.localScale.z : TA.startingAnchor.localScale.z
                        );
            }
            private void SetToSelection()
            {
                SetToTransform(selectedAnchor);
            }
            private Transform InferAnchorFromTime(float time)
            {
                if (TA.NumOfMidpointAnchors == 0)
                {
                    if (time > 0.49999f)
                    {
                        return TA.endTransform;
                    }
                    else
                    {
                        return TA.startTransform;
                    }
                }

                if (time < TA.evenSegmentSize / 2f) { return TA.startTransform; }
                if (time > 1f - (TA.evenSegmentSize / 2f)) { return TA.endTransform; }

                int index = Mathf.Clamp(Mathf.RoundToInt((time * (TA.NumOfMidpointAnchors + 1f)) - 1f), 0, TA.NumOfMidpointAnchors - 1);
                return TA.midpoints[index];
            }

            private void UpdateAnchorLabels()
            {
                anchorLabels = new Dictionary<Transform, string>()
                {
                    {TA.startTransform, "START" }, {TA.endTransform, "END"}
                };
                stateNamesList = new List<string>()
                {
                    "Start"
                };

                int i = 1;
                foreach (Transform mid in TA.midpoints)
                {
                    anchorLabels.Add(mid, $"Midpoint {i}");
                    stateNamesList.Add($"Midpoint {i}");
                    i++;
                }

                stateNamesList.Add("End");
            }
            private void ShowAnchors(bool enabled)
            {
                if (TA.startTransform) TA.startTransform.gameObject.SetActive(enabled);
                if (TA.endTransform) TA.endTransform.gameObject.SetActive(enabled);
                foreach (Transform midpoint in TA.midpoints)
                {
                    if (midpoint) midpoint.gameObject.SetActive(enabled);
                }
            }

            protected void EditSplineHandles(SplineSegment spline, int arrIndex)
            {
                if (TA.showHandles && !testingPath)
                {
                    spline.Handle1 = Handles.PositionHandle(spline.Handle1, Quaternion.identity);
                    if (!spline.SingleHandle)
                    {
                        spline.Handle2 = Handles.PositionHandle(spline.Handle2, Quaternion.identity);
                    }
                } 
                TA.splineSegments[arrIndex] = spline;
                serializedObject.Update();
            }
            protected void DrawSplineLines(SplineSegment spline)
            {
                if (spline.SingleHandle)
                {
                    Handles.DrawLine(spline.Start.position, spline.Handle1);
                    Handles.CircleHandleCap(0, spline.Handle1, Quaternion.LookRotation(Vector3.up), HandleUtility.GetHandleSize(spline.Handle1) * HANDLE_SIZE, EventType.Repaint);
                }
                else
                {
                    Handles.DrawLine(spline.Start.position, spline.Handle1);
                    Handles.CircleHandleCap(0, spline.Handle1, Quaternion.LookRotation(Vector3.up), HandleUtility.GetHandleSize(spline.Handle1) * HANDLE_SIZE, EventType.Repaint);
                    Handles.DrawLine(spline.End.position, spline.Handle2);
                    Handles.CircleHandleCap(0, spline.Handle2, Quaternion.LookRotation(Vector3.up), HandleUtility.GetHandleSize(spline.Handle1) * HANDLE_SIZE, EventType.Repaint);
                }

                Vector3 lastHandle = spline.SingleHandle ? spline.End.position : spline.Handle2;
                Handles.DrawBezier(spline.Start.position, spline.End.position, spline.Handle1, lastHandle, Color.white, null, 2f);
            }

            #endregion
        }
    }
}
#endif