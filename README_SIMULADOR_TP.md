# Trabajo Práctico: Prototipo con ArticulationBody
**Materia:** Simuladores · Licenciatura en Desarrollo de Videojuegos y Simulaciones · 2.º año  
**Proyecto:** Simulador de Grúa Torre de Construcción con Manipulación de Carga  
**Motor:** Unity 6 (`6000.0.69f1`) · Universal Render Pipeline (URP)

---

## 1. Resumen Ejecutivo y Cumplimiento de Consigna

El simulador implementa la operación y maniobra de una **Grúa Torre de Construcción Industrial**, diseñada con una jerarquía cinemática pura resuelta mediante el sistema de física avanzada `ArticulationBody` de Unity PhysX. 

El operador debe ejecutar una maniobra real de estiba de obra:
1. Posicionar la pluma y el carro sobre la **Zona de Recogida A**.
2. Descender el malacate de cable y pinzar el contenedor de obra (**Rigidbody** de 35 kg).
3. Izar la carga en vertical para librar un **edificio en construcción** (obstáculo central de 5 m de altura).
4. Girar la pluma 90° hacia la **Zona de Entrega B**, ajustando el radio del carro mientras compensa la oscilación del cable pasivo.
5. Descender y posar la carga sobre la plataforma de acopio elevada hasta estabilizarla para completar la misión.

### Matriz de Cumplimiento de Requisitos

| Aspecto | Mínimo para el TP | Implementación en este Proyecto | Estado |
| :--- | :--- | :--- | :---: |
| **Articulaciones con raíz** | Al menos 1 con raíz bien definida | Raíz inamovible (`immovable = true`) con zapata de hormigón | **CUMPLIDO** |
| **Cantidad de Cuerpos** | Entre 6 y 10 Articulation Bodies | **9 Articulation Bodies** exactamente calibrados | **CUMPLIDO** |
| **Tipos de Joint** | Al menos 3 distintos | **Los 4 tipos presentes:** `Fixed`, `Revolute`, `Prismatic`, `Spherical` | **CUMPLIDO** |
| **Joints con Drive Controlado** | Al menos 4 controlados | **4 controles de drive:** Pluma (Giro), Carro (Radio), Malacate (Altura), Pinza (Mordazas) | **CUMPLIDO** |
| **Joints Pasivos** | Al menos 1 parte que se mueva por gravedad | **Eslabón de cable colgante (`SphericalJoint`)** sin motor (`stiffness = 0`), oscila libremente por inercia y viento | **CUMPLIDO** |
| **Entrada** | 4 sliders y 2 botones | 4 sliders táctiles/ratón + 2 botones principales ("Abrir/Cerrar Pinza", "Reiniciar") + botón de cambio de cámara + teclado (Input System) | **CUMPLIDO** |
| **Lectura de Estado** | Target y valor real por joint en pantalla | **Telemetría en tiempo real:** Target vs Real (`jointPosition`) vs Error para cada joint + oscilación pendular en grados | **CUMPLIDO** |
| **Interacción con Físicas** | Una tarea que involucre Rigidbodies | Contenedor `Rigidbody` de 35 kg, zona de carga A, obstáculo y zona de descarga B con sensor trigger `ZonaEntregaCarga` | **CUMPLIDO** |
| **Plataforma Objetivo** | Compatible con WebGL | Físicas optimizadas a 50Hz, escala pura (1,1,1), UI modular sin dependencias nativas | **CUMPLIDO** |

---

## 2. Diagrama en Árbol Cinemático

```mermaid
graph TD
    Root["00_Base_Torre<br/>(immovable = true | Masa: 1200 kg)"] -->|FixedJoint| Body1["01_Torre_Mastil<br/>(Mástil vertical celosía 10m | Masa: 600 kg)"]
    Body1 -->|RevoluteJoint<br/>Drive 1: Giro Pluma ±170°| Body2["02_Pluma_Giratoria<br/>(Pluma 10m + Contrapluma + Contrapeso | Masa: 350 kg)"]
    Body2 -->|PrismaticJoint<br/>Drive 2: Carro Horizontal 1.5m a 8.5m| Body3["03_Carro_Distribuidor<br/>(Carro sobre rieles | Masa: 80 kg)"]
    Body3 -->|PrismaticJoint<br/>Drive 3: Malacate Vertical 0.5m a 8.0m| Body4["04_Carro_Malacate<br/>(Tambor de cable de izado | Masa: 50 kg)"]
    Body4 -->|SphericalJoint<br/>PASIVO (Sin drive / stiffness = 0)| Body5["05_Eslabon_Suspension_Pasivo<br/>(Cable colgante oscilante | Masa: 20 kg)"]
    Body5 -->|FixedJoint| Body6["06_Cabezal_Gancho<br/>(Bloque de izado industrial | Masa: 35 kg)"]
    Body6 -->|PrismaticJoint<br/>Drive 4: Mordaza Izquierda| Body7["07_Mordaza_Izquierda<br/>(Pinzado lateral | Masa: 10 kg)"]
    Body6 -->|PrismaticJoint<br/>Drive 4: Mordaza Derecha| Body8["08_Mordaza_Derecha<br/>(Pinzado lateral simétrico | Masa: 10 kg)"]

    style Root fill:#475569,stroke:#1e293b,stroke-width:2px,color:#fff
    style Body1 fill:#eab308,stroke:#ca8a04,stroke-width:2px,color:#000
    style Body2 fill:#eab308,stroke:#ca8a04,stroke-width:2px,color:#000
    style Body3 fill:#ef4444,stroke:#b91c1c,stroke-width:2px,color:#fff
    style Body4 fill:#64748b,stroke:#334155,stroke-width:2px,color:#fff
    style Body5 fill:#06b6d4,stroke:#0891b2,stroke-width:3px,color:#fff
    style Body6 fill:#f59e0b,stroke:#d97706,stroke-width:2px,color:#000
    style Body7 fill:#ef4444,stroke:#b91c1c,stroke-width:2px,color:#fff
    style Body8 fill:#ef4444,stroke:#b91c1c,stroke-width:2px,color:#fff
```

