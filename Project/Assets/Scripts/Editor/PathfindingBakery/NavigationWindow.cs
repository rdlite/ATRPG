using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using System.Text;

public class NavigationWindow : EditorWindow {
    public AStarGrid.TerrainType[] TerrainType;
    public int NotWalkableAroundPenalty;
    public LayerMask ObstacleLayerMask;
    public float NodeRadius;
    public float ObstacleAvoidance;
    public float HeightDelink;
    public float MaxSurfaceSlope;

    private int _encodingKey = 69;
    private bool _isWindowActive;
    private float _deactTimer = .001f;

    [MenuItem("Window/AI/A* Pathfinding")]
    public static void ShowWindow() {
        GetWindow(typeof(NavigationWindow));
    }

    private void OnFocus() {
        if (!_isWindowActive) {
            _isWindowActive = true;
            _deactTimer = .3f;

            FindObjectOfType<AStarGrid>().SetActiveDecal(true);
        }
    }

    private void OnInspectorUpdate() {
        if (!_isWindowActive) {
            _deactTimer -= Time.deltaTime;

            if (_deactTimer <= 0f) {
                _isWindowActive = true;
                FindObjectOfType<AStarGrid>().SetActiveDecal(true);
            }
        }
    }

    private void OnLostFocus() {
        if (_isWindowActive) {
            _isWindowActive = false;
            FindObjectOfType<AStarGrid>().SetActiveDecal(false);
        }
    }

    private void OnGUI() {
        GUILayout.Space(20);

        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty terrainProperty = so.FindProperty("TerrainType");

        EditorGUILayout.PropertyField(terrainProperty, true);
        so.ApplyModifiedProperties();

        ObstacleLayerMask = EditorGUILayout.LayerField("Obstacles mask", ObstacleLayerMask);
        GUILayout.Space(10);

        NotWalkableAroundPenalty = EditorGUILayout.IntField("Around obstacles penalties", NotWalkableAroundPenalty);
        NodeRadius = EditorGUILayout.Slider("Node Radius", NodeRadius, 0f, 2f);

        GUILayout.Space(10);

        ObstacleAvoidance = EditorGUILayout.Slider("Obstacles avoidance", ObstacleAvoidance, 0f, 1f);
        HeightDelink = EditorGUILayout.Slider("Delink height", HeightDelink, 0f, 3f);
        MaxSurfaceSlope = EditorGUILayout.Slider("Max walk slope", MaxSurfaceSlope, 0f, 90f);

        GUILayout.Space(20);

        if (GUILayout.Button("Bake")) {
            BakeField();
        }

        if (GUILayout.Button("Clear")) {
            ClearField();
        }
    }

    private void ClearField() {
        if (File.Exists(StringsContainer.NAVGRID_PATH + "/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + StringsContainer.NAVGRID_RESOLUTION)) {
            File.Delete(StringsContainer.NAVGRID_PATH + "/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + StringsContainer.NAVGRID_RESOLUTION);
        }

        AStarGrid grid = FindObjectOfType<AStarGrid>();
        grid.ClearData();
        grid.SetActiveDecal(false);
    }

    private void BakeField() {
        AStarGrid grid = FindObjectOfType<AStarGrid>();

        if (grid != null) {
            BakeGrid(grid);
        } else {
            grid = Instantiate(Resources.Load("Pathfinding/AStarGrid") as GameObject).GetComponent<AStarGrid>();

            GameObject LDPoint = new GameObject("LDPoint");
            GameObject RUPoint = new GameObject("RUPoint");
            LDPoint.transform.SetParent(grid.transform);
            RUPoint.transform.SetParent(grid.transform);
            LDPoint.transform.localPosition = Vector3.zero;
            RUPoint.transform.localPosition = Vector3.zero;
            LDPoint.transform.localPosition = new Vector3(-10f, 0f, -10f);
            RUPoint.transform.localPosition = new Vector3(10f, 0f, 10f);

            grid.SetBounds(LDPoint.transform, RUPoint.transform);

            BakeGrid(grid);
        }
    }

    private void BakeGrid(AStarGrid grid) {
        GridSettings settings = new GridSettings(
                TerrainType, NotWalkableAroundPenalty, ObstacleLayerMask,
                NodeRadius, ObstacleAvoidance, HeightDelink,
                MaxSurfaceSlope);

        Node[,] nodes = grid.CreateGrid(settings);
        DataToSaveContainer<Node> nodesSaver = new DataToSaveContainer<Node>(nodes);

        GridSaveData gridSaveData = new GridSaveData();
        gridSaveData.Settings = settings;
        gridSaveData.NodesData = nodesSaver;

        string data = JsonUtility.ToJson(gridSaveData);

        if (!Directory.Exists(StringsContainer.NAVGRID_PATH)) {

            Directory.CreateDirectory(StringsContainer.NAVGRID_PATH);
        }

        byte[] byteData = Encoding.ASCII.GetBytes(data);

        for (int i = 0; i < byteData.Length; i++) {
            byteData[i] = (byte)(byteData[i] ^ _encodingKey);
        }

        grid.SetNodesText(data);

        data = Encoding.ASCII.GetString(byteData);

        File.WriteAllText(StringsContainer.NAVGRID_PATH + "/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + StringsContainer.NAVGRID_RESOLUTION, data);

        grid.SetActiveDecal(true);
    }
}