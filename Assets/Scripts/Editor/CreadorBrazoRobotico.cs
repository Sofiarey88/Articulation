using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

public class CreadorBrazoRobotico : Editor
{
    [MenuItem("Tools/Crear Brazo Robótico")]
    [MenuItem("GameObject/3D Object/Brazo Robótico (ArticulationBody)", false, 10)]
    public static void CrearBrazo()
    {
        // 1. Objeto Principal (Raíz con ArticulationBody Inamovible)
        GameObject raiz = new GameObject("BrazoRobotico");
        raiz.transform.position = Vector3.zero;

        ArticulationBody abRaiz = raiz.AddComponent<ArticulationBody>();
        abRaiz.immovable = true;
        abRaiz.mass = 50f;

        // Base visual
        GameObject baseVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseVisual.name = "Visual_Base";
        baseVisual.transform.SetParent(raiz.transform);
        baseVisual.transform.localPosition = new Vector3(0, 0.15f, 0);
        baseVisual.transform.localScale = new Vector3(1.2f, 0.15f, 1.2f);
        ConfigurarColor(baseVisual, new Color(0.2f, 0.2f, 0.25f));

        // 2. Eslabón 1: Giro Base (Torreta / Yaw)
        GameObject eslabon1 = new GameObject("1_GiroBase");
        eslabon1.transform.SetParent(raiz.transform);
        eslabon1.transform.localPosition = new Vector3(0, 0.3f, 0);

        ArticulationBody ab1 = eslabon1.AddComponent<ArticulationBody>();
        ab1.jointType = ArticulationJointType.RevoluteJoint;
        // En ArticulationBody, revolute gira sobre el eje X del ancla.
        // Rotamos 90 grados en Z para que el eje X apunte hacia arriba (eje Y global).
        ab1.anchorRotation = Quaternion.Euler(0, 0, 90f);
        ab1.parentAnchorRotation = Quaternion.Euler(0, 0, 90f);
        ConfigurarDrive(ab1, -180f, 180f);

        GameObject eslabon1Visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        eslabon1Visual.name = "Visual_Torreta";
        eslabon1Visual.transform.SetParent(eslabon1.transform);
        eslabon1Visual.transform.localPosition = new Vector3(0, 0.2f, 0);
        eslabon1Visual.transform.localScale = new Vector3(0.8f, 0.2f, 0.8f);
        ConfigurarColor(eslabon1Visual, new Color(0.85f, 0.45f, 0.1f));

        // 3. Eslabón 2: Hombro (Pitch - eje X horizontal)
        GameObject eslabon2 = new GameObject("2_Hombro");
        eslabon2.transform.SetParent(eslabon1.transform);
        eslabon2.transform.localPosition = new Vector3(0, 0.4f, 0);

        ArticulationBody ab2 = eslabon2.AddComponent<ArticulationBody>();
        ab2.jointType = ArticulationJointType.RevoluteJoint;
        // Rota sobre el eje X natural (cabeceo / pitch)
        ab2.anchorRotation = Quaternion.identity;
        ab2.parentAnchorRotation = Quaternion.identity;
        ConfigurarDrive(ab2, -75f, 75f);

        GameObject eslabon2Visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        eslabon2Visual.name = "Visual_Brazo";
        eslabon2Visual.transform.SetParent(eslabon2.transform);
        eslabon2Visual.transform.localPosition = new Vector3(0, 0.6f, 0);
        eslabon2Visual.transform.localScale = new Vector3(0.3f, 1.2f, 0.3f);
        ConfigurarColor(eslabon2Visual, new Color(0.2f, 0.55f, 0.85f));

        // 4. Eslabón 3: Codo (Pitch - eje X horizontal)
        GameObject eslabon3 = new GameObject("3_Codo");
        eslabon3.transform.SetParent(eslabon2.transform);
        eslabon3.transform.localPosition = new Vector3(0, 1.2f, 0);

        ArticulationBody ab3 = eslabon3.AddComponent<ArticulationBody>();
        ab3.jointType = ArticulationJointType.RevoluteJoint;
        ab3.anchorRotation = Quaternion.identity;
        ab3.parentAnchorRotation = Quaternion.identity;
        ConfigurarDrive(ab3, -110f, 110f);

        GameObject eslabon3Visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        eslabon3Visual.name = "Visual_Antebrazo";
        eslabon3Visual.transform.SetParent(eslabon3.transform);
        eslabon3Visual.transform.localPosition = new Vector3(0, 0.5f, 0);
        eslabon3Visual.transform.localScale = new Vector3(0.25f, 1.0f, 0.25f);
        ConfigurarColor(eslabon3Visual, new Color(0.9f, 0.75f, 0.15f));

        // 5. Eslabón 4: Muñeca / Pinza
        GameObject eslabon4 = new GameObject("4_Muneca");
        eslabon4.transform.SetParent(eslabon3.transform);
        eslabon4.transform.localPosition = new Vector3(0, 1.0f, 0);

        ArticulationBody ab4 = eslabon4.AddComponent<ArticulationBody>();
        ab4.jointType = ArticulationJointType.RevoluteJoint;
        ab4.anchorRotation = Quaternion.identity;
        ab4.parentAnchorRotation = Quaternion.identity;
        ConfigurarDrive(ab4, -90f, 90f);

        GameObject eslabon4Visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        eslabon4Visual.name = "Visual_Mano";
        eslabon4Visual.transform.SetParent(eslabon4.transform);
        eslabon4Visual.transform.localPosition = new Vector3(0, 0.2f, 0);
        eslabon4Visual.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        ConfigurarColor(eslabon4Visual, new Color(0.8f, 0.2f, 0.2f));

        // 6. Añadir y configurar el script ControladorBrazo
        ControladorBrazo controlador = raiz.AddComponent<ControladorBrazo>();
        controlador.juntas = new ControladorBrazo.JuntaRobot[4];

        controlador.juntas[0] = new ControladorBrazo.JuntaRobot
        {
            nombre = "Giro Base",
            articulacion = ab1,
            teclaNegativa = Key.A,
            teclaPositiva = Key.D,
            anguloMinimo = -180f,
            anguloMaximo = 180f,
            velocidad = 60f
        };

        controlador.juntas[1] = new ControladorBrazo.JuntaRobot
        {
            nombre = "Hombro",
            articulacion = ab2,
            teclaNegativa = Key.S,
            teclaPositiva = Key.W,
            anguloMinimo = -75f,
            anguloMaximo = 75f,
            velocidad = 50f
        };

        controlador.juntas[2] = new ControladorBrazo.JuntaRobot
        {
            nombre = "Codo",
            articulacion = ab3,
            teclaNegativa = Key.Q,
            teclaPositiva = Key.E,
            anguloMinimo = -110f,
            anguloMaximo = 110f,
            velocidad = 55f
        };

        controlador.juntas[3] = new ControladorBrazo.JuntaRobot
        {
            nombre = "Muñeca",
            articulacion = ab4,
            teclaNegativa = Key.F,
            teclaPositiva = Key.R,
            anguloMinimo = -90f,
            anguloMaximo = 90f,
            velocidad = 70f
        };

        Undo.RegisterCreatedObjectUndo(raiz, "Crear Brazo Robotico");
        Selection.activeGameObject = raiz;
        Debug.Log("¡Brazo Robótico con ArticulationBody creado exitosamente!");
    }

    private static void ConfigurarDrive(ArticulationBody ab, float min, float max)
    {
        ab.mass = 5f;
        ArticulationDrive drive = ab.xDrive;
        drive.stiffness = 25000f;
        drive.damping = 600f;
        drive.forceLimit = 25000f;
        drive.lowerLimit = min;
        drive.upperLimit = max;
        drive.target = 0f;
        ab.xDrive = drive;
    }

    private static void ConfigurarColor(GameObject go, Color color)
    {
        Renderer rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Diffuse"));
            mat.color = color;
            rend.sharedMaterial = mat;
        }
    }
}
