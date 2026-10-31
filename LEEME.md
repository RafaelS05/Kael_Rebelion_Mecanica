# Kael: Rebelión Mecánica: prototipo del Avance 2

## Cómo instalarlo (una sola vez)

1. **Crear el proyecto en Unity Hub:** *New project* → plantilla **2D (Built-In Render Pipeline)** → versión **2022.3 LTS** → nombre `Kael_Rebelion_Mecanica`.
2. **Unir el proyecto con el repo (GitHub Desktop):** *File → Add local repository* y elegir la carpeta del proyecto. Si pide "create a repository", hay que crearlo y luego publicarlo en `RafaelS05/Kael_Rebelion_Mecanica`. Otra opción es clonar el repo vacío y crear el proyecto de Unity dentro de esa carpeta.
3. **Copiar los archivos:** el contenido de este paquete va en la **raíz** del proyecto: la carpeta `Assets` se combina con la que ya existe y el `.gitignore` queda junto a `Assets`, `Packages` y `ProjectSettings`.
4. **Construir:** en Unity, esperar a que compile y abrir el menú superior **Kael → Construir proyecto (Avance 2)**.
5. **Probar:** abrir `Assets/Scenes/MenuPrincipal` y presionar **Play**.
6. **Subir:** en GitHub Desktop hacer commit de `Assets`, `Packages`, `ProjectSettings` y `.gitignore`. La carpeta `Library` **no** se sube; el `.gitignore` ya la excluye.

## Qué incluye

| Requisito del Avance 2 | Dónde está |
|---|---|
| Movimiento, salto y disparo de Kael | `Scripts/Player/PlayerController.cs`, `PlayerShooter.cs` |
| Colisiones con enemigos y entorno | `Scripts/Level/DamageOnContact.cs`, `KillZone.cs`, Tilemaps con CompositeCollider2D |
| Interacción con el escenario | Plataformas de un solo sentido (PlatformEffector2D), espinas, fosos, puerta del jefe |
| Sistema de vida y puntaje | `Scripts/Core/Health.cs`, `ScoreManager.cs`, HUD |
| Enemigos | Chatarra (patrulla), Voltio (embiste), Electabot (dron que dispara hacia abajo) |
| Jefe | Oxibot Gigante: patrón de golpe al suelo, embestida y aturdido. Suelta el arma mejorada y abre la puerta |
| Menú principal (Jugar / Controles / Salir) | Escena `MenuPrincipal` |
| Pausa (Esc o P) | Continuar / Reiniciar / Menú |
| Pantallas de Gane y Pérdida con reinicio y menú | Escenas `Victoria` y `Derrota` |
| Carpetas organizadas | Scenes, Scripts, Prefabs, Sprites, Tiles, Audio, Animations, Levels… |

**Core loop:** se gana al llegar a la bandera después de vencer a Oxibot (la puerta se abre). Se pierde cuando la vida llega a 0 o al caer a un foso. Las dos pantallas permiten reiniciar o volver al menú.

## Controles
- **Moverse:** A/D o flechas
- **Saltar:** Espacio, W o ↑ (mantener presionado para saltar más alto)
- **Disparar:** J, K, Ctrl izquierdo o clic
- **Pausa:** Esc o P

## Cómo modificar cosas

- **Nivel:** se edita `Assets/Levels/Nivel1.txt` y se vuelve a ejecutar *Kael → Construir proyecto*.
  `#` suelo · `=` plataforma · `^` espinas · `P` Kael · `C` Chatarra · `V` Voltio · `E` Electabot · `B` Oxibot · `H` vida · `D` puerta del jefe · `G` meta
  Otra opción es pintar directamente en la escena con la Tile Palette: los tiles están en `Assets/Tiles`.
- **Arte final:** se reemplazan los PNG de `Assets/Sprites/...` por otros con el **mismo nombre** (pixel art a 16 px por unidad). El constructor no sobreescribe PNG que ya existan.
- **Animaciones:** si Kael tiene un `Animator` con los parámetros `Speed` (float), `Grounded` (bool) y `VelY` (float), `PlayerController` los actualiza automáticamente.
- **Audio (CC0):** se arrastran los clips al objeto `AudioManager` de cada escena (música, salto, disparo, golpe, etc.). Si falta un clip, simplemente no suena.
- **Balance:** velocidades, salto y vida se ajustan en el Inspector de cada prefab (`Assets/Prefabs`).

⚠️ Volver a ejecutar *Construir proyecto* **regenera las 4 escenas y los prefabs**. Si hicieron cambios a mano en las escenas, se pierden. Una vez que el equipo empiece a editar las escenas directamente, conviene dejar de usar el constructor.
