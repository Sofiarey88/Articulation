using UnityEngine;

/// <summary>
/// Detector de la Zona de Entrega (Drop-off Zone) para el prototipo de simulador.
/// Detecta cuando el contenedor con Rigidbody ingresa a la zona y queda estabilizado.
/// </summary>
public class ZonaEntregaCarga : MonoBehaviour
{
    [Header("Configuración de Objetivo")]
    [Tooltip("Etiqueta o referencia del Rigidbody que debe ser entregado")]
    public Rigidbody cargaObjetivo;

    [Tooltip("Velocidad máxima permitida para considerar la carga depositada (m/s)")]
    public float velocidadMaximaEstable = 0.25f;

    [Tooltip("Tiempo que la carga debe permanecer estable dentro de la zona (segundos)")]
    public float tiempoRequerido = 1.5f;

    [Header("Estado")]
    public bool cargaDentro = false;
    public bool misionCompletada = false;
    public float tiempoEstableActual = 0f;

    [Header("Efectos Visuales")]
    public Light luzIndicadora;
    public Color colorEsperando = new Color(1f, 0.7f, 0.1f);
    public Color colorExito = new Color(0.1f, 1f, 0.3f);

    private Renderer _rendererPlataforma;

    void Start()
    {
        _rendererPlataforma = GetComponent<Renderer>();
        ActualizarVisual(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (EsCargaObjetivo(other))
        {
            cargaDentro = true;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!EsCargaObjetivo(other) || misionCompletada) return;

        cargaDentro = true;
        Rigidbody rb = cargaObjetivo != null ? cargaObjetivo : other.attachedRigidbody;

        if (rb != null && !rb.isKinematic && rb.linearVelocity.magnitude <= velocidadMaximaEstable)
        {
            tiempoEstableActual += Time.fixedDeltaTime;
            if (tiempoEstableActual >= tiempoRequerido)
            {
                misionCompletada = true;
                ActualizarVisual(true);
            }
        }
        else
        {
            tiempoEstableActual = Mathf.Max(0f, tiempoEstableActual - Time.fixedDeltaTime);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (EsCargaObjetivo(other))
        {
            cargaDentro = false;
            tiempoEstableActual = 0f;
            if (!misionCompletada)
            {
                ActualizarVisual(false);
            }
        }
    }

    private bool EsCargaObjetivo(Collider col)
    {
        if (cargaObjetivo != null)
        {
            return col.attachedRigidbody == cargaObjetivo;
        }
        return col.CompareTag("Respawn") || (col.attachedRigidbody != null && col.attachedRigidbody.name.Contains("Carga"));
    }

    public void ResetearZona()
    {
        misionCompletada = false;
        cargaDentro = false;
        tiempoEstableActual = 0f;
        ActualizarVisual(false);
    }

    private void ActualizarVisual(bool exito)
    {
        Color c = exito ? colorExito : colorEsperando;
        if (luzIndicadora != null)
        {
            luzIndicadora.color = c;
            luzIndicadora.intensity = exito ? 3f : 1.5f;
        }
        if (_rendererPlataforma != null && _rendererPlataforma.material != null)
        {
            _rendererPlataforma.material.color = c;
        }
    }
}
