using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class S25PuddleSetup
{
    const string Root = "Assets/S25/";
    [MenuItem("S25/Create Puddle Landscape")]
    public static void Create()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var path = Root + "S25_Landscape.unity";
        if (!File.Exists(path)) AssetDatabase.CopyAsset("Assets/Mid/Mid_Landscape.unity", path);
        var scene = EditorSceneManager.OpenScene(path);
        if (GameObject.Find("S25_ReflectionAndPuddles")) { Debug.Log("S25 puddle scene already exists."); return; }

        AssetDatabase.CopyAsset("Assets/Settings/PC_Renderer.asset", Root + "S25_Renderer.asset");
        AssetDatabase.CopyAsset("Assets/Settings/PC_RPAsset.asset", Root + "S25_Pipeline.asset");
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root + "S25_Renderer.asset");
        var decalFeature = ScriptableObject.CreateInstance<DecalRendererFeature>();
        decalFeature.name = "S25 Decals DBuffer";
        AssetDatabase.AddObjectToAsset(decalFeature, renderer);
        var settings = new SerializedObject(decalFeature);
        settings.FindProperty("m_Settings.technique").enumValueIndex = 1;
        settings.ApplyModifiedPropertiesWithoutUndo();
        renderer.rendererFeatures.Add(decalFeature);
        renderer.SetDirty(); EditorUtility.SetDirty(renderer);
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root + "S25_Pipeline.asset");
        var pipelineData = new SerializedObject(pipeline);
        pipelineData.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue = renderer;
        pipelineData.ApplyModifiedPropertiesWithoutUndo();

        var root = new GameObject("S25_ReflectionAndPuddles");
        root.SetActive(false);
        var scope = root.AddComponent<S25ScenePipeline>(); scope.pipeline = pipeline;
        scope.originalPipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        root.SetActive(true);
        var material = new Material(AssetDatabase.LoadAssetAtPath<Shader>(Root + "S25_Puddle.shadergraph"));
        AssetDatabase.CreateAsset(material, Root + "S25_Puddle.mat");
        var prefabObject = new GameObject("S25_Decal_Puddle");
        prefabObject.transform.rotation = Quaternion.Euler(90, 0, 0);
        var projector = prefabObject.AddComponent<DecalProjector>();
        projector.material = material; projector.size = new Vector3(2.8f, 2.2f, .35f);
        projector.pivot = Vector3.zero; projector.drawDistance = 80;
        var prefab = PrefabUtility.SaveAsPrefabAsset(prefabObject, Root + "S25_Decal_Puddle.prefab");
        Object.DestroyImmediate(prefabObject);

        var ground = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
            .First(f => f.sharedMesh && f.sharedMesh.name.Contains("ContinuousGrassland"));
        var collider = ground.GetComponent<MeshCollider>();
        if (!collider) collider = ground.gameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = ground.sharedMesh;
        // 장면 복사본만 Reflection Probe의 정적 캡처 대상으로 지정한다.
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if (!r.GetComponentsInParent<Transform>().Any(t => t.name == "Chiikawa" || t.name == "Hachiware" || t.name.Contains("Rock")))
                GameObjectUtility.SetStaticEditorFlags(r.gameObject,
                    GameObjectUtility.GetStaticEditorFlags(r.gameObject) | StaticEditorFlags.ReflectionProbeStatic);

        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        { c.enabled = false; c.tag = "Untagged"; var a=c.GetComponent<AudioListener>(); if(a)a.enabled=false; }
        var camera = new GameObject("S25_PuddleCamera").AddComponent<Camera>();
        camera.transform.SetParent(root.transform);
        camera.transform.position = new Vector3(3.8f, .65f, -5.6f);
        camera.transform.LookAt(new Vector3(5.2f, .85f, -.2f));
        camera.fieldOfView = 62; camera.nearClipPlane = .05f; camera.farClipPlane = 150;
        camera.tag = "MainCamera"; camera.gameObject.AddComponent<AudioListener>();
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;

        var painter = root.AddComponent<S25PuddlePainter>();
        painter.viewCamera=camera; painter.ground=collider; painter.puddlePrefab=prefab.GetComponent<DecalProjector>();
        painter.SpawnAt(new Vector3(4.8f,0,-2.7f),Vector3.up);
        root.transform.GetChild(root.transform.childCount-1).name="Puddle_SavedExample";
        var probe = new GameObject("S25_BakedReflectionProbe").AddComponent<ReflectionProbe>();
        probe.transform.SetParent(root.transform); probe.transform.position=new Vector3(4.8f,.5f,-2.2f);
        probe.mode=ReflectionProbeMode.Baked; probe.resolution=1024; probe.hdr=true;
        probe.boxProjection=true; probe.size=new Vector3(24,12,24); probe.center=new Vector3(0,4,2);
        probe.nearClipPlane=.05f; probe.farClipPlane=100; probe.intensity=1;
        probe.clearFlags=ReflectionProbeClearFlags.Skybox;
        AssetDatabase.SaveAssets();
        if(!Lightmapping.BakeReflectionProbe(probe,Root+"S25_PavilionReflection.exr")) throw new System.Exception("Probe bake failed");
        AssetDatabase.ImportAsset(Root+"S25_PavilionReflection.exr");
        probe.customBakedTexture=AssetDatabase.LoadAssetAtPath<Cubemap>(Root+"S25_PavilionReflection.exr");
        probe.mode=ReflectionProbeMode.Custom; // 저장한 Bake 결과를 재시작 후에도 사용한다.
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Selection.activeGameObject=root;
        Debug.Log("S25 puddle landscape saved; Reflection Probe baked.");
    }

    [MenuItem("S25/Bake Puddle Reflection")]
    public static void Bake()
    {
        var scope=Object.FindFirstObjectByType<S25ScenePipeline>();
        if(!scope || EditorApplication.isPlaying) return;
        scope.originalPipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        var camera=GameObject.Find("S25_PuddleCamera").GetComponent<Camera>();
        camera.transform.position=new Vector3(3.8f,.48f,-5.5f);
        camera.transform.LookAt(new Vector3(5.1f,.72f,-.1f));
        var puddle=GameObject.Find("Puddle_SavedExample").GetComponent<DecalProjector>();
        puddle.transform.position=new Vector3(4.8f,.04f,-3.05f);
        puddle.size=new Vector3(4.2f,3.1f,.22f);
        var projectors=Object.FindObjectsByType<DecalProjector>(FindObjectsSortMode.None);
        foreach(var p in projectors)p.enabled=false;
        var probe=Object.FindFirstObjectByType<ReflectionProbe>();
        Lightmapping.BakeReflectionProbe(probe,Root+"S25_PavilionReflection.exr");
        foreach(var p in projectors)p.enabled=true;
        AssetDatabase.ImportAsset(Root+"S25_PavilionReflection.exr");
        probe.customBakedTexture=AssetDatabase.LoadAssetAtPath<Cubemap>(Root+"S25_PavilionReflection.exr");
        probe.mode=ReflectionProbeMode.Custom;
        EditorSceneManager.SaveScene(scope.gameObject.scene);AssetDatabase.SaveAssets();
        Debug.Log("S25 reflection rebaked and low camera saved.");
    }
}
