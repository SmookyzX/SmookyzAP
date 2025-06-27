using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        const uint WM_KEYDOWN = 0x0100;
        const uint WM_KEYUP = 0x0101;

        struct PlayerStatus
        {
            public int hpValue, hpMax, spValue, spMax;
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

            public int pauseKey = 0x24;
            public string windowTitle = "HoneyRO ~";
            public int baseAddress = 0x010DCE10;
            public int autoBuffDelay = 50;
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

            { "HOME", 0x24 }
        };

        static double Percent(double val1, double val2) => (val2 == 0) ? 0 : (val1 / val2) * 100.0;

        static void PressKey(IntPtr hWnd, int key, int delay)
        {
            PostMessage(hWnd, WM_KEYDOWN, key, 0);
            PostMessage(hWnd, WM_KEYUP, key, 0);
            Thread.Sleep(delay);
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
                hpKey=
                spKey=
                statusRecoveryKey=
                pauseKey=HOME

                aspdKey=
                gloomKey=
                sunKey=
                fireKey=
                waterKey=
                windKey=
                strKey=
                dexKey=
                agiKey=
                vitKey=
                lukKey=
                intelKey=
                resentmentKey=
                drowsinessKey=
                speedKey=
                gloriaKey=
                truesightKey=
                abrasiveKey=
                autoguardKey=
                reflectshieldKey=
                defenderKey=

            [Settings]
                spThreshold=
                windowTitle=HoneyRO ~
                baseAddress=010DCE10
                autoBuffDelay=50
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
            int bufferSize = 33;
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

        static void HandleActions(IntPtr hProcess, IntPtr hWnd, PlayerStatus status, Buffs buffs, double spThreshold, Config config, bool paused)
        {
            if (config.statusRecoveryKey != -1 && buffs.negativeStatus)
            {
                PressKey(hWnd, config.statusRecoveryKey, 15);
                return;
            }

            if (config.spKey != -1 && Percent(status.spValue, status.spMax) < config.spThreshold)
            {
                PressKey(hWnd, config.spKey, 15);
                return;
            }
            if (config.speedKey != -1 && !buffs.speed)
            {
                PressKey(hWnd, config.speedKey, config.autoBuffDelay);
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

            // If none of the above, Use HP pots instead :)
            if (!paused)  // <-- only press HP key if NOT paused | Pause check is inside Main
            {
                PressHPKey(hWnd, config.hpKey);
            }
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

            bool paused = false;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Server Found!\nSmookyz [ON]");
            Console.ResetColor();
            PlayerStatus status = new();
            Buffs buffs = new();
            int counter = 0;
            int debounceDelayMs = 600;
            DateTime lastPauseToggle = DateTime.MinValue;
            while (true)
            {
                // Pause toggle key detection (toggle paused on key press)
                if ((GetAsyncKeyState(config.pauseKey) & 0x8000) != 0)
                {
                    if ((DateTime.Now - lastPauseToggle).TotalMilliseconds > debounceDelayMs)
                    {
                        paused = !paused;
                        Console.WriteLine(paused ? "Paused" : "Resumed");
                        lastPauseToggle = DateTime.Now;
                    }
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
                HandleActions(hProcess, hWnd, status, buffs, config.spThreshold, config, paused);

                Thread.Sleep(15);
            }
        }

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
    }
}
