# megabonk-together

<p align="center">
  <img src="images/togetherButton.png" alt="logo" />
</p>

<!-- Shield -->

[![Contributors][contributors-shield]][contributors-url]
[![Download][download-shield]][download-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]

# About The Mod

Megabonk together is a WIP mod that bring netplay to megabonk.

This mod feature:

- Peer to peer online up to 6 players (Relay transport for ipv6 addresses)
- Mostly (not all) in game mechanics synchronized
  - Weapons (also their updgrade)
  - Projectiles (not perfect for all)
  - Monsters (follow, target, attacks)
  - Chest
  - Shrines
  - Portal
  - Challenges
  - and probably other i forgot
- Death cam
- Custom balance to match the number of players (Probably unbalanced right now but it can be tuned later)
- Auto-update
- Probably won't work with other non cosmetic mods

More info at [Notable Network Changes](./NETPLAY_CHANGES.md)

# Install

This mod has only been developed and tested on windows, probably won't work on other platform (Is the game can even be run on Linux/Mac even ?)

Also this mod was developed for the 1.0.49 version, meaning it will probably break when an official major update drop .

> [!NOTE]  
> Before starting, i suggest to make a backup of your current save file just in case.
> The game save is somewhere at {user}/AppData/LocalLow/Ved/Megabonk/Saves/CloudDir/{some steam id guess}
> You can also just copy all at {user}/AppData/LocalLow/Ved/Megabonk if not sure
> Make sure to copy your save somewhere in case you need to restore it back for some reason

This mod run with [BepInEX 6 Bleeding Edge Build](https://builds.bepinex.dev/projects/bepinex_be) (tested on the current latest #752):

Start by downloading the BepInex loader [BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.752](https://builds.bepinex.dev/projects/bepinex_be/752/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.752%2Bdd0655f.zip) in your game directory (Where your Megabonk.exe is). This mean you want to extract BepInEx at something like `{your own path}\steamapps\common\Megabonk`

> [!NOTE]  
> We specifically target the IL2CPP version (.IL2CPP) in the BepInEx build page and also the windows (-win) and the 64 bits (-x64) version of BepInex.
> Not sure if i will upgrade this page every new BepInEx update this how you know what version you should get
> Also i dont know if 64 bits is tied with how the game was built or your system , if the 64 bits version is not working, i guess you should try the 32 bits (x86) version (something like `BepInEx-Unity.IL2CPP-win-x86-6.0.0-be`)

- Launch the game first at least one time to confirm BepInEx is working (Wait for the main menu as BepInEx need to make some dummy dll). Yous should see a terminal opened along side with the game with some logs in it

- After exiting the game,grab and download the [latest release](https://github.com/Fcornaire/megabonk-together/releases/latest)

- Extract the zip and copy the folder `Megabonk-Together` to the plugins path of BepInEx , something like `../steamapps/common/Megabonk/BepInEx/plugins`

- Launch the game and confirm a new weird `Togther!` button is at the bottom of the main screen

> [!NOTE]  
> The button is not navigable right now, you won't be able to select it with a controller , use your mouse to interact with it (You can pick your controller later)
> Will try to fix later

- Enjoy ! Do not hesitate to open an [issue](https://github.com/Fcornaire/megabonk-together/issues) if you encounter a bug or something isn't working

# Known issue

- For some obscure reason, The game crash when loading the map. this is mostly rare and you can just close and restart the game if it ever happen. Dunno why it sometimes crash here ¯\_(ツ)\_/¯
- Not all the stuff happening in the game are perfectly synchronized, like getting money when you shouldn't or ghost item not spawning or whatever. I will mostly be looking for game breaking bug before looking at those

# Building (Developer)

- Clone this repository
- Set the environment variable **MegabonkPath** pointing to your own game install (something like _../steamapps/common/Megabonk_) for your IDE to know where to get the required DLLs to load for building the mod
- Build the solution

You should now have the macthmaking server built and also the mod file

> [!IMPORTANT]  
> The IDE will copy the result mod file in your game directory. This is super practical when developing locally but remember to delete it after.

To target a local server, modify the file `{your game path}/BepInEx/config/MegabonkTogether.cfg` and update [Network].ServerUrl to `ws://127.0.0.1:5000`

# Social

Bsky : [Dshad66](https://bsky.app/profile/dshad66.bsky.social)

Twitter : DShad - [@DShad66](https://twitter.com/DShad66)

Discord : dshad (was DShad#4670)

<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->

[contributors-shield]: https://img.shields.io/github/contributors/Fcornaire/megabonk-together.svg?style=for-the-badge
[contributors-url]: https://github.com/Fcornaire/megabonk-together/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/Fcornaire/megabonk-together.svg?style=for-the-badge
[forks-url]: https://github.com/Fcornaire/megabonk-together/network/members
[stars-shield]: https://img.shields.io/github/stars/Fcornaire/megabonk-together.svg?style=for-the-badge
[stars-url]: https://github.com/Fcornaire/megabonk-together/stargazers
[issues-shield]: https://img.shields.io/github/issues/Fcornaire/megabonk-togethersvg?style=for-the-badge
[issues-url]: https://github.com/Fcornaire/megabonk-together/issues
[license-shield]: https://img.shields.io/github/license/Fcornaire/megabonk-together.svg?style=for-the-badge
[download-shield]: https://img.shields.io/github/downloads/Fcornaire/megabonk-together/total?style=for-the-badge
[download-url]: https://github.com/Fcornaire/megabonk-together/releases
[license-url]: https://github.com/Fcornaire/megabonk-together/blob/master/LICENSE.txt
