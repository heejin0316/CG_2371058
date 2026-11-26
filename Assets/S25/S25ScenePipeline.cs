using UnityEngine;
using UnityEngine.Rendering;

// S25 씬에만 데칼을 지원하는 파이프라인 복사본을 적용한다.
[ExecuteAlways]
public class S25ScenePipeline : MonoBehaviour
{
    public RenderPipelineAsset pipeline;
    public RenderPipelineAsset originalPipeline;
    void OnEnable() { if (pipeline) QualitySettings.renderPipeline = pipeline; }
    void OnDisable() { if (QualitySettings.renderPipeline == pipeline) QualitySettings.renderPipeline = originalPipeline; }
}
