﻿using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using AutoHotkey.Interop;
using System.IO;

namespace EsoHelper
{
    public partial class MainForm : Form
    {
        private const string ProcessNameGame = "eso64";
        private const string ProcessTitleGame = "Elder Scrolls Online";
        private const string ProcessNameLauncher = "Bethesda.net_Launcher";
        private const string ScriptFolder = "scripts";
        private const string PathLightAttackScript = ScriptFolder + "\\eso-light-attack-weave.ahk";

        private DateTime? launcherDetectTime = null;
        private MenuItem itemAutoClose;
        private MenuItem itemInactiveClose;
        private MenuItem itemLightAttackWeave;
        private AutoHotkeyEngine autoHotkeyEngine = new AutoHotkeyEngine(AutoHotKeyVersion.v1);
        private bool lightAttackScriptRunning = false;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Visible = false;
            this.Hide();
            trayIcon.ContextMenu = initializeContextMenu();
            initializeMode();
        }

        private ContextMenu initializeContextMenu()
        {
            ContextMenu trayMenu = new ContextMenu();
            itemAutoClose = new MenuItem("Auto Close Mode", MenuAutoClose);
            itemInactiveClose = new MenuItem("Inactive Close Mode", MenuInactiveClose);
            itemLightAttackWeave = new MenuItem("Auto Light Attack Weaving", MenuLightAttackWeave);
            trayMenu.MenuItems.Add(itemAutoClose);
            trayMenu.MenuItems.Add(itemInactiveClose);
            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add(itemLightAttackWeave);
            trayMenu.MenuItems.Add("-");
            trayMenu.MenuItems.Add("Close Application", MenuExit);

            bool weave = Properties.Settings.Default.LightAttackWeave;
            itemLightAttackWeave.Checked = weave;
            timerLightAttackWeave.Enabled = weave;
            return trayMenu;
        }

        private void setMode(Mode mode)
        {
            Properties.Settings.Default.Mode = (ushort) mode;
            Properties.Settings.Default.Save();
        }

        private void initializeMode()
        {
            var mode = (Mode)Properties.Settings.Default.Mode;
            switch (mode)
            {
                case Mode.AutoClose:
                    itemAutoClose.Checked = true;
                    itemInactiveClose.Checked = false;
                    launcherDetectTime = null;
                    timerLauncherAutoClose.Enabled = true;
                    timerLauncherInactiveClose.Enabled = false;
                    break;
                case Mode.InactiveClose:
                    itemAutoClose.Checked = false;
                    itemInactiveClose.Checked = true;
                    launcherDetectTime = null;
                    timerLauncherAutoClose.Enabled = false;
                    timerLauncherInactiveClose.Enabled = true;
                    break;
                default:
                    throw new Exception("Unknown Mode was set");
            }
        }

        private void MenuExit(object sender, EventArgs e)
        {
            Close();
        }

        private void MenuAutoClose(object sender, EventArgs e)
        {
            setMode(Mode.AutoClose);
            initializeMode();
        }

        private void MenuInactiveClose(object sender, EventArgs e)
        {
            setMode(Mode.InactiveClose);
            initializeMode();
        }

        private void MenuLightAttackWeave(object sender, EventArgs e)
        {
            bool weave = Properties.Settings.Default.LightAttackWeave;
            itemLightAttackWeave.Checked = !weave;
            timerLightAttackWeave.Enabled = !weave;
            Properties.Settings.Default.LightAttackWeave = !weave;
            Properties.Settings.Default.Save();
        }

        private void startScript()
        {
            string script;
            if (File.Exists(PathLightAttackScript))
            {
                script = File.ReadAllText(PathLightAttackScript);
            }
            else
            {
                script = Encoding.UTF8.GetString(Properties.Resources.eso_light_attack_weave);
            }
            autoHotkeyEngine.LoadScript(script);
            autoHotkeyEngine.SetVar("suspend", "false");
            autoHotkeyEngine.UnSuspend();
        }

        private void unloadScript()
        {
            autoHotkeyEngine.Terminate();
            autoHotkeyEngine = new AutoHotkeyEngine(AutoHotKeyVersion.v1);
        }

        private bool isGameRunning()
        {
            Process[] gameProcesses = Process.GetProcessesByName(ProcessNameGame);
            if (gameProcesses.Length <= 0)
            {
                return false;
            }
            foreach (var process in gameProcesses)
            {
                if (process.MainWindowTitle == ProcessTitleGame)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Closes the launcher as soon as the game starts.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timerLauncherAutoClose_Tick(object sender, EventArgs e)
        {
            if (!isGameRunning())
            {
                return;
            }
            Process[] launcherProcesses = Process.GetProcessesByName(ProcessNameLauncher);
            foreach (var process in launcherProcesses)
            {
                process.Kill();
            }
        }

        /// <summary>
        /// Closes the launcher when it was inactive for at least a minute.
        /// Inactive means the launcher is running without the game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timerLauncherInactiveClose_Tick(object sender, EventArgs e)
        {
            Process[] launcherProcesses = Process.GetProcessesByName(ProcessNameLauncher);
            if (launcherProcesses.Length <= 0)
            {
                launcherDetectTime = null;
                return;
            }
            else if ((launcherProcesses.Length > 0 && launcherDetectTime == null) || (launcherProcesses.Length > 0 && isGameRunning()))
            {
                launcherDetectTime = DateTime.Now;
            }
            if ((DateTime.Now - launcherDetectTime).Value.TotalSeconds > 60)
            {
                foreach (var process in launcherProcesses)
                {
                    process.Kill();
                }
            }
        }

        private void timerAutoHotKeyManager_Tick(object sender, EventArgs e)
        {
            if (!isGameRunning())
            {
                if (lightAttackScriptRunning)
                {
                    unloadScript();
                    lightAttackScriptRunning = false;
                }
                return;
            }
            if (!lightAttackScriptRunning)
            {
                startScript();
                lightAttackScriptRunning = true;
            }
        }
    }
}
