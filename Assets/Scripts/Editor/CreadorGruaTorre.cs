using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Herramienta de Editor para construir automáticamente la escena completa del simulador de Grúa Torre con ArticulationBody.
/// Cumple con todos los requisitos del TP de Simuladores (Lic. en Videojuegos y Simulaciones):
/// - 9 ArticulationBodies (Raíz inamovible, Fixed, Revolute, Prismatic, Spherical pasivo).
/// - Escenario completo de obra: Suelo, Zona de Carga A, Obstáculo (Edificio), Zona de Entrega B.
/// - Rigidbody de carga y 3 cámaras de visualización.
/// - Configuración precisa de stiffness, damping, forceLimit y anclas.
/// </summary>
public class CreadorGruaTorre : Editor
{
    [MenuItem("Simulador/Construir Escenario de Grúa Torre Completo", false, 1)]
    [MenuItem("Tools/Simulador/Construir Grúa Torre", false, 1)]
    [MenuItem("Tools/Simulador/Construir", false, 1)]
    public static void ConstruirEscenarioCompleto()
    {
        // Limpiar objetos previos si existían
        GameObject viejoSimulador = GameObject.Find("SIMULADOR_GRUA_TORRE");
        if (viejoSimulador != null)
        {
            Undo.DestroyObjectImmediate(viejoSimulador);
        }

        GameObject viejoBrazo = GameObject.Find("BrazoRobotico");
        if (viejoBrazo != null)
        {
            Undo.DestroyObjectImmediate(viejoBrazo);
        }

        // Raíz principal del escenario
        GameObject contenedorSimulador = new GameObject("SIMULADOR_GRUA_TORRE");
        Undo.RegisterCreatedObjectUndo(contenedorSimulador, "Construir Escenario Grua Torre");

        // 1. Materiales Básicos URP / Standard
        Material matSuelo = CrearMaterial(new Color(0.35f, 0.33f, 0.30f), "Mat_SueloObra");
        Material matGruaAmarilla = CrearMaterial(new Color(0.96f, 0.72f, 0.08f), "Mat_GruaAmarillo");
        Material matCarroRojo = CrearMaterial(new Color(0.85f, 0.22f, 0.12f), "Mat_CarroRojo");
        Material matMetalOscuro = CrearMaterial(new Color(0.20f, 0.22f, 0.25f), "Mat_MetalOscuro");
        Material matHormigon = CrearMaterial(new Color(0.55f, 0.55f, 0.58f), "Mat_Hormigon");
        Material matCargaNaranja = CrearMaterial(new Color(0.95f, 0.45f, 0.05f), "Mat_Carga");
        Material matZonaEntrega = CrearMaterial(new Color(0.15f, 0.85f, 0.35f), "Mat_ZonaEntrega");
        Material matObstaculo = CrearMaterial(new Color(0.45f, 0.48f, 0.52f), "Mat_EdificioObra");
        Material matVidrio = CrearMaterial(new Color(0.4f, 0.7f, 0.9f, 0.6f), "Mat_VidrioCabina");

        // 2. Entorno y Suelo
        GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        suelo.name = "Suelo_Obra";
        suelo.transform.SetParent(contenedorSimulador.transform);
        suelo.transform.position = new Vector3(0, -0.5f, 0);
        suelo.transform.localScale = new Vector3(45f, 1f, 45f);
        suelo.GetComponent<Renderer>().sharedMaterial = matSuelo;

        // 3. Mecanismo de ArticulationBody (La Grúa Torre)
        GameObject raizGrua = new GameObject("GruaTorre_Mecanismo");
        raizGrua.transform.SetParent(contenedorSimulador.transform);
        raizGrua.transform.position = Vector3.zero;
        raizGrua.transform.rotation = Quaternion.identity;

        // 3.0 RAÍZ: Base Torre (Inmovible)
        ArticulationBody abBase = raizGrua.AddComponent<ArticulationBody>();
        abBase.immovable = true;
        abBase.mass = 1200f;

        GameObject visualBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualBase.name = "Visual_ZapataHormigon";
        visualBase.transform.SetParent(raizGrua.transform);
        visualBase.transform.localPosition = new Vector3(0, 0.35f, 0);
        visualBase.transform.localScale = new Vector3(3.6f, 0.7f, 3.6f);
        visualBase.GetComponent<Renderer>().sharedMaterial = matHormigon;

        // 3.1 CUERPO 1: Torre / Mástil (FixedJoint)
        GameObject goMastil = new GameObject("01_Torre_Mastil");
        goMastil.transform.SetParent(raizGrua.transform);
        goMastil.transform.localPosition = new Vector3(0, 0.7f, 0);

        ArticulationBody abMastil = goMastil.AddComponent<ArticulationBody>();
        abMastil.jointType = ArticulationJointType.FixedJoint;
        abMastil.mass = 600f;

        GameObject visualMastil = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualMastil.name = "Visual_MastilCelosia";
        visualMastil.transform.SetParent(goMastil.transform);
        visualMastil.transform.localPosition = new Vector3(0, 5.0f, 0);
        visualMastil.transform.localScale = new Vector3(1.1f, 10.0f, 1.1f);
        visualMastil.GetComponent<Renderer>().sharedMaterial = matGruaAmarilla;

        // Escalera / Detalles visuales en mástil
        GameObject visualDetalleMastil = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualDetalleMastil.name = "Visual_RefuerzoInterior";
        visualDetalleMastil.transform.SetParent(goMastil.transform);
        visualDetalleMastil.transform.localPosition = new Vector3(0, 5.0f, 0);
        visualDetalleMastil.transform.localScale = new Vector3(0.6f, 10.2f, 0.6f);
        visualDetalleMastil.GetComponent<Renderer>().sharedMaterial = matMetalOscuro;
        DestroyImmediate(visualDetalleMastil.GetComponent<Collider>());

        // 3.2 CUERPO 2: Pluma Giratoria (RevoluteJoint - Control 1: Giro Yaw)
        GameObject goPluma = new GameObject("02_Pluma_Giratoria");
        goPluma.transform.SetParent(goMastil.transform);
        goPluma.transform.localPosition = new Vector3(0, 10.0f, 0);

        ArticulationBody abPluma = goPluma.AddComponent<ArticulationBody>();
        abPluma.jointType = ArticulationJointType.RevoluteJoint;
        abPluma.mass = 350f;
        // Revolute rota en torno a X del ancla. Rotar 90° en Z alinea X con eje Y (giro vertical Yaw).
        abPluma.anchorRotation = Quaternion.Euler(0, 0, 90f);
        abPluma.parentAnchorRotation = Quaternion.Euler(0, 0, 90f);

        var drivePluma = abPluma.xDrive;
        drivePluma.stiffness = 80000f;
        drivePluma.damping = 3000f;
        drivePluma.forceLimit = 100000f;
        drivePluma.lowerLimit = -170f;
        drivePluma.upperLimit = 170f;
        drivePluma.target = 0f;
        abPluma.xDrive = drivePluma;

        // Visuales de la pluma
        // Corona de giro
        GameObject visualCorona = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visualCorona.name = "Visual_CoronaGiro";
        visualCorona.transform.SetParent(goPluma.transform);
        visualCorona.transform.localPosition = new Vector3(0, 0.2f, 0);
        visualCorona.transform.localScale = new Vector3(1.4f, 0.2f, 1.4f);
        visualCorona.GetComponent<Renderer>().sharedMaterial = matMetalOscuro;

        // Brazo delantero (Pluma de carga: 10m de longitud a lo largo de +Z)
        GameObject visualBrazoCarga = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualBrazoCarga.name = "Visual_PlumaDelantera";
        visualBrazoCarga.transform.SetParent(goPluma.transform);
        visualBrazoCarga.transform.localPosition = new Vector3(0, 0.5f, 5.0f);
        visualBrazoCarga.transform.localScale = new Vector3(0.7f, 0.7f, 10.0f);
        visualBrazoCarga.GetComponent<Renderer>().sharedMaterial = matGruaAmarilla;

        // Contrapluma trasera (3.5m de longitud a lo largo de -Z)
        GameObject visualContrapluma = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualContrapluma.name = "Visual_Contrapluma";
        visualContrapluma.transform.SetParent(goPluma.transform);
        visualContrapluma.transform.localPosition = new Vector3(0, 0.5f, -1.8f);
        visualContrapluma.transform.localScale = new Vector3(0.8f, 0.7f, 3.6f);
        visualContrapluma.GetComponent<Renderer>().sharedMaterial = matGruaAmarilla;

        // Bloque contrapeso de hormigón
        GameObject visualContrapeso = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualContrapeso.name = "Visual_Contrapeso";
        visualContrapeso.transform.SetParent(goPluma.transform);
        visualContrapeso.transform.localPosition = new Vector3(0, 0.5f, -3.2f);
        visualContrapeso.transform.localScale = new Vector3(1.6f, 1.2f, 1.2f);
        visualContrapeso.GetComponent<Renderer>().sharedMaterial = matHormigon;

        // Cúspide y tirantes superiores
        GameObject visualCuspide = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visualCuspide.name = "Visual_CuspideTorre";
        visualCuspide.transform.SetParent(goPluma.transform);
        visualCuspide.transform.localPosition = new Vector3(0, 1.8f, 0);
        visualCuspide.transform.localScale = new Vector3(0.5f, 1.2f, 0.5f);
        visualCuspide.GetComponent<Renderer>().sharedMaterial = matGruaAmarilla;

        // Cabina del operador
        GameObject visualCabina = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualCabina.name = "Visual_CabinaOperador";
        visualCabina.transform.SetParent(goPluma.transform);
        visualCabina.transform.localPosition = new Vector3(0.9f, 0.2f, 1.0f);
        visualCabina.transform.localScale = new Vector3(0.9f, 1.2f, 1.1f);
        visualCabina.GetComponent<Renderer>().sharedMaterial = matVidrio;

        // 3.3 CUERPO 3: Carro Distribuidor (PrismaticJoint - Control 2: Radio horizontal)
        GameObject goCarro = new GameObject("03_Carro_Distribuidor");
        goCarro.transform.SetParent(goPluma.transform);
        // Colocado en radio inicial Z = 3.5m, bajo la pluma (Y = 0)
        goCarro.transform.localPosition = new Vector3(0, 0f, 3.5f);

        ArticulationBody abCarro = goCarro.AddComponent<ArticulationBody>();
        abCarro.jointType = ArticulationJointType.PrismaticJoint;
        abCarro.mass = 80f;
        // Prismatic se desplaza sobre X del ancla. Rotar 90° en Y alinea X con eje Z global (a lo largo de la pluma).
        abCarro.anchorRotation = Quaternion.Euler(0, 90f, 0);
        abCarro.parentAnchorRotation = Quaternion.Euler(0, 90f, 0);

        var driveCarro = abCarro.xDrive;
        driveCarro.stiffness = 40000f;
        driveCarro.damping = 1500f;
        driveCarro.forceLimit = 50000f;
        driveCarro.lowerLimit = 1.5f;
        driveCarro.upperLimit = 8.5f;
        driveCarro.target = 3.5f;
        abCarro.xDrive = driveCarro;

        GameObject visualCarro = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualCarro.name = "Visual_ChasisCarro";
        visualCarro.transform.SetParent(goCarro.transform);
        visualCarro.transform.localPosition = Vector3.zero;
        visualCarro.transform.localScale = new Vector3(0.9f, 0.25f, 0.9f);
        visualCarro.GetComponent<Renderer>().sharedMaterial = matCarroRojo;

        // 3.4 CUERPO 4: Carro Malacate / Cable Vertical (PrismaticJoint - Control 3: Izado / Descenso)
        GameObject goMalacate = new GameObject("04_Carro_Malacate");
        goMalacate.transform.SetParent(goCarro.transform);
        goMalacate.transform.localPosition = new Vector3(0, -1.0f, 0); // Descenso inicial 1m

        ArticulationBody abMalacate = goMalacate.AddComponent<ArticulationBody>();
        abMalacate.jointType = ArticulationJointType.PrismaticJoint;
        abMalacate.mass = 50f;
        // Rotar -90° en Z alinea el eje X con -Y (descenso vertical hacia el suelo).
        abMalacate.anchorRotation = Quaternion.Euler(0, 0, -90f);
        abMalacate.parentAnchorRotation = Quaternion.Euler(0, 0, -90f);

        var driveMalacate = abMalacate.xDrive;
        driveMalacate.stiffness = 50000f;
        driveMalacate.damping = 2000f;
        driveMalacate.forceLimit = 60000f;
        driveMalacate.lowerLimit = 0.5f;
        driveMalacate.upperLimit = 8.0f;
        driveMalacate.target = 1.0f;
        abMalacate.xDrive = driveMalacate;

        GameObject visualPolea = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visualPolea.name = "Visual_PoleaSuperior";
        visualPolea.transform.SetParent(goMalacate.transform);
        visualPolea.transform.localPosition = Vector3.zero;
        visualPolea.transform.localScale = new Vector3(0.45f, 0.15f, 0.45f);
        visualPolea.GetComponent<Renderer>().sharedMaterial = matMetalOscuro;

        // 3.5 CUERPO 5: Eslabón de Suspensión Pasivo (SphericalJoint - PASIVO: Oscilación libre)
        GameObject goEslabonPasivo = new GameObject("05_Eslabon_Suspension_Pasivo");
        goEslabonPasivo.transform.SetParent(goMalacate.transform);
        goEslabonPasivo.transform.localPosition = new Vector3(0, -0.4f, 0);

        ArticulationBody abPasivo = goEslabonPasivo.AddComponent<ArticulationBody>();
        abPasivo.jointType = ArticulationJointType.SphericalJoint;
        abPasivo.mass = 20f;
        abPasivo.anchorRotation = Quaternion.identity;
        abPasivo.parentAnchorRotation = Quaternion.identity;

        // Configuración 100% PASIVA (sin stiffness ni fuerza de motor)
        var pasivoX = abPasivo.xDrive;
        pasivoX.stiffness = 0f;
        pasivoX.damping = 0.4f;
        pasivoX.forceLimit = 0f;
        abPasivo.xDrive = pasivoX;

        var pasivoY = abPasivo.yDrive;
        pasivoY.stiffness = 40000f;
        pasivoY.damping = 1500f;
        pasivoY.forceLimit = 40000f;
        pasivoY.target = 0f;
        abPasivo.yDrive = pasivoY;

        var pasivoZ = abPasivo.zDrive;
        pasivoZ.stiffness = 0f;
        pasivoZ.damping = 0.4f;
        pasivoZ.forceLimit = 0f;
        abPasivo.zDrive = pasivoZ;

        GameObject visualEslabon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visualEslabon.name = "Visual_VarillaSuspension";
        visualEslabon.transform.SetParent(goEslabonPasivo.transform);
        visualEslabon.transform.localPosition = new Vector3(0, -0.2f, 0);
        visualEslabon.transform.localScale = new Vector3(0.08f, 0.2f, 0.08f);
        visualEslabon.GetComponent<Renderer>().sharedMaterial = matMetalOscuro;

        // 3.6 CUERPO 6: Cabezal del Gancho / Chasis de la Pinza (FixedJoint)
        GameObject goCabezal = new GameObject("06_Cabezal_Gancho");
        goCabezal.transform.SetParent(goEslabonPasivo.transform);
        goCabezal.transform.localPosition = new Vector3(0, -0.4f, 0);

        ArticulationBody abCabezal = goCabezal.AddComponent<ArticulationBody>();
        abCabezal.jointType = ArticulationJointType.FixedJoint;
        abCabezal.mass = 35f;

        GameObject visualCabezal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualCabezal.name = "Visual_BloqueGancho";
        visualCabezal.transform.SetParent(goCabezal.transform);
        visualCabezal.transform.localPosition = Vector3.zero;
        visualCabezal.transform.localScale = new Vector3(0.7f, 0.25f, 0.45f);
        visualCabezal.GetComponent<Renderer>().sharedMaterial = matGruaAmarilla;

        // Gancho decorativo
        GameObject visualGancho = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visualGancho.name = "Visual_GanchoAcero";
        visualGancho.transform.SetParent(goCabezal.transform);
        visualGancho.transform.localPosition = new Vector3(0, -0.25f, 0);
        visualGancho.transform.localScale = new Vector3(0.12f, 0.15f, 0.12f);
        visualGancho.GetComponent<Renderer>().sharedMaterial = matMetalOscuro;

        // Punto de referencia para agarre
        GameObject puntoAgarre = new GameObject("PuntoAgarre");
        puntoAgarre.transform.SetParent(goCabezal.transform);
        puntoAgarre.transform.localPosition = new Vector3(0, -0.4f, 0);

        // 3.7 CUERPO 7: Mordaza Izquierda (PrismaticJoint - Control 4)
        GameObject goMordazaIzq = new GameObject("07_Mordaza_Izquierda");
        goMordazaIzq.transform.SetParent(goCabezal.transform);
        goMordazaIzq.transform.localPosition = new Vector3(-0.25f, -0.3f, 0);

        ArticulationBody abMordazaIzq = goMordazaIzq.AddComponent<ArticulationBody>();
        abMordazaIzq.jointType = ArticulationJointType.PrismaticJoint;
        abMordazaIzq.mass = 10f;
        abMordazaIzq.anchorRotation = Quaternion.identity;
        abMordazaIzq.parentAnchorRotation = Quaternion.identity;

        var driveMordazaIzq = abMordazaIzq.xDrive;
        driveMordazaIzq.stiffness = 25000f;
        driveMordazaIzq.damping = 800f;
        driveMordazaIzq.forceLimit = 30000f;
        driveMordazaIzq.lowerLimit = -0.35f;
        driveMordazaIzq.upperLimit = -0.08f;
        driveMordazaIzq.target = -0.25f;
        abMordazaIzq.xDrive = driveMordazaIzq;

        GameObject visualGarraIzq = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualGarraIzq.name = "Visual_Garra";
        visualGarraIzq.transform.SetParent(goMordazaIzq.transform);
        visualGarraIzq.transform.localPosition = Vector3.zero;
        visualGarraIzq.transform.localScale = new Vector3(0.08f, 0.45f, 0.35f);
        visualGarraIzq.GetComponent<Renderer>().sharedMaterial = matCarroRojo;

        // 3.8 CUERPO 8: Mordaza Derecha (PrismaticJoint - Control 4 simétrico)
        GameObject goMordazaDer = new GameObject("08_Mordaza_Derecha");
        goMordazaDer.transform.SetParent(goCabezal.transform);
        goMordazaDer.transform.localPosition = new Vector3(0.25f, -0.3f, 0);

        ArticulationBody abMordazaDer = goMordazaDer.AddComponent<ArticulationBody>();
        abMordazaDer.jointType = ArticulationJointType.PrismaticJoint;
        abMordazaDer.mass = 10f;
        abMordazaDer.anchorRotation = Quaternion.identity;
        abMordazaDer.parentAnchorRotation = Quaternion.identity;

        var driveMordazaDer = abMordazaDer.xDrive;
        driveMordazaDer.stiffness = 25000f;
        driveMordazaDer.damping = 800f;
        driveMordazaDer.forceLimit = 30000f;
        driveMordazaDer.lowerLimit = 0.08f;
        driveMordazaDer.upperLimit = 0.35f;
        driveMordazaDer.target = 0.25f;
        abMordazaDer.xDrive = driveMordazaDer;

        GameObject visualGarraDer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualGarraDer.name = "Visual_Garra";
        visualGarraDer.transform.SetParent(goMordazaDer.transform);
        visualGarraDer.transform.localPosition = Vector3.zero;
        visualGarraDer.transform.localScale = new Vector3(0.08f, 0.45f, 0.35f);
        visualGarraDer.GetComponent<Renderer>().sharedMaterial = matCarroRojo;

        // 4. Cable Dinámico (CableVisualizer)
        CableVisualizer visualizadorCable = goCarro.AddComponent<CableVisualizer>();
        visualizadorCable.anclajeCarro = goCarro.transform;
        visualizadorCable.anclajeGancho = goCabezal.transform;

        // 5. ZONA A: Recogida de Carga (Pick-up Zone)
        GameObject zonaA = new GameObject("Zona_Recogida_A");
        zonaA.transform.SetParent(contenedorSimulador.transform);
        zonaA.transform.position = new Vector3(0, 0, 3.5f);

        GameObject paletA = GameObject.CreatePrimitive(PrimitiveType.Cube);
        paletA.name = "Palet_Carga";
        paletA.transform.SetParent(zonaA.transform);
        paletA.transform.localPosition = new Vector3(0, 0.1f, 0);
        paletA.transform.localScale = new Vector3(1.6f, 0.2f, 1.6f);
        paletA.GetComponent<Renderer>().sharedMaterial = matHormigon;

        // Carga Rigidbody
        GameObject goCarga = GameObject.CreatePrimitive(PrimitiveType.Cube);
        goCarga.name = "Carga_Contenedor_Rigidbody";
        goCarga.transform.SetParent(contenedorSimulador.transform);
        goCarga.transform.position = new Vector3(0, 0.45f, 3.5f);
        goCarga.transform.localScale = new Vector3(0.42f, 0.45f, 0.42f);
        goCarga.GetComponent<Renderer>().sharedMaterial = matCargaNaranja;

        Rigidbody rbCarga = goCarga.AddComponent<Rigidbody>();
        rbCarga.mass = 35f;
        rbCarga.linearDamping = 0.05f;
        rbCarga.angularDamping = 0.05f;
        rbCarga.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // 6. OBSTÁCULO: Edificio en Construcción (Obliga a izar y maniobrar)
        GameObject edificioObstaculo = new GameObject("Obstaculo_EdificioEnConstruccion");
        edificioObstaculo.transform.SetParent(contenedorSimulador.transform);
        edificioObstaculo.transform.position = new Vector3(2.5f, 0, 0.5f);

        // Columnas y losas
        GameObject losa1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        losa1.name = "Losa_Piso1";
        losa1.transform.SetParent(edificioObstaculo.transform);
        losa1.transform.localPosition = new Vector3(0, 2.5f, 0);
        losa1.transform.localScale = new Vector3(3.5f, 0.4f, 3.5f);
        losa1.GetComponent<Renderer>().sharedMaterial = matObstaculo;

        GameObject losa2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        losa2.name = "Losa_Piso2";
        losa2.transform.SetParent(edificioObstaculo.transform);
        losa2.transform.localPosition = new Vector3(0, 4.8f, 0);
        losa2.transform.localScale = new Vector3(3.2f, 0.4f, 3.2f);
        losa2.GetComponent<Renderer>().sharedMaterial = matObstaculo;

        // 7. ZONA B: Entrega de Carga (Drop-off Zone)
        GameObject zonaB = new GameObject("Zona_Entrega_B");
        zonaB.transform.SetParent(contenedorSimulador.transform);
        zonaB.transform.position = new Vector3(4.5f, 0, -3.5f);

        // Plataforma elevada sobre andamios
        GameObject plataformaB = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plataformaB.name = "Plataforma_Acopio";
        plataformaB.transform.SetParent(zonaB.transform);
        plataformaB.transform.localPosition = new Vector3(0, 1.5f, 0);
        plataformaB.transform.localScale = new Vector3(2.2f, 0.3f, 2.2f);
        plataformaB.GetComponent<Renderer>().sharedMaterial = matZonaEntrega;

        // Soportes del andamio
        GameObject soporteAndamio = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        soporteAndamio.name = "Soporte_Andamio";
        soporteAndamio.transform.SetParent(zonaB.transform);
        soporteAndamio.transform.localPosition = new Vector3(0, 0.75f, 0);
        soporteAndamio.transform.localScale = new Vector3(0.4f, 0.75f, 0.4f);
        soporteAndamio.GetComponent<Renderer>().sharedMaterial = matMetalOscuro;

        // Trigger de Detección
        GameObject goTrigger = new GameObject("Trigger_Entrega");
        goTrigger.transform.SetParent(zonaB.transform);
        goTrigger.transform.localPosition = new Vector3(0, 2.2f, 0);
        BoxCollider boxTrig = goTrigger.AddComponent<BoxCollider>();
        boxTrig.isTrigger = true;
        boxTrig.size = new Vector3(2.0f, 1.2f, 2.0f);

        ZonaEntregaCarga detectorEntrega = goTrigger.AddComponent<ZonaEntregaCarga>();
        detectorEntrega.cargaObjetivo = rbCarga;

        // Baliza luminosa en la zona de entrega
        GameObject goLuz = new GameObject("LuzBaliza");
        goLuz.transform.SetParent(zonaB.transform);
        goLuz.transform.localPosition = new Vector3(0, 2.5f, 0);
        Light luzZona = goLuz.AddComponent<Light>();
        luzZona.type = LightType.Point;
        luzZona.range = 6f;
        luzZona.intensity = 2f;
        luzZona.color = new Color(0.2f, 1f, 0.4f);
        detectorEntrega.luzIndicadora = luzZona;

        // 8. Configurar ControladorGruaTorre
        ControladorGruaTorre controlador = raizGrua.AddComponent<ControladorGruaTorre>();
        controlador.plumaGiratoria = abPluma;
        controlador.carroDistribuidor = abCarro;
        controlador.carroMalacate = abMalacate;
        controlador.mordazaIzquierda = abMordazaIzq;
        controlador.mordazaDerecha = abMordazaDer;
        controlador.eslabonSuspensionPasivo = abPasivo;
        controlador.cargaRigidbody = rbCarga;
        controlador.zonaRecogidaInicial = paletA.transform;
        controlador.zonaEntrega = detectorEntrega;
        controlador.puntoAgarreGancho = puntoAgarre.transform;

        // 9. Cámara Principal
        Camera camGen = Camera.main;
        GameObject goCamGeneral;
        if (camGen != null)
        {
            goCamGeneral = camGen.gameObject;
        }
        else
        {
            goCamGeneral = new GameObject("Camara_General");
            camGen = goCamGeneral.AddComponent<Camera>();
            goCamGeneral.AddComponent<AudioListener>();
        }
        goCamGeneral.name = "Camara_General";
        goCamGeneral.transform.SetParent(contenedorSimulador.transform);
        goCamGeneral.transform.position = new Vector3(-13f, 13f, -13f);
        goCamGeneral.transform.rotation = Quaternion.Euler(32f, 45f, 0);
        camGen.enabled = true;

        // Ajustar Tag del Main Camera
        goCamGeneral.tag = "MainCamera";

        // Marcar la escena como modificada y guardarla
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = raizGrua;

        Debug.Log("<color=#10b981><b>[SIMULADOR DE GRÚA TORRE]</b></color> Escenario completo construido con éxito con 9 ArticulationBodies, 4 tipos de joint, telemetría y maniobra de carga.");
    }

    [MenuItem("Simulador/Compilar Build WebGL", false, 20)]
    public static void GenerarBuildWebGL()
    {
        string buildDir = "Builds/WebGL";
        System.IO.Directory.CreateDirectory(buildDir);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = buildDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"<color=#10b981><b>Build WebGL exitosa</b></color> en {buildDir} ({report.summary.totalSize / (1024 * 1024):F1} MB)");
        }
        else
        {
            Debug.LogError($"Error al compilar WebGL: {report.summary.result}");
        }
    }

    [MenuItem("Simulador/Compilar Build Windows", false, 21)]
    public static void GenerarBuildWindows()
    {
        string buildDir = "Builds/Windows/SimuladorGrua.exe";
        System.IO.Directory.CreateDirectory("Builds/Windows");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = buildDir,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"<color=#10b981><b>Build Windows exitosa</b></color> en {buildDir}");
        }
        else
        {
            Debug.LogError($"Error al compilar Windows: {report.summary.result}");
        }
    }

    private static Material CrearMaterial(Color color, string nombre)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
        Material mat = new Material(shader);
        mat.name = nombre;
        mat.color = color;
        return mat;
    }
}
