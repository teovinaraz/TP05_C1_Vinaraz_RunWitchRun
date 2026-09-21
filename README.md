# Run Witch Run

A 2D endless runner where a witch runs through a dark forest at night. Jump over the obstacles, grab moon gems and try to last as long as you can. Made in Unity 6000.3.21f1 for TP05.

**Play it here:** https://teovinaraz.itch.io/run-witch-run

## Controls

- Jump: `Space` or left click (hold to jump higher)
- Pause: `P` or `Esc`
- Retry after losing: `R`

The witch never stops running, so all you can do is jump.

## How it works

The witch actually runs in place and the world scrolls past her, which is how most runners do it. Obstacles spawn at random distances, but there is always enough room to jump them. The game gets faster the longer you survive.

- **Jump:** `PlayerController.cs`
- **Random obstacles:** `EndlessSpawner.cs`
- **Sound:** menu and gameplay music, jump, land, gems, power-up, game over and buttons (`AudioManager.cs`)
- **Volume:** the sliders in Settings change the AudioMixer directly (`SettingsMenu.cs`)
- **Particles:** ParticleSystem for dust, jumps, gems and the broom trail
- **Player data:** starting stats live in a ScriptableObject (`Assets/Data/PlayerData`)
- **Power-up:** the Magic Broom makes you 1.5x faster for a few seconds

## Running the project

Open the folder from Unity Hub (6000.3.21f1), open `Assets/Scenes/MainMenu` and press Play. The first time, the scenes are built automatically. If something looks off, use `Run Witch Run > Rebuild Project` in the Unity menu.

## Credits

Made by Teo Vinaraz. Music and pixel art generated with ChatGPT / OpenAI, specifically for this project.
