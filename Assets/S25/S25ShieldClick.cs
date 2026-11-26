using UnityEngine;
using UnityEngine.InputSystem;

namespace S25
{
    [RequireComponent(typeof(Collider), typeof(Renderer))]
    public sealed class S25ShieldClick : MonoBehaviour
    {
        static readonly int ImpactPosition = Shader.PropertyToID("_ImpactPosition");
        static readonly int ImpactTime = Shader.PropertyToID("_ImpactTime");

        Renderer shieldRenderer;
        Collider shieldCollider;
        MaterialPropertyBlock properties;

        void Awake()
        {
            shieldRenderer = GetComponent<Renderer>();
            shieldCollider = GetComponent<Collider>();
            properties = new MaterialPropertyBlock();
            properties.SetFloat(ImpactTime, -1000f);
            shieldRenderer.SetPropertyBlock(properties);
        }

        void Update()
        {
            Pointer pointer = Pointer.current;
            Camera camera = Camera.main;
            if (pointer == null || camera == null || !pointer.press.wasPressedThisFrame)
                return;

            Ray ray = camera.ScreenPointToRay(pointer.position.ReadValue());
            if (!shieldCollider.Raycast(ray, out RaycastHit hit, 1000f))
                return;

            properties.SetVector(ImpactPosition, hit.point);
            properties.SetFloat(ImpactTime, Time.timeSinceLevelLoad);
            shieldRenderer.SetPropertyBlock(properties);
        }
    }
}
