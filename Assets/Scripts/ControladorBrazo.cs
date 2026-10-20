using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador para un brazo robótico o grúa utilizando ArticulationBody en Unity C#.
/// Permite mover cada junta mediante teclas o mediante una interfaz en pantalla (GUI).
/// </summary>
public class ControladorBrazo : MonoBehaviour
{
    [System.Serializable]
    public class JuntaRobot
    {
        [Tooltip("Nombre descriptivo del eslabón")]
        public string nombre = "Nueva Junta";

        [Tooltip("Componente ArticulationBody correspondiente a este eslabón")]
        public ArticulationBody articulacion;

        [Header("Controles por Teclado")]
        public Key teclaPositiva = Key.E;
        public Key teclaNegativa = Key.Q;

        [Header("Límites y Velocidad")]
        public float anguloMinimo = -90f;
        public float anguloMaximo = 90f;
        public float velocidad = 50f; // Grados por segundo

        [Header("Configuración del Motor")]
        public float rigidez = 20000f;  // Stiffness
        public float amortiguacion = 500f; // Damping
        public float limiteFuerza = 20000f; // Force limit

        [Header("Estado Actual")]
        [Range(-180f, 180f)]
        public float anguloObjetivo = 0f;
    }

    [Header("Articulaciones")]
    [Tooltip("Lista de articulaciones que componen el brazo")]
    public JuntaRobot[] juntas;

    [Header("Interfaz en Pantalla (UI)")]
    [Tooltip("Muestra sliders y guía de controles durante el juego")]
    public bool mostrarGUI = true;

    void Start()
    {
        InicializarArticulaciones();
    }

    /// <summary>
    /// Configura los parámetros físicos del motor para cada articulación.
    /// </summary>
    public void InicializarArticulaciones()
    {
        if (juntas == null) return;

        foreach (var junta in juntas)
        {
            if (junta.articulacion == null) continue;

            ArticulationDrive drive = junta.articulacion.xDrive;
            drive.stiffness = junta.rigidez;
            drive.damping = junta.amortiguacion;
            drive.forceLimit = junta.limiteFuerza;
            drive.lowerLimit = junta.anguloMinimo;
            drive.upperLimit = junta.anguloMaximo;
            drive.target = junta.anguloObjetivo;

            junta.articulacion.xDrive = drive;
        }
    }

    void Update()
    {
        ProcesarEntradaTeclado();
    }

    /// <summary>
    /// Lee el teclado con el nuevo Input System y actualiza el ángulo objetivo de cada articulación.
    /// </summary>
    private void ProcesarEntradaTeclado()
    {
        if (juntas == null) return;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        foreach (var junta in juntas)
        {
            if (junta.articulacion == null) continue;

            float direccion = 0f;

            if (junta.teclaPositiva != Key.None && keyboard[junta.teclaPositiva].isPressed)
                direccion += 1f;

            if (junta.teclaNegativa != Key.None && keyboard[junta.teclaNegativa].isPressed)
                direccion -= 1f;

            if (direccion != 0f)
            {
                junta.anguloObjetivo += direccion * junta.velocidad * Time.deltaTime;
                junta.anguloObjetivo = Mathf.Clamp(junta.anguloObjetivo, junta.anguloMinimo, junta.anguloMaximo);
                ActualizarMotor(junta);
            }
        }
    }

    /// <summary>
    /// Aplica el ángulo objetivo al componente ArticulationBody.
    /// </summary>
    public void ActualizarMotor(JuntaRobot junta)
    {
        if (junta.articulacion == null) return;

        ArticulationDrive drive = junta.articulacion.xDrive;
        drive.target = junta.anguloObjetivo;
        junta.articulacion.xDrive = drive;
    }

    /// <summary>
    /// Método público para mover una junta desde otro script, botón o slider UI.
    /// </summary>
    public void EstablecerAngulo(int indiceJunta, float nuevoAngulo)
    {
        if (juntas == null || indiceJunta < 0 || indiceJunta >= juntas.Length) return;

        var junta = juntas[indiceJunta];
        junta.anguloObjetivo = Mathf.Clamp(nuevoAngulo, junta.anguloMinimo, junta.anguloMaximo);
        ActualizarMotor(junta);
    }

    void OnGUI()
    {
        if (!mostrarGUI || juntas == null || juntas.Length == 0) return;

        int anchoPanel = 320;
        int altoPanel = 70 + (juntas.Length * 65);
        GUI.Box(new Rect(15, 15, anchoPanel, altoPanel), "Control de Brazo Robótico");

        int posY = 45;
        for (int i = 0; i < juntas.Length; i++)
        {
            var junta = juntas[i];
            string textoInfo = $"{junta.nombre} [{junta.teclaNegativa}/{junta.teclaPositiva}]: {junta.anguloObjetivo:F1}°";
            GUI.Label(new Rect(25, posY, anchoPanel - 20, 20), textoInfo);

            float nuevoAngulo = GUI.HorizontalSlider(
                new Rect(25, posY + 25, anchoPanel - 40, 20),
                junta.anguloObjetivo,
                junta.anguloMinimo,
                junta.anguloMaximo
            );

            if (Mathf.Abs(nuevoAngulo - junta.anguloObjetivo) > 0.01f)
            {
                junta.anguloObjetivo = nuevoAngulo;
                ActualizarMotor(junta);
            }

            posY += 60;
        }

        GUI.Label(new Rect(25, posY + 5, anchoPanel - 20, 20), "Usa las teclas indicadas o arrastra las barras.");
    }
}
