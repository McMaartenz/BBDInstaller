﻿using BandagedBD.Panels;
using System;
using System.Net;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace BandagedBD {

    public enum PanelTypes { License, Action, InstallConfig, RepairConfig, UninstallConfig, Install, Repair, Uninstall, NONE };

    public partial class FormMain : Form {

        private PanelTypes CurrentPanel = PanelTypes.NONE;
        public PanelTypes Action = PanelTypes.NONE;

        private string currentTag = "v" + Properties.Resources.Version;
        private string remoteTag = "v";

        private Dictionary<PanelTypes, IPanel> panelMap = new Dictionary<PanelTypes, IPanel>();


        public FormMain() {
            InitializeComponent();
            this.Text = "BandagedBD Installer v" + Properties.Resources.Version;
            logo.Click += Logo_Click;

            panelMap[PanelTypes.License] = new LicensePanel();
            panelMap[PanelTypes.Action] = new ActionPanel();
            panelMap[PanelTypes.InstallConfig] = new InstallConfigPanel();
            panelMap[PanelTypes.UninstallConfig] = new UninstallConfigPanel();
            panelMap[PanelTypes.RepairConfig] = new RepairConfigPanel();
            panelMap[PanelTypes.Install] = new InstallPanel();
            panelMap[PanelTypes.Uninstall] = new UninstallPanel();
            panelMap[PanelTypes.Repair] = new RepairPanel();

            foreach (KeyValuePair<PanelTypes, IPanel> entry in panelMap) {
                entry.Value.SetWindow(this);
                entry.Value.Control.Dock = DockStyle.Fill;
            }

            if (!Properties.Settings.Default.AgreedToTerms) SwitchPanel(PanelTypes.License);
            else SwitchPanel(PanelTypes.Action);

            Task.Run(checkForUpdate).ContinueWith(result => {
                if (result.Result) showUpdateNotice();
            });
        }

        public void SetTitle(string title) => lblTitle.Text = title;

        public IPanel GetPanel(PanelTypes which) {
            return panelMap[which];
        }

        public void SwitchPanel(PanelTypes which) {
            if (CurrentPanel != PanelTypes.NONE) panelDock.Controls.Remove(panelMap[CurrentPanel].Control);

            CurrentPanel = which;
            panelDock.Controls.Add(panelMap[which].Control);
            SetTitle($"BandagedBD — {panelMap[which].Title}");
            panelMap[which].OnShow();

            if (panelMap[CurrentPanel].PreviousPanel == PanelTypes.NONE) btnBack.HideDisable();
            if (panelMap[CurrentPanel].NextPanel == PanelTypes.NONE) {
                btnBack.HideDisable();
                btnNext.HideDisable();
                btnCancel.ShowEnable("Exit");
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            var g = e.Graphics;
            g.FillRectangle(headerBrush, headerShape);
            g.DrawLine(headerPen, 0, 85, Width, 85);
        }

        private void btnNext_Click(object sender, EventArgs e) {
            if (CurrentPanel == PanelTypes.License) {
                Properties.Settings.Default.AgreedToTerms = true;
                Properties.Settings.Default.Save();
            }
            SwitchPanel(panelMap[CurrentPanel].NextPanel);
        }

        private void btnCancel_Click(object sender, EventArgs e) {
            Application.Exit();
        }

        private void btnBack_Click(object sender, EventArgs e) {
            SwitchPanel(panelMap[CurrentPanel].PreviousPanel);
        }

        public void Fail() {
            btnCancel.ShowEnable();
            btnNext.ShowDisable();
            btnBack.ShowEnable();
        }

        private void Logo_Click(object sender, EventArgs e) {
            Utilities.OpenProcess("https://github.com/rauenzi/BetterDiscordApp");
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            Utilities.OpenProcess("https://paypal.me/ZackRauen");
        }

        private async Task<bool> checkForUpdate()
        {
            string endpoint = "https://api.github.com/repos/rauenzi/BBDInstaller/releases/latest";

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;

            string tagNameKey = "\"tag_name\":";
            using (var wc = new TimedWebClient())
            {

                try
                {
                    wc.Headers.Set("User-Agent", "BBDInstaller");
                    string json = wc.DownloadString(endpoint);
                    int tagNameIndex = json.IndexOf(tagNameKey);
                    int versionStart = tagNameIndex + tagNameKey.Length + 1;
                    int versionEnd = json.IndexOf("\"", versionStart);
                    remoteTag = json.Substring(versionStart, versionEnd - versionStart);
                    return string.CompareOrdinal(remoteTag, currentTag) > 0;
                }
                catch
                {
                    return false;
                }

            }
        }

        private void showUpdateNotice()
        {
            DialogResult confirmResult = MessageBox.Show($"There is a newer version of the installer available ({remoteTag}). Would you like to download it now?", "Update Available!", MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.Yes)
            {
                Utilities.OpenProcess($"https://github.com/rauenzi/BBDInstaller/releases/download/{remoteTag}/BandagedBD.exe");
                Application.Exit();
            }
            else
            {
                Application.Exit();
            }
        }
    }
}
