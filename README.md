Discord: smookyz2024

*** REQUIRED TO DOWNLOAD .NET Runtime 8.0 for it to work *** 

Download .NET runtime 8.0 -> https://dotnet.microsoft.com/en-us/download/dotnet/8.0

*** You may need to "Unblock" the files. Right click on each extracted file. Click properties. At the bottom, there might be an "Unblock" checkbox. Unblock it and hit apply. Do this for all files. ***



This autopotion solution aims to even the playing field for all WoE players allowing even the 300+ ping players to pot similar or EVEN FASTER than 50 ping players when the current hp drops.
The bottleneck is the default item usage delay of 100ms. This means there is no reason to purchase other autopotion programs as they will simply not provide any meaningful gain when your ping is 40-50 or greater. 

I put all the code into the program.cs file to make it easier for newer or beginner programmers to ctrl+f and edit what they need to add.

Explanation -> https://www.youtube.com/watch?v=P06RzfgapOg

SmookyzAP in action -> https://www.youtube.com/watch?v=ecfqVSuzZu8
350 ping guild dominating -> https://www.youtube.com/watch?v=ful58PGnanU

---

# SmookyzAP – Ragnarok Autopotion & Macro Program

A C# tool for automating potions, buffs, skill spam, and macros for Ragnarok Online WoE. 

---

## Features

- **Auto-potion:** Automatically uses HP and SP potions based on memory status.
- **Auto-buff:** Automatically casts configured buffs (Speed, Aspd, Gloria, etc.).
- **Skill Spammer:** Spams configured skills with or without mouse clicks.
- **Macro Switching:** Switches equipment/skills for SG Gypsy class.
- **Chain Macro:** Runs a sequence of keys with customizable timing.
- **Configurable Hotkeys & Delays**
- **Console Status Display:** Real-time feedback (pause, high ping mode, etc.).

---

## Installation

1. **Requirements:**  
   - Windows OS  
   - .NET 8.0+ SDK or runtime  
2. **Build:**  
   - Download or clone this repo:  
     ```sh
     git clone https://github.com/SmookyzX/SmookyzAP.git
     ```
   - Open in Visual Studio and build, or use CLI:
     ```sh
     dotnet build
     ```
---

## Configuration

On first run, a `config.ini` file will be created. Edit this file to set hotkeys, delays, and features.

---

## Usage

- Run `SmookyzAP.exe` as Admin.
- The console will display status and wait for the Ragnarok window.
- Toggle pause with the configured key (`END` by default).
- Toggle High Ping Mode with the configured key (`HOME` by default).
- Edit `config.ini` for advanced setup.

---

## Hotkeys & Controls

- **Pause/Resume:** `END` (default) – toggles autopot features on/off.
- **High Ping Mode:** `HOME` (default) – switches to constant potting mode for 40-50+ ping players
- **Skill Spam:** Hold configured skill key(s).
- **Chain Macro:** Hold the configured macro trigger key to run the sequence.
- **Macro Switcher:** For SG Gypsy, hold the whip key to trigger PDFM and Combat Knife sequence.

---

## Credits

Created by [SmookyzX](https://github.com/SmookyzX)
