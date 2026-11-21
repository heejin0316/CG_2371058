using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

// S23 3번: 네 모서리를 쌍선형 보간하여 나뭇잎의 격자 정점과 UV를 만든다.
public static class S23LeafBuilder
{
    const string Root = "Assets/S23";
    const string ScenePath = Root + "/S23_Landscape.unity";

    [MenuItem("S23/3. Add Bilinear Leaves")]
    public static void AddLeaves()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath);

        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/S23_BilinearPatch/Mat_S23_Leaf.mat");
        string materialPath = Root + "/Materials/S23_Leaf.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) == null)
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), materialPath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        var group = GameObject.Find("S23_BilinearLeaves") ?? new GameObject("S23_BilinearLeaves");
        for (int i = group.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(group.transform.GetChild(i).gameObject);
        var stem = GameObject.Find("S23_VineStem");
        Vector3 basePos = stem.transform.position;

        CreateLeaf(group.transform, material, "S23_Leaf_Lower", basePos + new Vector3(.04f, 2.35f, .20f), new Vector3(-1.20f, .38f, .75f), "S23_Leaf_Lower_Mesh");
        CreateLeaf(group.transform, material, "S23_Leaf_Upper", basePos + new Vector3(.03f, 3.08f, .20f), new Vector3(-.90f, .50f, 1.00f), "S23_Leaf_Upper_Mesh");

        var camera = GameObject.Find("ShotCamera").GetComponent<Camera>();
        camera.transform.position = new Vector3(-6.0f, 3.0f, 5.0f);
        camera.transform.LookAt(basePos + new Vector3(-.45f, 2.75f, .45f));
        camera.fieldOfView = 34f;
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = group;
        Debug.Log("S23 bilinear leaves saved: 2 textured leaves, 12x16 grid per leaf.");
    }

    static void CreateLeaf(Transform parent, Material material, string name, Vector3 origin, Vector3 direction, string meshName)
    {
        var leaf = new GameObject(name);
        leaf.transform.SetParent(parent, false);
        leaf.transform.position = origin;
        // 길이 축은 덩굴에서 바깥으로, 폭 축은 잎의 좌우로 향한다.
        var length = direction.normalized * .95f;
        var width = Vector3.Cross(length, new Vector3(-.55f, 0, .83f)).normalized * .40f;
        var corners = new[] {
            new Vector3(-.07f, 0, 0), new Vector3(.07f, 0, 0),
            length - width + Vector3.up * .10f,
            length + width + Vector3.up * .10f
        };
        string[] labels = { "Corner00", "Corner10", "Corner01", "Corner11" };
        for (int i = 0; i < 4; i++)
        {
            var marker = new GameObject(labels[i]);
            marker.transform.SetParent(leaf.transform, false);
            marker.transform.localPosition = corners[i];
        }

        const int uSegments = 12, vSegments = 16;
        int row = uSegments + 1;
        var vertices = new Vector3[row * (vSegments + 1)];
        var uv = new Vector2[vertices.Length];
        var indices = new int[uSegments * vSegments * 12];
        for (int vIndex = 0; vIndex <= vSegments; vIndex++)
        for (int uIndex = 0; uIndex <= uSegments; uIndex++)
        {
            float u = (float)uIndex / uSegments, v = (float)vIndex / vSegments;
            int index = vIndex * row + uIndex;
            vertices[index] = (1-u)*(1-v)*corners[0] + u*(1-v)*corners[1]
                            + (1-u)*v*corners[2] + u*v*corners[3];
            uv[index] = new Vector2(u, v);
        }
        int t = 0;
        for (int j = 0; j < vSegments; j++)
        for (int i = 0; i < uSegments; i++)
        {
            int a = j * row + i, b = a + 1, c = a + row, d = c + 1;
            // 양쪽 면 모두 생성하여 측면과 아래에서도 보인다.
            int[] pair = { a,c,b,b,c,d, b,c,a,d,c,b };
            foreach (int k in pair) indices[t++] = k;
        }
        var mesh = new Mesh { name = meshName, vertices = vertices, uv = uv, triangles = indices };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        string path = Root + "/Meshes/" + meshName + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) AssetDatabase.CreateAsset(mesh, path);
        else { EditorUtility.CopySerialized(mesh, existing); EditorUtility.SetDirty(existing); mesh = existing; }
        leaf.AddComponent<MeshFilter>().sharedMesh = mesh;
        leaf.AddComponent<MeshRenderer>().sharedMaterial = material;
        PrefabUtility.SaveAsPrefabAssetAndConnect(leaf, Root + "/Prefabs/" + name + ".prefab", InteractionMode.AutomatedAction);
    }
}