---

## 3. Especificación Técnica de Juntas y Motores

| ID | Nombre de Eslabón | Tipo de Joint | Eje de Movimiento | Rango / Límites | Masa | Stiffness | Damping | ForceLimit |
| :---: | :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **00** | `Base_Torre` | Raíz Inamovible | — | Fijo en el mundo | 1200 kg | — | — | — |
| **01** | `Torre_Mastil` | `FixedJoint` | — | Rígido | 600 kg | — | — | — |
| **02** | `Pluma_Giratoria` | `RevoluteJoint` | Eje Y (Yaw) | [-170°, +170°] | 350 kg | 80 000 | 3 000 | 100 000 |
| **03** | `Carro_Distribuidor` | `PrismaticJoint` | Eje Z (Radio) | [1.5 m, 8.5 m] | 80 kg | 40 000 | 1 500 | 50 000 |
| **04** | `Carro_Malacate` | `PrismaticJoint` | -Eje Y (Vertical) | [0.5 m, 8.0 m] | 50 kg | 50 000 | 2 000 | 60 000 |
| **05** | `Eslabon_Pasivo` | `SphericalJoint` | 3D Esférico | ±45° (Libre) | 20 kg | **0 (Pasivo)** | **0.4 (Aire)** | **0** |
| **06** | `Cabezal_Gancho` | `FixedJoint` | — | Rígido a eslabón | 35 kg | — | — | — |
| **07** | `Mordaza_Izquierda`| `PrismaticJoint` | Eje X (Lateral) | [-0.35 m, -0.08 m] | 10 kg | 25 000 | 800 | 30 000 |
| **08** | `Mordaza_Derecha`  | `PrismaticJoint` | Eje X (Lateral) | [+0.08 m, +0.35 m] | 10 kg | 25 000 | 800 | 30 000 |

---

## 4. Telemetría de Estado y Lectura en Pantalla

Siguiendo el requerimiento estricto de la cátedra:
> *"Lectura de estado: Target y valor real por joint en pantalla"*

El script `ControladorGruaTorre.cs` lee en cada `FixedUpdate` los grados y metros reales directamente desde la memoria de PhysX mediante `ArticulationBody.jointPosition`:

- **Giro Pluma**: `plumaGiratoria.jointPosition[0] * Mathf.Rad2Deg` (convirtiendo los radianes nativos de Unity PhysX a grados).
- **Carro Radio**: `carroDistribuidor.jointPosition[0]` (distancia radial real en metros).
- **Malacate Izado**: `carroMalacate.jointPosition[0]` (altura/descenso en metros).
- **Pinza**: `mordazaDerecha.jointPosition[0]` (distancia de pinzado en metros).
- **Péndulo Pasivo**: Ángulos de Euler locales de oscilación (Pitch y Roll en grados) para diagnosticar la inestabilidad de la carga.

---

## 5. Mapeo de Controles e Interacción (UI Simplificada y Compacta)

| Acción / Control | Control en Pantalla (UI) | Tecla Asignada (Input System) |
| :--- | :--- | :---: |
| **Giro de Pluma** | Slider de Giro (muestra Target y Real) | <kbd>A</kbd> / <kbd>D</kbd> o <kbd>←</kbd> / <kbd>→</kbd> |
| **Carro en el Brazo (Horizontal)** | Slider de Carro (muestra Target y Real) | <kbd>W</kbd> / <kbd>S</kbd> o <kbd>↑</kbd> / <kbd>↓</kbd> |
| **Largo Vertical (Malacate)**| Slider de Altura (muestra Target y Real) | <kbd>Q</kbd> (subir) / <kbd>E</kbd> (bajar) o <kbd>R</kbd> / <kbd>F</kbd> |
| **Bajar y Agarrar Carga** | Botón **⬇ Bajar y Agarrar** (desciende, pinza y eleva) | <kbd>Espacio</kbd> |
| **Soltar Carga** | Botón **⬆ Soltar** (abre pinzas y libera física Rigidbody) | <kbd>Espacio</kbd> |
| **Reiniciar Escenario** | Botón **Reiniciar Carga [X]** | <kbd>X</kbd> |

---

## 6. Instrucciones para la Entrega y Compilación WebGL

1. **Abrir el proyecto en Unity 6 (`6000.0.69f1`)**:
   - Abrir la escena `Assets/Scenes/SampleScene.unity`.
   - Si se desea regenerar todo desde cero con un solo clic: ir al menú superior de Unity `Simulador > Construir Escenario de Grúa Torre Completo`.
2. **Probar en Play Mode**:
   - Presionar **Play**.
   - Usar las teclas o los sliders en pantalla para maniobrar la grúa.
3. **Generar la Build WebGL para la entrega**:
   - Para compilar en WebGL, verificar que en **Unity Hub** el editor tenga instalado el módulo **WebGL Build Support** (*Unity Hub > Installs > 6000.0.69f1 > Add modules > WebGL Build Support*).
   - En Unity, seleccionar el menú `Simulador > Compilar Build WebGL` (o abrir *File > Build Settings > WebGL > Switch Platform > Build*).
   - La carpeta compilada resultante quedará lista en `Builds/WebGL` para publicar en **itch.io**, **GitHub Pages** o **Unity Play**.
