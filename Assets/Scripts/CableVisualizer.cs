using UnityEngine;

/// <summary>
/// Dibuja cables dinámicos entre el carro distribuidor y el cabezal del gancho usando LineRenderer.
/// </summary>
[ExecuteAlways]
public class CableVisualizer : MonoBehaviour
{
    [Tooltip("Punto de anclaje superior en el carro de la pluma")]
    public Transform anclajeCarro;

    [Tooltip("Punto de anclaje inferior en el cabezal del gancho")]
    public Transform anclajeGancho;

    private LineRenderer _lineRenderer;

    void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null)
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = 0.035f;
        _lineRenderer.endWidth = 0.035f;
        Material cableMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse"));
        cableMat.color = new Color(0.15f, 0.15f, 0.18f);
        _lineRenderer.sharedMaterial = cableMat;
        _lineRenderer.useWorldSpace = true;
    }

    void LateUpdate()
    {
        if (anclajeCarro != null && anclajeGancho != null && _lineRenderer != null)
        {
            _lineRenderer.SetPosition(0, anclajeCarro.position);
            _lineRenderer.SetPosition(1, anclajeGancho.position);
        }
    }
}
