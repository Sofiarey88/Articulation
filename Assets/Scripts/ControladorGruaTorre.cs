using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador de la Grúa Torre con ArticulationBody.
/// - Detecta automáticamente el eje libre de las juntas prismáticas (carro, malacate, mordazas),
///   así funciona sin importar si en el Inspector el eje es X, Y o Z.
/// - Slider para Giro de la Pluma, Carro en Brazo y Largo Vertical (Target vs Real).
/// - Botones "Bajar y Agarrar" y "Soltar".
/// - Agarre cinemático estable + mordazas físicas.
/// </summary>
public class ControladorGruaTorre : MonoBehaviour
{
    [Header("Articulaciones Controladas")]
    public ArticulationBody plumaGiratoria;
    public ArticulationBody carroDistribuidor;
    public ArticulationBody carroMalacate;
    public ArticulationBody mordazaIzquierda;
    public ArticulationBody mordazaDerecha;

    [Header("Articulación Pasiva (Sin Drive)")]
    public ArticulationBody eslabonSuspensionPasivo;

    [Header("Rangos y Parámetros")]
    // Horizontal (carro sobre el brazo)
    public float carroMinDist = 0f;
    public float carroMaxDist = 14f;
    public float carroVelocidad = 4f;

    // Vertical (largo del cable)
    public float malacateMinY = 0f;
    public float malacateMaxY = 12f;
    public float malacateVelocidad = 3f;

    // Giro (vuelta completa)
    public float plumaMinAngulo = -360f;
    public float plumaMaxAngulo = 360f;
    public float plumaVelocidad = 45f;

    public float pinzaAperturaMin = 0.12f; // Cerrada (pinzando)
    public float pinzaAperturaMax = 0.55f; // Abierta

    [Header("Targets Actuales")]
    public float targetGiroPluma = 0f;
    public float targetCarroDist = 3.5f;
    public float targetMalacateDescenso = 1.0f;
    public float targetPinzaApertura = 0.55f;

    [Header("Carga y Entorno")]
    public Rigidbody cargaRigidbody;
    public Transform zonaRecogidaInicial;
    public ZonaEntregaCarga zonaEntrega;
    public Transform puntoAgarreGancho;

    [Header("Depuración")]
    public bool logDiagnostico = true;

    // Estado interno
    private bool _cargaAcoplada = false;
    private bool _bajandoParaAgarrar = false;

    // Velocidad del gancho para transferir inercia al soltar
    private Vector3 _posicionPreviaGancho;
    private Vector3 _velocidadGancho;

    // Valores reales leídos de las juntas
    private float _realGiroPluma = 0f;
    private float _realCarroDist = 0f;
    private float _realMalacateDescenso = 0f;
    private Vector3 _oscilacionPasiva = Vector3.zero;

    // =========================================================================
    // HELPERS: detectan el eje lineal libre de una junta prismática
    // =========================================================================
    private static ArticulationDrive GetDriveLibre(ArticulationBody ab)
    {
        if (ab.linearLockX != ArticulationDofLock.LockedMotion) return ab.xDrive;
        if (ab.linearLockY != ArticulationDofLock.LockedMotion) return ab.yDrive;
        return ab.zDrive;
    }

    private static void SetDriveLibre(ArticulationBody ab, ArticulationDrive d)
    {
        if (ab.linearLockX != ArticulationDofLock.LockedMotion) ab.xDrive = d;
        else if (ab.linearLockY != ArticulationDofLock.LockedMotion) ab.yDrive = d;
        else ab.zDrive = d;
    }

    void Start()
    {
        InicializarDrives();
        if (puntoAgarreGancho != null)
            _posicionPreviaGancho = puntoAgarreGancho.position;

        if (logDiagnostico) DiagnosticarJuntas();
    }

    private void DiagnosticarJuntas()
    {
        DiagnosticarJunta("Pluma", plumaGiratoria);
        DiagnosticarJunta("Carro", carroDistribuidor);
        DiagnosticarJunta("Malacate", carroMalacate);
        DiagnosticarJunta("Mordaza Izq", mordazaIzquierda);
        DiagnosticarJunta("Mordaza Der", mordazaDerecha);
    }

