using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Smookyz
{
    class Program
    {
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        const uint WM_KEYDOWN = 0x0100;
        const uint WM_KEYUP = 0x0101;

        static bool spammerRunning = false;

        struct PlayerStatus
        {
            public int hpValue, hpMax, spValue, spMax;
        }
        public struct SpamKey
        {
            public int KeyCode;
            public bool UseHold;

            public SpamKey(int keyCode, bool useHold)
            {
                KeyCode = keyCode;
                UseHold = useHold;
            }
        }
        class Buffs
        {
            public bool aspd, gloom, quag, sun, fire, water, wind,
                        str, dex, agi, vit, luk, intel, drowsiness, resentment, speed,
                        negativeStatus,
                        gloria, truesight, abrasive, autoguard, reflectshield, defender;
        }
        class Config
        {
            public int aspdKey = -1, gloomKey = -1, sunKey = -1, spKey = -1, hpKey = -1,
                       fireKey = -1, waterKey = -1, windKey = -1,
                       dexKey = -1, agiKey = -1, vitKey = -1, lukKey = -1, intelKey = -1,
                       resentmentKey = -1, drowsinessKey = -1, speedKey = -1,
                       strKey = -1, statusRecoveryKey = -1, gloriaKey = -1,
                       truesightKey = -1, abrasiveKey = -1, autoguardKey = -1,
                       reflectshieldKey = -1, defenderKey = -1;

            // Adjustable SP threshold (percentage)
            public double spThreshold = -1;

            public int HighPingModeToggle = 0x24;
            public string windowTitle = "";
            public int baseAddress = 0x010DCE10;
            public int autoBuffDelay = 50;

            public List<SpamKey> skillSpamClickKeys = new();
            public List<SpamKey> skillSpamNoClickKeys = new();
            public int skillSpamDelay = 1;
            public int mouseBoostAddress = -1;
            public int holdKey = -1;
            public int holdKeyDelay = 29;

            public int whipKey = -1;
            public int pdfmKey = -1;
            public int combatKnifeKey = -1;
            public int pdfmDelay = 100;
            public int combatKnifeDelay = 100;

            public int periodicKey = -1;
            public int periodicDelay = -1; 

            public int fullPauseKey = 0x23;

            public bool chainMacroEnabled = false;
            public int chainMacroKey = -1;
            public List<(int key, int delay)> chainMacroSequence = new();
        }
        static readonly Dictionary<string, int> virtualKeyMap = new()
        {
            { "F1", 0x70 }, { "F2", 0x71 }, { "F3", 0x72 }, { "F4", 0x73 },
            { "F5", 0x74 }, { "F6", 0x75 }, { "F7", 0x76 }, { "F8", 0x77 },
            { "F9", 0x78 }, { "F10", 0x79 }, { "F11", 0x7A }, { "F12", 0x7B },

            { "0", 0x30 }, { "1", 0x31 }, { "2", 0x32 }, { "3", 0x33 }, { "4", 0x34 },
            { "5", 0x35 }, { "6", 0x36 }, { "7", 0x37 }, { "8", 0x38 }, { "9", 0x39 },

            { "A", 0x41 }, { "B", 0x42 }, { "C", 0x43 }, { "D", 0x44 }, { "E", 0x45 },
            { "F", 0x46 }, { "G", 0x47 }, { "H", 0x48 }, { "I", 0x49 }, { "J", 0x4A },
            { "K", 0x4B }, { "L", 0x4C }, { "M", 0x4D }, { "N", 0x4E }, { "O", 0x4F },
            { "P", 0x50 }, { "Q", 0x51 }, { "R", 0x52 }, { "S", 0x53 }, { "T", 0x54 },
            { "U", 0x55 }, { "V", 0x56 }, { "W", 0x57 }, { "X", 0x58 }, { "Y", 0x59 }, { "Z", 0x5A },

            // Lowercase letters
            { "a", 0x41 }, { "b", 0x42 }, { "c", 0x43 }, { "d", 0x44 }, { "e", 0x45 },
            { "f", 0x46 }, { "g", 0x47 }, { "h", 0x48 }, { "i", 0x49 }, { "j", 0x4A },
            { "k", 0x4B }, { "l", 0x4C }, { "m", 0x4D }, { "n", 0x4E }, { "o", 0x4F },
            { "p", 0x50 }, { "q", 0x51 }, { "r", 0x52 }, { "s", 0x53 }, { "t", 0x54 },
            { "u", 0x55 }, { "v", 0x56 }, { "w", 0x57 }, { "x", 0x58 }, { "y", 0x59 }, { "z", 0x5A },

            // Control keys
            { "BACKSPACE", 0x08 },
            { "TAB", 0x09 },
            { "ENTER", 0x0D },
            { "SHIFT", 0x10 },
            { "CTRL", 0x11 },
            { "ALT", 0x12 },
            { "CAPSLOCK", 0x14 },
            { "ESCAPE", 0x1B },

            // Space and punctuation
            { "SPACE", 0x20 },
            { "PAGEUP", 0x21 },
            { "PAGEDOWN", 0x22 },
            { "END", 0x23 },
            { "HOME", 0x24 },
            { "LEFT", 0x25 },
            { "UP", 0x26 },
            { "RIGHT", 0x27 },
            { "DOWN", 0x28 },
            { "INSERT", 0x2D },
            { "DELETE", 0x2E },

            // Number pad keys
            { "NUM0", 0x60 }, { "NUM1", 0x61 }, { "NUM2", 0x62 }, { "NUM3", 0x63 }, { "NUM4", 0x64 },
            { "NUM5", 0x65 }, { "NUM6", 0x66 }, { "NUM7", 0x67 }, { "NUM8", 0x68 }, { "NUM9", 0x69 },
        };

        static double Percent(double val1, double val2) => (val2 == 0) ? 0 : (val1 / val2) * 100.0;

        static void PressKey(IntPtr hWnd, int key, int delay)
        {
            PostMessage(hWnd, WM_KEYDOWN, key, 0);
            PostMessage(hWnd, WM_KEYUP, key, 0);
            Thread.Sleep(delay);
        }
        static void PressKeyNoDl(IntPtr hWnd, int key)
        {
            PostMessage(hWnd, WM_KEYDOWN, key, 0);
            PostMessage(hWnd, WM_KEYUP, key, 0);
        }
        static void PressHPKey(IntPtr hWnd, int key)
        {
            PostMessage(hWnd, WM_KEYDOWN, key, 0);
            PostMessage(hWnd, WM_KEYUP, key, 0);
        }
        static void PressSPKey(IntPtr hWnd, int key)
        {
            PostMessage(hWnd, WM_KEYDOWN, key, 0);
            PostMessage(hWnd, WM_KEYUP, key, 0);
        }
        static void LoadOrCreateConfig(Config config)
        {
            const string file = "config.ini";
            if (!File.Exists(file))
            {
                File.WriteAllText(file, """
            [Hotkeys]
                hpKey = 
                spKey = 
                statusRecoveryKey = 

            ; High Ping Mode which toggles the constant potting is now DEFAULT OFF
                HighPingModeToggle = HOME

            ; The fullpausekey only pauses the potter/autobuff. It does not pause the clicker. Just to save some woe mats
                fullPauseKey = END

            ; AutoBuffs
                aspdKey = 
                gloomKey = 
                sunKey = 
                fireKey = 
                waterKey = 
                windKey = 
                strKey = 
                dexKey = 
                agiKey = 
                vitKey = 
                lukKey = 
                intelKey = 
                resentmentKey = 
                drowsinessKey = 
                speedKey = 
                gloriaKey = 
                truesightKey = 
                abrasiveKey = 
                autoguardKey = 
                reflectshieldKey = 
                defenderKey = 

            ; *** Don't forget to set your SP Treshold below ***
            [Settings]
                spThreshold = 40
                windowTitle = Ragnarok Online
                baseAddress = 010DCE10
                autoBuffDelay = 50

            [Skill Spammer]
                mouseBoostAddress = 
                skillSpamClickKeys = F1,F2:false
                skillSpamNoClickKeys = 
                skillSpamDelay = 1
                holdKey = 
                holdKeyDelay = 29

            [SG Gypsy]
                whipKey = 
                pdfmKey = 
                combatKnifeKey = 
                pdfmDelay = 100
                combatKnifeDelay = 100

            ; Periodic Key = Send x key every x milliseconds. Used for autogloria etc... DOES NOT WORK PROPERLY IF HIGH PING MODE IS ON
            [Periodic Key]
                periodicKey = 
                periodicDelay = 

            [Chain Macro]
                chainMacroEnabled = false
                chainMacroKey = 
                chainMacroSequence = F1:1000,F2:500,F3:1500
            """);
                return;
            }

            var lines = File.ReadAllLines(file);
            string section = "";
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.EndsWith("]")) { section = line[1..^1]; continue; }

                var parts = line.Split('=');
                if (parts.Length != 2) continue;
                string key = parts[0].Trim(), val = parts[1].Trim();
                if (section == "Hotkeys")
                {
                    if (string.IsNullOrWhiteSpace(val))
                    {
                        typeof(Config).GetField(key)?.SetValue(config, -1); // disabled
                    }
                    else if (virtualKeyMap.TryGetValue(val, out int code))
                    {
                        typeof(Config).GetField(key)?.SetValue(config, code);
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Unknown key '{val}' for '{key}'");
                        typeof(Config).GetField(key)?.SetValue(config, -1);
                    }
                }
                else if (section == "Settings")
                {
                    if (key == "spThreshold" && double.TryParse(val, out double spVal))
                    {
                        config.spThreshold = spVal;
                    }
                    else if (key == "windowTitle")
                    {
                        config.windowTitle = val;
                    }
                    else if (key == "baseAddress" && int.TryParse(val, System.Globalization.NumberStyles.HexNumber, null, out int addr))
                    {
                        config.baseAddress = addr;
                    }
                    else if (key == "autoBuffDelay" && int.TryParse(val, out int delayVal))
                    {
                        config.autoBuffDelay = delayVal;
                    }
                }
                else if (section == "Skill Spammer")
                {
                    if (key == "mouseBoostAddress")
                    {
                        string cleanedVal = val.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? val.Substring(2) : val;

                        if (int.TryParse(cleanedVal, System.Globalization.NumberStyles.HexNumber, null, out int hexAddr))
                        {
                            config.mouseBoostAddress = hexAddr;
                        }
                        else if (int.TryParse(val, out int decAddr)) // fallback in case it's pure decimal
                        {
                            config.mouseBoostAddress = decAddr;
                        }
                    }
                    else if (key == "skillSpamClickKeys")
                    {
                        var keys = val.Split(',');
                        foreach (var k in keys)
                        {
                            var keyParts = k.Split(':');  
                            var keyName = keyParts[0].Trim();
                            bool useHold = keyParts.Length > 1 ? bool.TryParse(keyParts[1], out bool b) && b : true;

                            if (virtualKeyMap.TryGetValue(keyName, out int keyCode))
                                config.skillSpamClickKeys.Add(new SpamKey(keyCode, useHold));
                        }
                    }
                    else if (key == "skillSpamNoClickKeys")
                    {
                        var keys = val.Split(',');
                        foreach (var k in keys)
                        {
                            var keyParts = k.Split(':');  
                            var keyName = keyParts[0].Trim();
                            bool useHold = keyParts.Length > 1 ? bool.TryParse(keyParts[1], out bool b) && b : true;

                            if (virtualKeyMap.TryGetValue(keyName, out int keyCode))
                                config.skillSpamNoClickKeys.Add(new SpamKey(keyCode, useHold));
                        }
                    }
                    else if (key == "skillSpamDelay" && int.TryParse(val, out int spamDelay))
                    {
                        config.skillSpamDelay = spamDelay;
                    }
                    else if (key == "holdKey")
                    {
                        if (string.IsNullOrWhiteSpace(val))
                        {
                            typeof(Config).GetField(key)?.SetValue(config, -1); // disabled
                        }
                        else if (virtualKeyMap.TryGetValue(val, out int code))
                        {
                            typeof(Config).GetField(key)?.SetValue(config, code);
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Unknown key '{val}' for '{key}'");
                            typeof(Config).GetField(key)?.SetValue(config, -1);
                        }
                    }
                    else if (key == "holdKeyDelay" && int.TryParse(val, out int holdDelay))
                    {
                        config.holdKeyDelay = holdDelay;
                    }
                }
                else if (section == "SG Gypsy")
                {
                    if (key is "whipKey" or "pdfmKey" or "combatKnifeKey")
                    {
                        if (string.IsNullOrWhiteSpace(val))
                            typeof(Config).GetField(key)?.SetValue(config, -1);
                        else if (virtualKeyMap.TryGetValue(val, out int code))
                            typeof(Config).GetField(key)?.SetValue(config, code);
                        else
                            Console.WriteLine($"Unknown macro key '{val}' for '{key}'");
                    }
                    else if ((key == "pdfmDelay" || key == "combatKnifeDelay") && int.TryParse(val, out int delay))
                    {
                        typeof(Config).GetField(key)?.SetValue(config, delay);
                    }
                }
                else if (section == "Periodic Key")
                {
                    if (key == "periodicKey")
                    {
                        if (string.IsNullOrWhiteSpace(val))
                            config.periodicKey = -1;
                        else if (virtualKeyMap.TryGetValue(val, out int code))
                            config.periodicKey = code;
                        else
                            Console.WriteLine($"Unknown periodic key '{val}'");
                    }
                    else if (key == "periodicDelay" && int.TryParse(val, out int pd))
                    {
                        config.periodicDelay = pd;
                    }
                }
                else if (section == "Chain Macro")
                {
                    if (key == "chainMacroEnabled" && bool.TryParse(val, out bool enabled))
                        config.chainMacroEnabled = enabled;
                    else if (key == "chainMacroKey" && virtualKeyMap.TryGetValue(val, out int triggerKey))
                        config.chainMacroKey = triggerKey;
                    else if (key == "chainMacroSequence")
                    {
                        var entries = val.Split(',');
                        foreach (var entry in entries)
                        {
                            var entryParts = entry.Split(':');
                            if (entryParts.Length != 2) continue;

                            if (virtualKeyMap.TryGetValue(entryParts[0].Trim(), out int k) &&
                                int.TryParse(entryParts[1].Trim(), out int d))
                            {
                                config.chainMacroSequence.Add((k, d));
                            }
                        }
                    }
                }
            }
        }
        static void ReadHpOnly(IntPtr hProcess, int addr, ref PlayerStatus status)
        {
            byte[] buffer = new byte[8];
            ReadProcessMemory(hProcess, (IntPtr)addr, buffer, buffer.Length, out _);
            status.hpValue = BitConverter.ToInt32(buffer, 0);
            status.hpMax = BitConverter.ToInt32(buffer, 4);
        }
        static void ReadSp(IntPtr hProcess, int spAddr, ref PlayerStatus status)
        {
            byte[] buffer = new byte[8];
            ReadProcessMemory(hProcess, (IntPtr)spAddr, buffer, 8, out _);
            status.spValue = BitConverter.ToInt32(buffer, 0);
            status.spMax = BitConverter.ToInt32(buffer, 4);
        }
        static void CheckBuffs(IntPtr hProcess, int addr, Buffs buffs)
        {
            int bufferSize = 40;
            byte[] buffer = new byte[4 * bufferSize];
            if (!ReadProcessMemory(hProcess, (IntPtr)addr, buffer, buffer.Length, out _)) return;

            for (int i = 0; i < bufferSize; i++)
            {
                int buffId = BitConverter.ToInt32(buffer, i * 4);
                if (buffId == -1) break;
                switch (buffId)
                {
                    case 883:
                    case 884:
                    case 885:
                    case 886:
                    case 887:
                        buffs.negativeStatus = true;
                        break;
                    case 3: buffs.gloom = true; break;
                    case 8: buffs.quag = true; break;
                    case 21: buffs.gloria = true; break;        // GLORIA
                    case 37: case 38: case 39: buffs.aspd = true; break;
                    case 41: buffs.speed = true; break;
                    case 58: buffs.autoguard = true; break;     // AUTOGUARD
                    case 59: buffs.reflectshield = true; break; // REFLECTSHIELD
                    case 62: buffs.defender = true; break;      // DEFENDER
                    case 115: buffs.truesight = true; break;    // TRUESIGHT
                    case 184: buffs.sun = true; break;
                    case 295: buffs.abrasive = true; break;     // ABRASIVE
                    case 908: buffs.water = true; break;
                    case 910: buffs.fire = true; break;
                    case 911: buffs.wind = true; break;
                    case 241: buffs.str = true; break;
                    case 244: buffs.dex = true; break;
                    case 242: buffs.agi = true; break;
                    case 243: buffs.vit = true; break;
                    case 246: buffs.luk = true; break;
                    case 245: buffs.intel = true; break;
                    case 150: buffs.resentment = true; break;
                    case 151: buffs.drowsiness = true; break;
                }
            }
        }
        static DateTime nextSpeedKeyTime = DateTime.MinValue;
        static DateTime nextPeriodicKeyTime = DateTime.MinValue;
        static void HandleActions(IntPtr hProcess, IntPtr hWnd, PlayerStatus status, Buffs buffs, double spThreshold, Config config, bool HighPingMode)
        {
            if (config.statusRecoveryKey != -1 && buffs.negativeStatus)
            {
                PressKeyNoDl(hWnd, config.statusRecoveryKey);
                return;
            }

            if (config.spKey != -1 && Percent(status.spValue, status.spMax) < config.spThreshold)
            {
                PressSPKey(hWnd, config.spKey);
                return;
            }
            if (config.speedKey != -1 && !buffs.speed)
            {
                PressKey(hWnd, config.speedKey, config.autoBuffDelay);
                nextSpeedKeyTime = DateTime.Now.AddMilliseconds(3800);
                return;
            }
            if (config.speedKey != -1 && DateTime.Now >= nextSpeedKeyTime)
            {
                Thread.Sleep(80);
                PressKey(hWnd, config.speedKey, 15);
                PressKey(hWnd, config.speedKey, 15);
                nextSpeedKeyTime = DateTime.Now.AddMilliseconds(3800);
                return;
            }

            if (config.resentmentKey != -1 && !buffs.resentment)
            {
                PressKey(hWnd, config.resentmentKey, config.autoBuffDelay);
                return;
            }

            if (config.drowsinessKey != -1 && !buffs.drowsiness)
            {
                PressKey(hWnd, config.drowsinessKey, config.autoBuffDelay);
                return;
            }

            if (!buffs.quag)
            {
                if (config.gloomKey != -1 && !buffs.gloom)
                {
                    PressKey(hWnd, config.gloomKey, config.autoBuffDelay);
                    return;
                }
            }
            if (config.aspdKey != -1 && !buffs.aspd)
            {
                PressKey(hWnd, config.aspdKey, config.autoBuffDelay);
                return;
            }
            if (config.sunKey != -1 && !buffs.sun)
            {
                PressKey(hWnd, config.sunKey, config.autoBuffDelay);
                return;
            }
            if (config.fireKey != -1 && !buffs.fire)
            {
                PressKey(hWnd, config.fireKey, config.autoBuffDelay);
                return;
            }
            if (config.waterKey != -1 && !buffs.water)
            {
                PressKey(hWnd, config.waterKey, config.autoBuffDelay);
                return;
            }
            if (config.windKey != -1 && !buffs.wind)
            {
                PressKey(hWnd, config.windKey, config.autoBuffDelay);
                return;
            }
            if (config.strKey != -1 && !buffs.str)
            {
                PressKey(hWnd, config.strKey, config.autoBuffDelay);
                return;
            }
            if (config.dexKey != -1 && !buffs.dex)
            {
                PressKey(hWnd, config.dexKey, config.autoBuffDelay);
                return;
            }
            if (config.agiKey != -1 && !buffs.agi)
            {
                PressKey(hWnd, config.agiKey, config.autoBuffDelay);
                return;
            }
            if (config.vitKey != -1 && !buffs.vit)
            {
                PressKey(hWnd, config.vitKey, config.autoBuffDelay);
                return;
            }
            if (config.lukKey != -1 && !buffs.luk)
            {
                PressKey(hWnd, config.lukKey, config.autoBuffDelay);
                return;
            }
            if (config.intelKey != -1 && !buffs.intel)
            {
                PressKey(hWnd, config.intelKey, config.autoBuffDelay);
                return;
            }
            if (config.gloriaKey != -1 && !buffs.gloria)
            {
                PressKey(hWnd, config.gloriaKey, config.autoBuffDelay);
                return;
            }
            if (config.truesightKey != -1 && !buffs.truesight)
            {
                PressKey(hWnd, config.truesightKey, config.autoBuffDelay);
                return;
            }
            if (config.abrasiveKey != -1 && !buffs.abrasive)
            {
                PressKey(hWnd, config.abrasiveKey, config.autoBuffDelay);
                return;
            }
            if (config.autoguardKey != -1 && !buffs.autoguard)
            {
                PressKey(hWnd, config.autoguardKey, config.autoBuffDelay);
                return;
            }
            if (config.reflectshieldKey != -1 && !buffs.reflectshield)
            {
                PressKey(hWnd, config.reflectshieldKey, config.autoBuffDelay);
                return;
            }
            if (config.defenderKey != -1 && !buffs.defender)
            {
                PressKey(hWnd, config.defenderKey, config.autoBuffDelay);
                return;
            }
            if (config.periodicKey != -1 && DateTime.Now >= nextPeriodicKeyTime)
            {
                PressKey(hWnd, config.periodicKey, config.autoBuffDelay);
                nextPeriodicKeyTime = nextPeriodicKeyTime == DateTime.MinValue
                    ? DateTime.Now.AddMilliseconds(config.periodicDelay)
                    : nextPeriodicKeyTime.AddMilliseconds(config.periodicDelay);
                return;
            }

            // If none of the above, Use HP pots instead :)
            if (HighPingMode)
            {
                PressHPKey(hWnd, config.hpKey);
                Thread.Sleep(15);
            }
        }
        static void WriteIntToMemory(IntPtr hProcess, int address, int value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteProcessMemory(hProcess, (IntPtr)address, buffer, buffer.Length, out _);
        }
        static bool FindActiveSpamKey(List<SpamKey> keys, out int keyCode, out bool useHold)
        {
            foreach (var spamKey in keys)
            {
                if ((GetAsyncKeyState(spamKey.KeyCode) & 0x8000) != 0)
                {
                    keyCode = spamKey.KeyCode;
                    useHold = spamKey.UseHold;
                    return true;
                }
            }

            keyCode = -1;
            useHold = false;
            return false;
        }
        static void SkillSpammerUnifiedThread(IntPtr hProcess, IntPtr hWnd, int address, int holdKey, int holdDelay, List<SpamKey> clickKeys, List<SpamKey> noClickKeys, int spamDelay)
        {
            bool holdActive = false;
            int activeKey = -1;
            bool useHold = false;

            while (spammerRunning)
            {
                bool isClickKey = clickKeys.Any(k => (GetAsyncKeyState(k.KeyCode) & 0x8000) != 0);

                if (activeKey == -1)
                {
                    // Look for active key
                    var found = FindActiveSpamKey(clickKeys, out activeKey, out useHold);
                    if (!found)
                        found = FindActiveSpamKey(noClickKeys, out activeKey, out useHold);

                    if (found && holdKey != -1 && useHold && !holdActive)
                    {
                        PostMessage(hWnd, WM_KEYDOWN, holdKey, 0);
                        Thread.Sleep(holdDelay);
                        PostMessage(hWnd, WM_KEYUP, holdKey, 0);
                        holdActive = true;
                    }

                    Thread.Sleep(14); // small sleep only if no active key
                    continue;
                }

                // If key is still being held
                if ((GetAsyncKeyState(activeKey) & 0x8000) != 0)
                {
                    WriteIntToMemory(hProcess, address, 500);
                    PostMessage(hWnd, WM_KEYDOWN, activeKey, 0);

                    if (isClickKey)
                    {
                        PostMessage(hWnd, 0x0201, 0x0001, 0); // mouse down
                        Thread.Sleep(spamDelay);
                        PostMessage(hWnd, 0x0202, 0x0000, 0); // mouse up
                    }

                    Thread.Sleep(spamDelay);
                }
                else
                {
                    // Key was released
                    if (holdActive && holdKey != -1 && useHold)
                    {
                        SendMessage(hWnd, WM_KEYDOWN, holdKey, 0);
                        Thread.Sleep(holdDelay);
                        SendMessage(hWnd, WM_KEYUP, holdKey, 0);
                        holdActive = false;
                    }

                    PostMessage(hWnd, WM_KEYUP, activeKey, 0);
                    activeKey = -1;
                }
            }
        }

        static void StartUnifiedSpammerThread(IntPtr hProcess, IntPtr hWnd, Config config)
        {
            var thread = new Thread(() =>
            {
                SkillSpammerUnifiedThread(
                    hProcess,
                    hWnd,
                    config.mouseBoostAddress,
                    config.holdKey,
                    config.holdKeyDelay,
                    new List<SpamKey>(config.skillSpamClickKeys),
                    new List<SpamKey>(config.skillSpamNoClickKeys),
                    config.skillSpamDelay
                );
            })
            {
                IsBackground = true
            };

            thread.Start();
        }
        static void MacroSwitcherThread(IntPtr hWnd, int whipKey, int pdfmKey, int combatKnifeKey, int pdfmDelay, int combatKnifeDelay)
        {
            while (true)
            {
                if ((GetAsyncKeyState(whipKey) & 0x8000) != 0)
                {
                    PressKey(hWnd, whipKey, 14);
                    PressKeyNoDl(hWnd, pdfmKey);
                    Thread.Sleep(pdfmDelay);
                    PressKeyNoDl(hWnd, combatKnifeKey);
                    Thread.Sleep(combatKnifeDelay);
                }

                Thread.Sleep(15);
            }
        }
        static void StartMacroSwitcherThread(IntPtr hWnd, Config config)
        {
            var thread = new Thread(() =>
            {
                MacroSwitcherThread(
                    hWnd,
                    config.whipKey,
                    config.pdfmKey,
                    config.combatKnifeKey,
                    config.pdfmDelay,
                    config.combatKnifeDelay
                );
            })
            {
                IsBackground = true
            };

            thread.Start();
        }
        static void ChainMacroThread(IntPtr hWnd, int triggerKey, List<(int key, int delay)> sequence)
        {
            while (true)
            {
                if ((GetAsyncKeyState(triggerKey) & 0x8000) != 0)
                {
                    foreach (var (key, delay) in sequence)
                    {
                        PressKey(hWnd, key, delay);
                    }

                    while ((GetAsyncKeyState(triggerKey) & 0x8000) != 0) Thread.Sleep(30);
                }

                Thread.Sleep(15);
            }
        }
        static void StartChainMacroThread(IntPtr hWnd, Config config)
        {
            var thread = new Thread(() =>
            {
                ChainMacroThread(
                    hWnd,
                    config.chainMacroKey,
                    new List<(int key, int delay)>(config.chainMacroSequence)
                );
            })
            {
                IsBackground = true
            };

            thread.Start();
        }
        static void Main()
        {
            Console.Title = "Smookyz";
            var config = new Config();
            LoadOrCreateConfig(config);

            IntPtr hWnd = IntPtr.Zero;
            IntPtr hProcess = IntPtr.Zero;
            int pid = 0;

            while (hWnd == IntPtr.Zero || hProcess == IntPtr.Zero)
            {
                hWnd = FindWindow(null, config.windowTitle);
                if (hWnd != IntPtr.Zero)
                {
                    GetWindowThreadProcessId(hWnd, out pid);
                    hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, pid);
                }

                if (hWnd == IntPtr.Zero || hProcess == IntPtr.Zero)
                {
                    Console.Clear();
                    Console.WriteLine("Waiting for Ragnarok window...");
                    Thread.Sleep(1000);
                }
            }

            int baseAddr = config.baseAddress;
            int hpAddr = baseAddr;
            int spAddr = baseAddr + 8;
            int buffAddr = baseAddr + 0x474;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Server Found!\nSmookyz [ON]\nHave A Blessed Day <3\n");
            Console.ResetColor();

            // Reserve lines for status display
            Console.WriteLine(); // Line for pause status
            Console.WriteLine(); // Line for high ping mode status
            int statusTopLine = Console.CursorTop - 2;

            if (config.skillSpamClickKeys.Count > 0 || config.skillSpamNoClickKeys.Count > 0)
            {
                spammerRunning = true;
                StartUnifiedSpammerThread(hProcess, hWnd, config);
            }

            if (config.whipKey != -1 && config.pdfmKey != -1 && config.combatKnifeKey != -1)
            {
                StartMacroSwitcherThread(hWnd, config);
            }
            if (config.chainMacroEnabled && config.chainMacroSequence.Count > 0 && config.chainMacroKey != -1)
            {
                StartChainMacroThread(hWnd, config);
            }

            PlayerStatus status = new();
            Buffs buffs = new();

            bool HighPingMode = false;
            bool lastHighPingState = false;

            bool isPaused = false;
            bool lastPausedState = false;

            int counter = 0;
            int debounceDelayMs = 600;
            DateTime lastHighPingModeToggle = DateTime.MinValue;

            int pauseKey = config.fullPauseKey;
            int debounceMs = 600;
            DateTime lastPauseToggle = DateTime.MinValue;

            while (true)
            {
                // Pause toggle
                if (pauseKey != -1 && (GetAsyncKeyState(pauseKey) & 0x8000) != 0)
                {
                    if ((DateTime.Now - lastPauseToggle).TotalMilliseconds > debounceMs)
                    {
                        isPaused = !isPaused;
                        Console.Beep(400, 300);
                        lastPauseToggle = DateTime.Now;
                    }
                }

                // High Ping toggle (allowed during pause or not paused)
                if ((GetAsyncKeyState(config.HighPingModeToggle) & 0x8000) != 0)
                {
                    if ((DateTime.Now - lastHighPingModeToggle).TotalMilliseconds > debounceDelayMs)
                    {
                        HighPingMode = !HighPingMode;
                        lastHighPingModeToggle = DateTime.Now;
                    }
                }

                // Update Toggle Status
                if (lastPausedState != isPaused || lastHighPingState != HighPingMode)
                {
                    Console.SetCursorPosition(0, statusTopLine);
                    Console.ForegroundColor = isPaused ? ConsoleColor.Red : ConsoleColor.Green;
                    Console.Write("SmookyzAP: " + (isPaused ? "OFF " : "ON ") + "   "); // clear old text

                    Console.SetCursorPosition(0, statusTopLine + 1);
                    Console.ForegroundColor = HighPingMode ? ConsoleColor.DarkGreen : ConsoleColor.Yellow;
                    Console.Write("High Ping Mode: " + (HighPingMode ? "ON " : "OFF ") + "   ");

                    Console.ResetColor();
                    lastPausedState = isPaused;
                    lastHighPingState = HighPingMode;
                }

                if (isPaused)
                {
                    Thread.Sleep(20);
                    continue;
                }

                ReadHpOnly(hProcess, hpAddr, ref status);
                if (status.hpValue != status.hpMax)
                {
                    PressHPKey(hWnd, config.hpKey);
                    counter++;
                    Thread.Sleep(15);
                    if (counter == 3)
                    {
                        ReadSp(hProcess, spAddr, ref status);
                        if (Percent(status.spValue, status.spMax) < config.spThreshold)
                        {
                            PressSPKey(hWnd, config.spKey);
                            Thread.Sleep(15);
                        }
                        counter = 0;
                    }
                    continue;
                }

                ReadSp(hProcess, spAddr, ref status);
                buffs = new Buffs();
                CheckBuffs(hProcess, buffAddr, buffs);
                HandleActions(hProcess, hWnd, status, buffs, config.spThreshold, config, HighPingMode);

                Thread.Sleep(14);
            }
        }

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
    }
}
