# TP05_C1_Vinaraz_RunWitchRun

**Run Witch Run** es un juego 2D de correr sin fin, parecido al dinosaurio de Google Chrome. Una bruja corre sin parar por un bosque oscuro, salta obstáculos y junta gemas.

- Motor: Unity 6000.3.21f1
- Autor: Teo Vinaraz

## Cómo abrirlo y jugar

1. En Unity Hub: **Add project from disk** y elegir esta carpeta.
2. Esperar a que Unity termine de cargar. La primera vez arma solo las escenas (aparece `[Run Witch Run] Proyecto armado` en la consola).
3. Abrir la escena `Assets/Scenes/MainMenu` y apretar **Play**.

Si algo se ve raro, en el menú de Unity: **Run Witch Run > Rebuild Project**.

## Controles

| Acción | Tecla |
|---|---|
| Saltar (mantener = salto más alto) | `Espacio` o click izquierdo |
| Pausa | `P`, `Esc` o el botón de pausa |
| Reintentar al perder | `R` o botón Retry |

La bruja no puede frenar: siempre corre. Lo único que hace el jugador es saltar.

## Qué pide el TP y dónde está

Un juego 2D funciona con sprites (imágenes planas), una cámara sin perspectiva y física 2D (`Rigidbody2D`, `Collider2D`). Este proyecto usa solo eso.

| Pauta del TP | Estado | Dónde está |
|---|---|---|
| Runner 2D estilo Dino | Hecho | Escena `Gameplay` |
| Repositorio y proyecto con nombre `TP05_C1_Apellido_Título` | Hecho | `TP05_C1_Vinaraz_RunWitchRun` |
| El jugador no puede detenerse | Hecho | `PlayerController.cs` |
| El personaje puede saltar | Hecho | `PlayerController.cs` |
| Obstáculos en posiciones aleatorias | Hecho | `EndlessSpawner.cs` |
| Sonido en al menos 3 interacciones | Hecho (8) | `AudioManager.cs` |
| Volumen desde Settings directo a un AudioMixer | Hecho | `SettingsMenu.cs` y `MainMixer.mixer` |
| ParticleSystem o TrailRenderer | Hecho (ParticleSystem) | Polvo, salto, aterrizaje, gemas y escoba |
| Datos iniciales del jugador en un ScriptableObject | Hecho | `Assets/Data/PlayerData/PlayerData.asset` |
| Power up o cambio sutil de jugabilidad | Hecho | Escoba Mágica (`PowerUp.cs`) |

### Detalle de lo que se pidió

- **Que no se detenga:** en realidad la bruja corre en el mismo lugar y lo que se mueve es el mundo hacia la izquierda. Así hacen la mayoría de los runners. Con el tiempo el mundo va más rápido.
- **Obstáculos aleatorios:** aparecen distintos obstáculos (como lápidas) a distancias al azar, pero siempre con espacio para poder saltarlos.
- **Sonidos:** música del menú, música del juego, saltar, aterrizar, agarrar gema, agarrar power up, perder y click de botones.
- **AudioMixer:** los sliders de Música y Efectos de Settings cambian directamente el volumen del mixer (`MusicVolume` y `SFXVolume`) y el valor queda guardado.
- **Partículas:** se eligió ParticleSystem. No se usó TrailRenderer.
- **ScriptableObject:** `PlayerData` guarda velocidad inicial, fuerza de salto, gravedad, duración de la escoba y otros valores. Se pueden cambiar desde el Inspector sin tocar código.
- **Power up:** la Escoba Mágica sube la velocidad x1.5 durante unos segundos.

## Extras que no pedía el TP

- Menú principal, menú de pausa, Settings y Credits.
- Puntaje con récord guardado y pantalla de Game Over.
- Gema Lunar que suma +10 puntos.
- Dificultad que sube con el tiempo.

## Qué NO tiene

- Más de un nivel o escenario: hay un solo bosque.
- Soporte para celular o joystick (solo teclado y mouse).
- Uso de TrailRenderer (la consigna decía una cosa o la otra).

## Carpetas principales

```
Assets/
  Art/        imágenes de la bruja, obstáculos y fondo
  Audio/      música, sonidos y el AudioMixer
  Data/       el ScriptableObject PlayerData
  Prefabs/    objetos reutilizables (obstáculos, gemas, escoba)
  Scenes/     MainMenu, Gameplay (se crean al abrir el proyecto)
  Scripts/    todo el código
```

## Créditos

- Creado por Teo Vinaraz
- Música: ChatGPT / OpenAI
- Arte y recursos visuales: ChatGPT / OpenAI

Pixel Art generado específicamente para este proyecto.
