using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// S23 4번: 정자 지붕을 두 개의 bilinear patch가 만나는 캐노피로 교체한다.
public static class S23CanopyBuilder
{
    const string Root = "Assets/S23";
    const string ScenePath = Root + "/S23_Landscape.unity";
    const string MeshPath = Root + "/Meshes/S23_PavilionCanopy_Mesh.asset";
    const string MaterialPath = Root + "/Materials/S23_Canopy_Mat.mat";

    [MenuItem("S23/4. Replace Pavilion Roof With Canopy")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath) == null)
        {
            AssetDatabase.CreateAsset(new Mesh { name = "S23_PavilionCanopy_Mesh" }, MeshPath);
            AssetDatabase.SaveAssets();
        }
        var canopyMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (canopyMaterial == null)
        {
            canopyMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "S23_Canopy_Mat" };
            canopyMaterial.SetColor("_BaseColor", new Color(.83f, .68f, .47f));
            canopyMaterial.SetFloat("_Smoothness", .12f);
            canopyMaterial.SetFloat("_Cull", 2f);
            AssetDatabase.CreateAsset(canopyMaterial, MaterialPath);
            AssetDatabase.SaveAssets();
        }
        else
        {
            canopyMaterial.SetFloat("_Cull", 2f);
            EditorUtility.SetDirty(canopyMaterial);
        }
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var roof = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .FirstOrDefault(r => r.name == "Pavilion_Roof");
        if (roof == null) { Debug.LogError("Pavilion_Roof was not found."); return; }
        var pavilion = roof.transform.parent;
        var bounds = roof.bounds;

        var canopy = pavilion.Find("S23_PavilionCanopy");
        if (canopy == null)
        {
            canopy = new GameObject("S23_PavilionCanopy").transform;
            canopy.SetParent(pavilion, false);
        }
        canopy.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        canopy.localScale = Vector3.one;
        // 월드 좌표의 지붕 영역을 이용해 캐노피 네 모서리와 중앙 능선을 정한다.
        float left = bounds.min.x - .08f, right = bounds.max.x + .08f;
        float front = bounds.min.z - .08f, back = bounds.max.z + .08f;
        float middle = (left + right) * .5f;
        float eave = bounds.min.y + .18f;
        float ridge = bounds.max.y + .24f;
        Vector3[] corners = {
            new(left,eave,front), new(middle,ridge,front),
            new(left,eave+.10f,back), new(middle,ridge-.12f,back),
            new(middle,ridge,front), new(right,eave,front),
            new(middle,ridge-.12f,back), new(right,eave+.10f,back)
        };
        for (int i = canopy.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(canopy.GetChild(i).gameObject);
        var mesh = MakeMesh(canopy, corners);
        var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        savedMesh.Clear();
        savedMesh.vertices = mesh.vertices;
        savedMesh.uv = mesh.uv;
        savedMesh.triangles = mesh.triangles;
        savedMesh.RecalculateNormals();
        savedMesh.RecalculateBounds();
        EditorUtility.SetDirty(savedMesh);
        Object.DestroyImmediate(mesh);
        mesh = savedMesh;
        var filter = canopy.GetComponent<MeshFilter>();
        if (filter == null) filter = canopy.gameObject.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        var renderer = canopy.GetComponent<MeshRenderer>();
        if (renderer == null) renderer = canopy.gameObject.AddComponent<MeshRenderer>();
        renderer.enabled = true;
        renderer.sharedMaterial = canopyMaterial;
        var collider = canopy.GetComponent<MeshCollider>();
        if (collider != null) Object.DestroyImmediate(collider);
        roof.enabled = false;
        var oldCollider = roof.GetComponent<Collider>();
        if (oldCollider != null) oldCollider.enabled = true;
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"S23 canopy saved: {mesh.vertexCount} vertices, {mesh.triangles.Length/3} triangles.");
    }

    static Mesh MakeMesh(Transform canopy, Vector3[] c)
    {
        const int uCount = 21, vCount = 17;
        int patchVertices = uCount*vCount;
        // 앞뒷면 정점을 분리해야 RecalculateNormals가 반대 방향 법선을 서로 지우지 않는다.
        var vertices = new Vector3[patchVertices*4];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[(uCount-1)*(vCount-1)*2*12];
        int t = 0;
        for (int patch=0; patch<2; patch++)
        {
            int offset = patch*patchVertices*2, backOffset = offset+patchVertices, co = patch*4;
            for (int vIndex=0; vIndex<vCount; vIndex++)
            for (int uIndex=0; uIndex<uCount; uIndex++)
            {
                float u=(float)uIndex/(uCount-1), v=(float)vIndex/(vCount-1);
                var world=(1-u)*(1-v)*c[co]+u*(1-v)*c[co+1]
                         +(1-u)*v*c[co+2]+u*v*c[co+3];
                int index=offset+vIndex*uCount+uIndex;
                vertices[index]=canopy.InverseTransformPoint(world);
                vertices[backOffset+vIndex*uCount+uIndex]=vertices[index];
                uv[index]=new Vector2((patch+u)*.5f,v);
                uv[backOffset+vIndex*uCount+uIndex]=uv[index];
            }
            for (int j=0;j<vCount-1;j++)
            for (int i=0;i<uCount-1;i++)
            {
                int a=offset+j*uCount+i,b=a+1,d=a+uCount+1,cc=a+uCount;
                int ba=backOffset+j*uCount+i, bb=ba+1, bd=ba+uCount+1, bc=ba+uCount;
                int[] both={a,cc,b,b,cc,d,bb,bc,ba,bd,bc,bb};
                foreach(int index in both) triangles[t++]=index;
            }
        }
        var mesh=new Mesh {name="S23_PavilionCanopy_Mesh",vertices=vertices,uv=uv,triangles=triangles};
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }
}
