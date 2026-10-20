# Guía y Reglas del Proyecto Unity (Articulation)

Este archivo define el contexto, estándares y flujos de trabajo para Antigravity en este proyecto.

## 1. Información del Entorno
- **Motor**: Unity 6 (`6000.0.69f1`).
- **Render Pipeline**: Universal Render Pipeline (URP).
- **Enfoque Principal**: Simulación de física y robótica utilizando componentes `ArticulationBody` (brazos robóticos, cinemática, articulaciones continuas/revolute).
- **Lenguaje**: C# (.NET / Mono).

## 2. Reglas de Validación de Código (Estrictas)
1. **Verificación de Compilación**:
   - Cada vez que se agreguen o modifiquen scripts C#, Antigravity **debe ejecutar automáticamente**:
     ```powershell
     dotnet build Articulation.sln
     ```
   - No dar por terminada una tarea con errores de compilación o warnings críticos.
2. **Integridad de Assets y Archivos `.meta`**:
   - No crear, renombrar o borrar archivos `.meta` manualmente salvo que sea estrictamente necesario.
   - Dejar que Unity genere y sincronice sus propios metadatos cuando se creen nuevos scripts o prefabs.
3. **Separación de Contextos (Runtime vs Editor)**:
   - Scripts que usen `UnityEditor` (`[MenuItem]`, `EditorWindow`, `CustomEditor`) deben residir obligatoriamente dentro de carpetas `Editor/` (ejemplo: `Assets/Scripts/Editor/`).
   - El código de juego/simulación vive en `Assets/Scripts/`.

## 3. Convenciones para ArticulationBody
- Tipos de Juntas (`ArticulationJointType`):
  - Usar siempre los nombres de enum exactos de Unity: `RevoluteJoint`, `PrismaticJoint`, `FixedJoint`, `SphericalJoint`.
- Parámetros de Motor (`ArticulationDrive`):
  - Las articulaciones `RevoluteJoint` controlan su movimiento a través de `xDrive`.
  - Configurar siempre `stiffness`, `damping`, `forceLimit` y `target` coherentes para evitar explosiones de físicas o temblores en las juntas.
- El cuerpo raíz de una base fija debe tener `immovable = true`.

## 4. Automatización y Control del Editor
- Siempre que se requiera armar una estructura repetitiva (como robots de múltiples eslabones, colliders o configuraciones de juntas), proveer herramientas de editor (`[MenuItem]` o `EditorWindow`) para que el usuario pueda instanciarlas o regenerarlas con un solo clic en Unity.
- Para inspeccionar errores en tiempo de ejecución de Unity, consultar el log en `%LOCALAPPDATA%\Unity\Editor\Editor.log`.
