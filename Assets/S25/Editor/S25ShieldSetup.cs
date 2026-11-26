using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace S25.Editor
{
    public static class S25ShieldSetup
    {
        const string Folder = "Assets/S25";
        const string GraphPath = Folder + "/S25_WaterShieldGraph.shadergraph";
        const string MaterialPath = Folder + "/S25_WaterShield.mat";
        const string ScenePath = Folder + "/S25_Shield.unity";

        [MenuItem("S25/Create Shield Demo")]
        public static void Create()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
            if (shader == null)
                throw new System.InvalidOperationException("S25 shield graph was not imported.");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
                material.SetFloat("_Opacity", 0.45f);
            }
            else if (material.shader != shader)
                material.shader = shader;

            if (material.GetTexture("_WaterTexture") == null)
                material.SetTexture("_WaterTexture", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/S25_Water.png"));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shield.name = "WaterShield_ClickToRipple";
            shield.transform.position = new Vector3(0f, 1.7f, 0f);
            shield.transform.localScale = Vector3.one * 3f;
            shield.GetComponent<Renderer>().sharedMaterial = material;
            shield.AddComponent<S25ShieldClick>();

            GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backdrop.name = "WhiteBackdrop_TransparencyCheck";
            backdrop.transform.position = new Vector3(0f, 1.7f, 2.5f);
            backdrop.transform.localScale = new Vector3(0.7f, 3.4f, 0.05f);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.12f, 0.2f);
            camera.fieldOfView = 45f;
            cameraObject.transform.position = new Vector3(0f, 1.7f, -6.5f);
            cameraObject.transform.LookAt(shield.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("S25 shield demo saved: " + ScenePath);
        }
    }
}
