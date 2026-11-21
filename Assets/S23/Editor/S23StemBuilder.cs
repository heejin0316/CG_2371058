using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

// S23 2번: Unity Splines의 실제 SplineExtrude로 줄기를 생성한다.
// 교수님 시작 장면은 복사만 하며, 생성 결과는 Assets/S23에 저장한다.
public static class S23StemBuilder
{
    const string Root = "Assets/S23";
    const string ScenePath = Root + "/S23_Landscape.unity";

    [MenuItem("S23/2. Create Vine Stem Scene")]
    public static void Create()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play before creating S23."); return; }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        { Debug.Log("S23 scene already exists; preserved without rebuilding."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        foreach (string name in new[] { "Materials", "Meshes", "Prefabs" })
            if (!AssetDatabase.IsValidFolder(Root + "/" + name)) AssetDatabase.CreateFolder(Root, name);
        AssetDatabase.CopyAsset("Assets/S23_BilinearPatch/S23_SplineExtrude_Stem_Finish.unity", ScenePath);
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var stem = Object.FindObjectsByType<SplineExtrude>(FindObjectsSortMode.None).Single();
        stem.name = "S23_VineStem";
        var container = stem.GetComponent<SplineContainer>();
        stem.transform.SetPositionAndRotation(new Vector3(-3.02f, 0, 1.88f), Quaternion.identity);
        stem.transform.localScale = Vector3.one;
        // 로컬 좌표로 제어점을 배치하고 AutoSmooth 접선으로 부드럽게 연결한다.
        container.Splines = new[] { new Spline(new float3[] {
            new float3(0, .015f, 0), new float3(-.10f, .65f, .05f),
            new float3(.12f, 1.30f, .02f), new float3(.08f, 1.95f, -.12f),
            new float3(-.10f, 2.60f, -.08f), new float3(.15f, 3.25f, .05f),
            new float3(.65f, 3.65f, .12f), new float3(1.05f, 3.55f, .18f)
        }, TangentMode.AutoSmooth, false) };
        stem.Container = container;
        stem.Radius = .045f;
        stem.Sides = 12;
        SetCircleSides(stem);
        stem.SegmentsPerUnit = 12;
        stem.Capped = true;
        stem.Range = new Vector2(0, 1);
        stem.RebuildOnSplineChange = true;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = "S23_VineStem_Green";
        mat.SetColor("_BaseColor", new Color(.20f, .38f, .12f, 1));
        mat.SetFloat("_Smoothness", .22f);
        AssetDatabase.CreateAsset(mat, Root + "/Materials/S23_VineStem_Green.mat");
        stem.GetComponent<MeshRenderer>().sharedMaterials = new[] { mat };
        var mesh = new Mesh { name = "S23_VineStem_Mesh" };
        AssetDatabase.CreateAsset(mesh, Root + "/Meshes/S23_VineStem_Mesh.asset");
        stem.targetMesh = null;
        stem.Rebuild();
        EditorUtility.CopySerialized(stem.GetComponent<MeshFilter>().sharedMesh, mesh);
        mesh.name = "S23_VineStem_Mesh";
        EditorUtility.SetDirty(mesh);
        PrefabUtility.SaveAsPrefabAssetAndConnect(stem.gameObject, Root + "/Prefabs/S23_VineStem.prefab", InteractionMode.AutomatedAction);
        var camera = GameObject.Find("ShotCamera").GetComponent<Camera>();
        camera.transform.position = new Vector3(-7.7f, 3.0f, 7.3f);
        camera.transform.LookAt(stem.transform.position + new Vector3(.25f, 1.85f, 0));
        camera.fieldOfView = 42;
        foreach(var other in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            other.enabled = other == camera;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.45f,.46f,.43f);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = stem.gameObject;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log($"S23 stem saved: {container.Spline.Count} knots, radius {stem.Radius}, sides {stem.Sides}, caps {stem.Capped}, {mesh.vertexCount} vertices / {mesh.triangles.Length/3} triangles.");
    }

    // 2.9.2에서는 Circle 단면의 값도 별도로 동기화한다.
    static void SetCircleSides(SplineExtrude stem)
    {
        var serialized = new SerializedObject(stem);
        serialized.FindProperty("m_Shape").FindPropertyRelative("m_Sides").intValue = 12;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("S23/Save Stem Geometry")]
    public static void SaveGeometry()
    {
        if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath) return;
        var stem = GameObject.Find("S23_VineStem").GetComponent<SplineExtrude>();
        SetCircleSides(stem);
        // 고정 에셋은 제출용 스냅샷으로 보관한다. 실시간 편집용 메시와 분리하여
        // Play 전환 시 Splines 패키지의 지연 SaveAssetIfDirty 오류를 피한다.
        stem.targetMesh = null;
        stem.Rebuild();
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/Meshes/S23_VineStem_Mesh.asset");
        EditorUtility.CopySerialized(stem.GetComponent<MeshFilter>().sharedMesh, mesh);
        mesh.name = "S23_VineStem_Mesh";
        EditorUtility.SetDirty(mesh);
        PrefabUtility.ApplyPrefabInstance(stem.gameObject, InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(stem.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"S23 geometry saved: {mesh.vertexCount} vertices, {mesh.triangles.Length/3} triangles; capped, 12 sides.");
    }
}