    private void DiagnosticarJunta(string nombre, ArticulationBody ab)
    {
        if (ab == null)
        {
            Debug.LogError($"[Grúa] {nombre}: NO asignado en el Inspector");
            return;
        }
        Debug.Log($"[Grúa] {nombre}: tipo={ab.jointType} lockX={ab.linearLockX} lockY={ab.linearLockY} lockZ={ab.linearLockZ} dof={ab.dofCount} immovable={ab.immovable}");
        if (ab.dofCount == 0)
            Debug.LogWarning($"[Grúa] {nombre}: dofCount = 0, la junta no se puede mover (revisar Locks / Immovable).");
    }

    public void InicializarDrives()
    {
        // 1. Pluma (Revolute: gira sobre el eje Twist, usa xDrive)
        if (plumaGiratoria != null)
        {
            var drive = plumaGiratoria.xDrive;
            drive.stiffness = 60000f;
            drive.damping = 2500f;
            drive.forceLimit = 80000f;
            drive.lowerLimit = plumaMinAngulo;
            drive.upperLimit = plumaMaxAngulo;
            drive.target = targetGiroPluma;
            plumaGiratoria.xDrive = drive;
        }

        // 2. Carro (Prismatic) - eje libre detectado automáticamente
        if (carroDistribuidor != null)
        {
            var drive = GetDriveLibre(carroDistribuidor);
            drive.stiffness = 30000f;
            drive.damping = 1200f;
            drive.forceLimit = 35000f;
            drive.lowerLimit = carroMinDist;
            drive.upperLimit = carroMaxDist;
            drive.target = targetCarroDist;
            SetDriveLibre(carroDistribuidor, drive);
        }

        // 3. Malacate Vertical (Prismatic) - eje libre detectado automáticamente
        if (carroMalacate != null)
        {
            var drive = GetDriveLibre(carroMalacate);
            drive.stiffness = 35000f;
            drive.damping = 1500f;
            drive.forceLimit = 45000f;
            drive.lowerLimit = malacateMinY;
            drive.upperLimit = malacateMaxY;
            drive.target = targetMalacateDescenso;
            SetDriveLibre(carroMalacate, drive);
        }

        // 4. Mordazas de la Pinza (Prismatic)
        ConfigurarDriveMordaza(mordazaIzquierda, -pinzaAperturaMax * 0.5f, -pinzaAperturaMin * 0.5f, -targetPinzaApertura * 0.5f);
        ConfigurarDriveMordaza(mordazaDerecha, pinzaAperturaMin * 0.5f, pinzaAperturaMax * 0.5f, targetPinzaApertura * 0.5f);

        // 5. Joint Pasivo: Spherical con oscilación pendular libre (X y Z pasivos) y Yaw anti-torsión
        if (eslabonSuspensionPasivo != null)
        {
            var px = eslabonSuspensionPasivo.xDrive;
            px.stiffness = 0f;
            px.damping = 0.5f;
            px.forceLimit = 0f;
            eslabonSuspensionPasivo.xDrive = px;

            var py = eslabonSuspensionPasivo.yDrive;
            py.stiffness = 40000f;
            py.damping = 1500f;
            py.forceLimit = 40000f;
            py.target = 0f;
            eslabonSuspensionPasivo.yDrive = py;

            var pz = eslabonSuspensionPasivo.zDrive;
            pz.stiffness = 0f;
            pz.damping = 0.5f;
            pz.forceLimit = 0f;
            eslabonSuspensionPasivo.zDrive = pz;
        }
    }

    private void ConfigurarDriveMordaza(ArticulationBody ab, float min, float max, float target)
    {
        if (ab == null) return;
        var drive = GetDriveLibre(ab);
        drive.stiffness = 25000f;
        drive.damping = 800f;
        drive.forceLimit = 30000f;
        drive.lowerLimit = min;
        drive.upperLimit = max;
        drive.target = target;
        SetDriveLibre(ab, drive);
    }

