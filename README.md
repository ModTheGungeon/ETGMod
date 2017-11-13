ETGMod
===

A modding platform for [Enter the Gungeon](http://enterthegungeon.com) by [Dodgeroll Studios](http://dodgeroll.com), published by [Devolver Digital](http://devolverdigital.com/).

### For end users

#### What does this add to the game?

By itself, nothing except a title screen. This is a mod loader - intended to be a base for mods to run, not a mod by itself.

#### How does this work?

If you're interested in the actual details, you can check the "For developers" section below. To simplify it, you run the [installer](https://github.com/ModTheGungeon/ETGMod.Installer), which modifies the game to insert all of this code into it. Then, when you start the game, ETGMod is started "inside" of it, with the ability to access anything from the game. It'll then start loading mods and doing all of the things it needs to do.

#### Who's working on this?

You can check the members of the Mod the Gungeon GitHub organization [here](https://github.com/orgs/ModTheGungeon/people). Note that, while there are 5 people there, 2 have officially quit the project while others remain mostly dormant. In other words, at this moment I'm pretty much the only person working on ETGMod.

#### Does this work for the latest update? Is this different from the version in the installer?

Currently, the master branch (the branch you're seeing above) of this repository houses a version of ETGMod dubbed "ETGMod Reloaded". ETGMod Reloaded is a mod rewritten completely from scratch, with almost no code taken from the original ETGMod. The version that's currently available in the installer is an outdated version of the old ETGMod that was updated only to run on the latest Gungeon update (but it doesn't even have its new items as `give` command IDs).

#### What are the differences between the "installer ETGMod" and ETGMod Reloaded?

ETGMod Reloaded has moved away from C# mods in favor of **Lua** mods. This, while slightly complicating the code, allows for a greater amount of control over mods by ETGMod and makes writing mods a lot easier. For example, I was able to implement the ability to reload mods without restarting the game, saving both the mod developers' and the end users' time. Mods are now also sandboxed, so that they can't just delete all of your files.

#### How do I install this (ETGMod Reloaded)?

Since it's a work in progress, there isn't even a beta release yet. If you think you'll be able to do it, you can try building this repository (use the `build.sh` script or the `build.bat` script **[NOTE: the build.bat script isn't finished yet]**; you'll need to have Visual Studio or Mono installed). Then you can select the resulting `ETGMOD.zip` in the Advanced tab in the installer, install, and have fun.


### For developers

#### Licensing
The `LICENSE_ETGMOD` file contains the licenses of the projects used by ETGMod. If an external project is used by ETGMod and its license is not in `LICENSE_ETGMOD`, then it is an oversight and I ask you to report it on the issue tracker.

#### Legality
Dodgeroll is fully aware of ETGMod's existence and goals. For the foreseeable future, there shouldn't be any worries about whether ETGMod would be forced to have its development stopped.

#### Underlying technology
* Enter the Gungeon is written in C# using the Unity Engine.
* ETGMod is written in C#.
* [MonoMod](https://github.com/0x0ade/MonoMod) - The ultimate CLR assembly patcher.
* [A fork of Eluant](https://github.com/modthegungeon/Eluant) - CLR/Lua bindings with a focus on stability, memory conservatism and with the best error reporting out of all other Lua bindings for the CLR.
* [YamlDotNet](https://github.com/aaubry/YamlDotNet) - The best YAML parser for the CLR.
* [SGUI](https://github.com/ModTheGungeon/SGUI) - Scriptable GUI for Unity mods.

#### What are mods written in?

Lua.

#### Tutorials

ETGMod Reloaded is WIP, so no tutorials yet. Sorry!

#### Resources

* [Official WIP website](https://modthegungeon.zatherz.eu/)
  (for the time being, hosted on my private web server)
* [**Very** WIP API reference](https://modthegungeon.zatherz.eu/gungeonapidocs/)
  (doesn't have ETGMod classes yet, the look and feel will change soon)
* [Discord channel](https://discord.gg/ngkYDes)
* [Comparison](https://zatherz.eu/csharp/eluant-nlua-comparison/) between the Lua bindings that ETGMod uses, upstream Eluant and NLua.
* [My barely-updated blog](https://zatherz.eu/blogs/) which contains some techniques and tools used by ETGMod
