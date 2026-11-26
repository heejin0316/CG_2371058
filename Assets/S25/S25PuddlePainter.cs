using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

public class S25PuddlePainter : MonoBehaviour
{
    public Camera viewCamera;
    public Collider ground;
    public DecalProjector puddlePrefab;
    public float spacing = .45f;
    Vector3 lastPoint;
    bool painting;
    int count;

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.isPressed) { painting = false; return; }
        if (!ground.Raycast(viewCamera.ScreenPointToRay(mouse.position.ReadValue()), out var hit, 100)) return;
        if (!painting || Vector3.Distance(lastPoint, hit.point) >= spacing)
        {
            SpawnAt(hit.point, hit.normal);
            lastPoint = hit.point;
            painting = true;
        }
    }

    public void SpawnAt(Vector3 point, Vector3 normal)
    {
        if (count >= 100) return;
        var decal = Instantiate(puddlePrefab, point + normal * .04f,
            Quaternion.LookRotation(-normal, Vector3.forward), transform);
        decal.name = "Puddle_Click_" + ++count;
    }
}