    void Update()
    {
        ProcesarEntradaTeclado();
        ActualizarManiobraAutomatica();
    }

    void FixedUpdate()
    {
        AplicarDrives();
        LeerTelemetriaReal();

        // Mantener la carga acoplada en posición exacta bajo el gancho
        if (_cargaAcoplada && cargaRigidbody != null && puntoAgarreGancho != null)
        {
            cargaRigidbody.transform.position = puntoAgarreGancho.position + new Vector3(0, -0.28f, 0);
            cargaRigidbody.transform.rotation = puntoAgarreGancho.rotation;
        }

        // Velocidad real del gancho para transferir inercia al soltar
        if (puntoAgarreGancho != null)
        {
            _velocidadGancho = (puntoAgarreGancho.position - _posicionPreviaGancho) / Time.fixedDeltaTime;
            _posicionPreviaGancho = puntoAgarreGancho.position;
        }
    }

    private void ProcesarEntradaTeclado()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float dt = Time.deltaTime;

        // Giro Pluma: A / D o Flechas Izq/Der
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
            targetGiroPluma += plumaVelocidad * dt;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
            targetGiroPluma -= plumaVelocidad * dt;
        targetGiroPluma = Mathf.Clamp(targetGiroPluma, plumaMinAngulo, plumaMaxAngulo);

        // Carro Horizontal en el Brazo: W (avanzar) / S (retroceder) o Flechas Arriba/Abajo
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
            targetCarroDist += carroVelocidad * dt;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
            targetCarroDist -= carroVelocidad * dt;
        targetCarroDist = Mathf.Clamp(targetCarroDist, carroMinDist, carroMaxDist);

        // Largo Vertical: E / F (baja) - Q / R (sube)
        if (kb.eKey.isPressed || kb.fKey.isPressed)
            targetMalacateDescenso += malacateVelocidad * dt;
        if (kb.qKey.isPressed || kb.rKey.isPressed)
            targetMalacateDescenso -= malacateVelocidad * dt;
        targetMalacateDescenso = Mathf.Clamp(targetMalacateDescenso, malacateMinY, malacateMaxY);

        // Barra espaciadora: si tiene carga la suelta, si no inicia agarre
        if (kb.spaceKey.wasPressedThisFrame)
        {
            if (_cargaAcoplada) SoltarCarga();
            else IniciarBajarYAgarrar();
        }

