# Rayman Control Panel
<p align="center">
    <img src="img/main_artwork.png" width="400">
</p>

Rayman Control Panel is an open source community project created by [RayCarrot](https://github.com/RayCarrot). It acts as a launcher for your Rayman games, and also aims to unify existing game patches and fixes, as well as allowing extended configurations. It also provides a modding environment for modifying game files and creating and downloading mods. This program does not come with any games and requires the user to have them installed.

Check out the [wiki](https://github.com/RayCarrot/RayCarrot.RCP.Metro/wiki) for documentation on the project and its features.

Note: This repository is only for the WPF version (4.0.0 and above). The WinForms version (1.0.0 - 3.2.2) repository has since been made private as it's no longer being maintained and is heavily outdated.

> [!TIP]
> Download the latest release [here](https://github.com/RayCarrot/RayCarrot.RCP.Metro/releases/latest).

# Features
![Rayman Control Panel](img/example_games.png)

Main features:
- Launcher for Rayman games
- Extended game configuration support
- Setup game actions, showing recommended steps for setting up the games, as well as potential issues
- Mod loader with GameBanana integration
- Game tools, such as allowing per-level soundtrack in Rayman 1 and restoring prototype features in Rayman Raving Rabbids
- Save data viewing and editing, along with backup/restore options
- Disc installers to install select games from discs
- Options to modify game files, such as editing the UBIArt .ipk or CPA .cnt archives

## Mod Loader
![Mods](img/example_modloader_r2.png)

The mod loader allows you to create and install mods which modify the game in different ways. These can be file replacements, delta patches or game-specific changes. Mods uploaded to [GameBanana](https://gamebanana.com/) can be downloaded directly through the app.

![Mods](img/example_modloader_download_rl.png)

For more information about creating and using mods, see the [documentation](https://github.com/RayCarrot/RayCarrot.RCP.Metro/wiki/Mod-Loader).

## Archive Explorer
![Archive Explorer](img/example_archive_explorer.png)

The Archive Explorer is a tool within the Rayman Control Panel which allows supported game archive files to be viewed and modified. This is mainly used to replace textures in games, but can also be used for other file types such as sounds and more.

Supported archive file types:
- Rayman 1 `.dat` files
- CPA `.cnt` files
- UBIArt `.ipk` files

## Configuration
![Game Config](img/example_config_r1.png)

Supported games have a configuration page where its settings can be changed. This usually allows for more options than the native configuration tools each game has, such as being able to enable controller support, run in windowed mode or change the language.

## Tools
![Mods](img/example_prototype_restoration.png)

Different game-specific tools are available, such as the Prototype Restoration mod for Rayman Raving Rabbids and the Per-Level Soundtrack mod for Rayman 1.

![Mods](img/example_runtime_modifications.png)

Select games also support runtime modifications, which allows certain data in the game to be modified as its running, such as the number of lives or which level you're currently in. This can sometimes also be used to toggle unused features.

## Progression
![Progression](img/example_progression.png)

Detailed game save progression can be viewed, along with options to edit the data as serialized JSON and create/restore backups. 

# Linux
Currently the app can only run natively on Windows due it using WPF which is not cross-platform. Long-term I would like to migrate the app over to Avalonia UI to allow for true cross-platform, but it would require a lot of work. If anyone would like to help then please let me know! You can also up-vote the request for a native Linux version [here](https://github.com/RayCarrot/RaymanControlPanel/issues/44).

In the meantime the best way to get it to work on Linux is through Wine or Proton. In order to get the mod loader to work you might also need to grant write permission to the games folders by right-click on it, going to the Properties, then to the Permissions and then set the groups to "Can View & Modify Content". There might still be other issues which are being looked into [here](https://github.com/RayCarrot/RaymanControlPanel/issues/70).

# License

[MIT License (MIT)](./LICENSE)
