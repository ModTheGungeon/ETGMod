# Enter the Gungeon Base API Mod pre-rewrite fork
# Compatible with Advanced Gungeons & Draguns update

### License: MIT

----

This repository houses the "Mod the Gungeon" (ETGMod) modding API for the game "Enter the Gungeon."  

The "base backend" will be installed before anything else in [the ETGMod.Installer](https://github.com/ModTheGungeon/ETGMod.Installer).

It is still in its early phases. Do not expect anything usable for end-users here for quite a while.

For compatibility reasons, it contains its own copy of Mono.Cecil (it's not a submodule).  

# Mod installation (tutorial by gOOLY)

1. Download the [latest mod build](https://github.com/katzsmile/ETGMod/releases/latest), and put it on your desktop
2. Download the [latest MtG installer build](https://github.com/ModTheGungeon/ETGMod.Installer/releases/latest) from the website
3. Open the ETGMod.Installer
4. Look on the Installer. Look towards the middle and you will see 2 Tabs, "Backends" and "Advanced"
5. Select the Advanced Tab
6. On the Advanced Tab you will need to scroll down until you see> "Offline mode-only use APIs here". Check that box 
7. Now, if you remember that ETGMOD.zip file you were supposed to download earlier, you will need to for the last step. Scroll back up on the Advanced Tab, and drag-and-drop that .zip file until it indicates that there is a file there (Mine says C:\Users\Admin\Desktop\ETGMOD.zip , but yours may say something else) It is important to note that you should NOT UNZIP ETGMOD.zip, just put it in there exactly the way you found it
8. Select "Step 3: Install ETGMod" on the installer
9. Run Enter the Gungeon and see if it worked. 

# Before contributing

Make sure to read the [Code Style Guide](STYLE.md) and the [Contributing](CONTRIBUTING.md) documents.

# Helpful resources

* [Example API mod ("backend")](https://github.com/ModTheGungeon/ETGMod.ExampleAPI)

# Frequently Asked Questions (FAQ)

### How do I make mods?

ETGMod is work in progress software. We do not yet offer a stable API.

### Is there a Mac version?

Worry not, ETGMod is written in the same language as Gungeon and uses the same runtime as Gungeon, Mono.  
It already runs on Windows, Linux and Mac.

### Is there a Linux version?

Read the answer to the question above.

### Console modding?

PS4 modding will never happen. The platform differs a lot from PC and we've never worked with programming for consoles.

### Are the devs okay with this?

The developers of the game actually helped us with some code for ETGMod.  
They often hang out in the Gungeon Discord and are really great people.

### Does this work on pirated copies of the game?

We do not support pirated copies of the game. You're on your own.  
We suggest supporting the awesome devs! Things would probably look a lot different if they weren't as supportive as they are.

### This is too complicated for me. How do I install a mod?

ETGMod is work in progress software. We're working on simple tutorials.  
We'll make sure to have them before we release a stable version.