        // Tecla X: Reiniciar
        if (kb.xKey.wasPressedThisFrame)
            ReiniciarEscenario();
    }

    /// <summary>
    /// Desciende la pinza, agarra la carga y la eleva automáticamente.
    /// </summary>
    public void IniciarBajarYAgarrar()
    {
        if (_cargaAcoplada) return;

        targetPinzaApertura = pinzaAperturaMax;
        _bajandoParaAgarrar = true;

        // Si ya está muy cerca de la carga, agarrar inmediatamente
        if (puntoAgarreGancho != null && cargaRigidbody != null)
        {
            float dist = Vector3.Distance(puntoAgarreGancho.position, cargaRigidbody.position);
            if (dist <= 1.2f)
            {
                AgarrarCarga();
                _bajandoParaAgarrar = false;
                targetMalacateDescenso = 3.0f; // Elevar
                return;
            }
        }

        // Descenso hacia la altura de la carga
        targetMalacateDescenso = 7.35f;
    }

    private void ActualizarManiobraAutomatica()
    {
        if (!_bajandoParaAgarrar || cargaRigidbody == null || puntoAgarreGancho == null) return;

        float dist = Vector3.Distance(puntoAgarreGancho.position, cargaRigidbody.position);

        if (dist <= 1.1f || targetMalacateDescenso >= 7.3f)
        {
            AgarrarCarga();
            _bajandoParaAgarrar = false;
            targetMalacateDescenso = 3.0f; // Elevar la carga automáticamente
        }
    }

    /// <summary>
    /// Agarra la carga de manera firme y cierra las mordazas.
    /// </summary>
    public void AgarrarCarga()
    {
        if (cargaRigidbody == null || puntoAgarreGancho == null) return;

        targetPinzaApertura = pinzaAperturaMin;
        _cargaAcoplada = true;

        cargaRigidbody.isKinematic = true;
        var colCarga = cargaRigidbody.GetComponent<Collider>();
        if (colCarga != null) colCarga.isTrigger = true;

        cargaRigidbody.transform.position = puntoAgarreGancho.position + new Vector3(0, -0.28f, 0);
        cargaRigidbody.transform.rotation = puntoAgarreGancho.rotation;
    }

    /// <summary>
    /// Suelta la carga, abre las pinzas y restaura la física libre.
    /// </summary>
    public void SoltarCarga()
    {
        _bajandoParaAgarrar = false;
        targetPinzaApertura = pinzaAperturaMax;

        if (_cargaAcoplada && cargaRigidbody != null)
        {
            _cargaAcoplada = false;
            var colCarga = cargaRigidbody.GetComponent<Collider>();
            if (colCarga != null) colCarga.isTrigger = false;
            cargaRigidbody.isKinematic = false;
            cargaRigidbody.linearVelocity = _velocidadGancho;
        }
    }

    private void AplicarDrives()
    {
        if (plumaGiratoria != null)
        {
            var drive = plumaGiratoria.xDrive;
            drive.target = targetGiroPluma;
            plumaGiratoria.xDrive = drive;
        }

        if (carroDistribuidor != null)
        {
            var drive = GetDriveLibre(carroDistribuidor);
            drive.target = targetCarroDist;
            SetDriveLibre(carroDistribuidor, drive);
        }

        if (carroMalacate != null)
        {
            var drive = GetDriveLibre(carroMalacate);
            drive.target = targetMalacateDescenso;
            SetDriveLibre(carroMalacate, drive);
        }

        if (mordazaIzquierda != null)
        {
            var drive = GetDriveLibre(mordazaIzquierda);
            drive.target = -targetPinzaApertura * 0.5f;
            SetDriveLibre(mordazaIzquierda, drive);
        }

        if (mordazaDerecha != null)
        {
            var drive = GetDriveLibre(mordazaDerecha);
            drive.target = targetPinzaApertura * 0.5f;
            SetDriveLibre(mordazaDerecha, drive);
        }
    }

    private void LeerTelemetriaReal()
    {
        if (plumaGiratoria != null && plumaGiratoria.dofCount > 0)
            _realGiroPluma = plumaGiratoria.jointPosition[0] * Mathf.Rad2Deg;

        if (carroDistribuidor != null && carroDistribuidor.dofCount > 0)
            _realCarroDist = carroDistribuidor.jointPosition[0];

        if (carroMalacate != null && carroMalacate.dofCount > 0)
            _realMalacateDescenso = carroMalacate.jointPosition[0];

        if (eslabonSuspensionPasivo != null)
        {
            _oscilacionPasiva = eslabonSuspensionPasivo.transform.localEulerAngles;
            if (_oscilacionPasiva.x > 180f) _oscilacionPasiva.x -= 360f;
            if (_oscilacionPasiva.z > 180f) _oscilacionPasiva.z -= 360f;
        }
    }

    public void ReiniciarEscenario()
    {
        SoltarCarga();

        if (cargaRigidbody != null)
        {
            var colCarga = cargaRigidbody.GetComponent<Collider>();
            if (colCarga != null) colCarga.isTrigger = false;
            cargaRigidbody.isKinematic = false;
            cargaRigidbody.linearVelocity = Vector3.zero;
            cargaRigidbody.angularVelocity = Vector3.zero;
            if (zonaRecogidaInicial != null)
            {
                cargaRigidbody.transform.position = zonaRecogidaInicial.position + Vector3.up * 0.45f;
                cargaRigidbody.transform.rotation = Quaternion.identity;
            }
        }

        if (zonaEntrega != null)
            zonaEntrega.ResetearZona();

        targetGiroPluma = 0f;
        targetCarroDist = 3.5f;
        targetMalacateDescenso = 1.0f;
    }

    // =========================================================================
    // INTERFAZ DE USUARIO COMPACTA Y MINIMALISTA
    // =========================================================================
    void OnGUI()
    {
        GUI.skin.box.fontSize = 11;
        GUI.skin.label.fontSize = 11;
        GUI.skin.button.fontSize = 11;
        GUI.skin.box.richText = true;
        GUI.skin.label.richText = true;

        int w = 260;
        int h = 232;
        Rect panelRect = new Rect(12, 12, w, h);

        GUI.Box(panelRect, "<b>GRÚA TORRE</b>");

        int y = 34;
        int x = 20;
        int cw = w - 16;

        // 1. Estado de la carga / misión
        string estado;
        Color colorEstado;
        if (zonaEntrega != null && zonaEntrega.misionCompletada)
        {
            estado = "● ¡Misión Completada!";
            colorEstado = Color.green;
        }
        else if (_cargaAcoplada)
        {
            estado = "● Carga Sujeta (En transporte)";
            colorEstado = new Color(0.3f, 0.9f, 1f);
        }
        else if (_bajandoParaAgarrar)
        {
            estado = "● Bajando gancho...";
            colorEstado = Color.yellow;
        }
        else
        {
            estado = "● Carga en el suelo";
            colorEstado = Color.white;
        }

        GUI.color = colorEstado;
        GUI.Label(new Rect(x, y, cw, 18), estado);
        GUI.color = Color.white;
        y += 20;

        // 2. Giro de la Pluma [A / D]
        GUI.Label(new Rect(x, y, cw, 18), $"Giro: <b>{targetGiroPluma:F0}°</b> (Real: {_realGiroPluma:F0}°)");
        y += 17;
        targetGiroPluma = GUI.HorizontalSlider(new Rect(x, y, cw, 16), targetGiroPluma, plumaMinAngulo, plumaMaxAngulo);
        y += 20;

        // 3. Carro Horizontal en el Brazo [W / S]
        GUI.Label(new Rect(x, y, cw, 18), $"Carro en Brazo: <b>{targetCarroDist:F1}m</b> (Real: {_realCarroDist:F1}m)");
        y += 17;
        targetCarroDist = GUI.HorizontalSlider(new Rect(x, y, cw, 16), targetCarroDist, carroMinDist, carroMaxDist);
        y += 20;

        // 4. Largo Vertical [Q / E]
        GUI.Label(new Rect(x, y, cw, 18), $"Largo Vertical: <b>{targetMalacateDescenso:F1}m</b> (Real: {_realMalacateDescenso:F1}m)");
        y += 17;
        targetMalacateDescenso = GUI.HorizontalSlider(new Rect(x, y, cw, 16), targetMalacateDescenso, malacateMinY, malacateMaxY);
        y += 22;

        // 5. Botones principales
        int btnW = (cw - 6) / 2;
        string textoAgarrar = _cargaAcoplada ? "✓ Agarrada" : (_bajandoParaAgarrar ? "Bajando..." : "⬇ Bajar y Agarrar");

        GUI.backgroundColor = _cargaAcoplada ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.2f, 0.5f, 0.9f);
        if (GUI.Button(new Rect(x, y, btnW, 30), textoAgarrar))
        {
            if (!_cargaAcoplada) IniciarBajarYAgarrar();
        }

        GUI.backgroundColor = new Color(0.85f, 0.3f, 0.2f);
        if (GUI.Button(new Rect(x + btnW + 6, y, btnW, 30), "⬆ Soltar"))
        {
            SoltarCarga();
        }
        GUI.backgroundColor = Color.white;
        y += 34;

        // 6. Reiniciar
        if (GUI.Button(new Rect(x, y, cw, 22), "Reiniciar Carga [X]"))
        {
            ReiniciarEscenario();
        }
        y += 24;

        // 7. Pie de telemetría mínima
        GUI.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        GUI.Label(new Rect(x, y, cw, 16), $"9 Bodies · Spherical Pasivo: {_oscilacionPasiva.x:F0}°");
        GUI.color = Color.white;
    }
}