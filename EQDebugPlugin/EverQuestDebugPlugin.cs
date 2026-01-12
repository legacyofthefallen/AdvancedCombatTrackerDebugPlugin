using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

/*
* Project: EverQuest DPS Plugin
* Original: EverQuest 2 English DPS Localization plugin developed by EQAditu
* Description: Missing from the arsenal of the plugin based Advanced Combat Tracker to track EverQuest's current combat messages.  Ignores chat as that is displayed in game.
*/

namespace LotFPlugins
{
    public class EQDebugParser : UserControl, IActPluginV1
    {
        #region Designer generated code (Avoid editing)
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                watcherForDebugFile?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EQDebugParser));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.selectDirectory = new System.Windows.Forms.Button();
            this.eqDirectory = new System.Windows.Forms.Label();
            this.directoryPathTB = new System.Windows.Forms.TextBox();
            this.watcherForDebugFile = new System.IO.FileSystemWatcher();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.watcherForDebugFile)).BeginInit();
            this.SuspendLayout();
            // 
            // selectDirectory
            // 
            resources.ApplyResources(this.selectDirectory, "selectDirectory");
            this.selectDirectory.Name = "selectDirectory";
            this.selectDirectory.UseVisualStyleBackColor = true;
            this.selectDirectory.Click += new System.EventHandler(this.SelectDirectoryClick);
            this.selectDirectory.MouseEnter += new System.EventHandler(this.OnMouseEnterButtonArea);
            this.selectDirectory.MouseLeave += new System.EventHandler(this.OnMouseLeaveButtonArea);
            // 
            // eqDirectory
            // 
            resources.ApplyResources(this.eqDirectory, "eqDirectory");
            this.eqDirectory.Name = "eqDirectory";
            // 
            // directoryPathTB
            // 
            resources.ApplyResources(this.directoryPathTB, "directoryPathTB");
            this.directoryPathTB.Name = "directoryPathTB";
            this.directoryPathTB.ReadOnly = true;
            this.directoryPathTB.TextChanged += new System.EventHandler(this.DirectoryPathTxtBox_TextChanged);
            this.directoryPathTB.MouseEnter += new System.EventHandler(this.OnMouseEnterDirectory);
            this.directoryPathTB.MouseLeave += new System.EventHandler(this.OnMouseLeaveButtonArea);
            // 
            // EQDPSParser
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.eqDirectory);
            this.Controls.Add(this.selectDirectory);
            this.Controls.Add(this.directoryPathTB);
            this.Controls.Add(this.groupBox1);
            this.Name = "EQDPSParser";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.watcherForDebugFile)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        #endregion

        #region Class Members       
        string settingsFile;
        SettingsSerializer xmlSettings;
        readonly string PluginSettingsFileName = $"Config{Path.DirectorySeparatorChar}ACT_EverQuest_Debug_Parser.config.xml";
        #region UI Class Members
        TreeNode optionsNode = null;
        Label lblStatus;    // The status label that appears in ACT's Plugin tab
        private GroupBox groupBox1;
        private TextBox directoryPathTB;
        private Button selectDirectory;
        private Label eqDirectory;
        #endregion
        String EverQuestDirectoryPath;
        FileSystemWatcher watcherForDebugFile;
        #endregion

        /// <summary>
        /// Constructor that calls initialize component
        /// </summary>
        public EQDebugParser()
        {
            InitializeComponent();
        }

        #region Plugin Class Interface Methods
        /// <summary>
        /// Called by the ACT program to start the plugin initialization
        /// Calls regex initialization methods and check for update methods
        /// assigns methods to the delegates in ActGlobals class
        /// </summary>
        /// <param name="pluginScreenSpace"></param>
        /// <param name="pluginStatusText"></param>
        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, PluginSettingsFileName);
            lblStatus = pluginStatusText;   // Hand the status label's reference to our local var

            //pluginScreenSpace.Controls.Add(this);
            this.Dock = DockStyle.Fill;
            foreach (TreeNode tn in ActGlobals.oFormActMain.OptionsTreeView.Nodes)
            {
                if (tn.Text.Equals("Data Correction"))
                {
                    Action optionsControlSetsAdd = () =>
                    {
                        optionsNode = tn.Nodes.Add($"{typeof(EQDebugParser).Assembly.GetName().Name}");
                        // Register our user control(this) to our newly create node path.  All controls added to the list will be laid out left to right, top to bottom
                        ActGlobals.oFormActMain.OptionsControlSets.Add($@"Data Correction\{typeof(EQDebugParser).Assembly.GetName().Name}",
                            new List<Control> { this });
                        Label lblConfig = new Label
                        {
                            AutoSize = true,
                            Text = "Find the applicable options in the Options tab, Data Correction section."
                        };

                        lblConfig.ImageAlign = ContentAlignment.MiddleLeft;
                        lblConfig.TextAlign = ContentAlignment.MiddleCenter;

                        pluginScreenSpace.Controls.Add(lblConfig);
                    };

                    if (ActGlobals.oFormActMain.InvokeRequired)
                    {
                        ActGlobals.oFormActMain.Invoke(optionsControlSetsAdd);
                    }
                    else
                    {
                        optionsControlSetsAdd.Invoke();
                    }
                    break;
                }
            }

            xmlSettings = new SettingsSerializer(this); // Create a new settings serializer and pass it this instance
            LoadSettings();
            ChangePluginStatusLabel($"{typeof(EQDebugParser).Assembly.GetName().Name} started");
        }

        /// <summary>
        /// Removes methods from the delegates assigned during initialization
        /// attemps to save the settings and then update the plugin dock with status of the exit
        /// </summary>
        public void DeInitPlugin()
        {
            Action removeOptionsFromMainForm = () =>
            {
                if (!(optionsNode == null))    // If we added our user control to the Options tab, remove it
                {
                    optionsNode.Remove();
                    ActGlobals.oFormActMain.OptionsControlSets.Remove($@"Data Correction\{Properties.PluginRegex.pluginName}");
                }
            };

            if (ActGlobals.oFormActMain.InvokeRequired)
            {
                ActGlobals.oFormActMain.Invoke(removeOptionsFromMainForm);
            }
            else
            {
                removeOptionsFromMainForm.Invoke();
            }
            SaveSettings();
            ChangePluginStatusLabel($"{typeof(EQDebugParser).Assembly.GetName().Name} {Properties.PluginRegex.pluginExited}");
        }
        #endregion

        #region Settings
        //=/ <summary>
        /// Loads settings file and attempts to assign values to the controls added in the method
        /// </summary>
        void LoadSettings()
        {
            Action loadSettings = new Action(() =>
            {
                xmlSettings.AddControlSetting(directoryPathTB.Name, directoryPathTB);

                if (File.Exists(settingsFile))
                {
                    using (FileStream settingsFileStream = new FileStream(settingsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (XmlTextReader xReader = new XmlTextReader(settingsFileStream))
                    {
                        try
                        {
                            while (xReader.Read())
                            {
                                if (xReader.NodeType == XmlNodeType.Element)
                                {
                                    if (xReader.LocalName == "SettingsSerializer")
                                        xmlSettings.ImportFromXml(xReader);
                                }
                            }
                        }
                        catch (ArgumentNullException ex)
                        {
                            ChangePluginStatusLabel($"Argument Null for {ex.ParamName} with message: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            ChangePluginStatusLabel($"With message: {ex.Message}");
                        }
                    }
                    if (!Directory.Exists(directoryPathTB.Text))
                    {
                        MessageBox.Show($"directory path for EQ Game does not exist");
                    }                   
                }
                else
                {
                    ChangePluginStatusLabel($"{settingsFile} does not exist and no settings were loaded, first time loading {Properties.PluginRegex.pluginName}?");
                    SaveSettings();
                }
            });

            if (this.InvokeRequired)
                this.Invoke(loadSettings);
            else
                loadSettings.Invoke();
        }

        /// <summary>
        /// Saves the settings file usually called when there is a change in the settings, 
        /// a settings file doesn't exist during LoadSettings method call, 
        /// or during the exit of the plugin
        /// </summary>
        void SaveSettings()
        {
            Action action = new Action(() =>
                {
                    try
                    {
                        using (FileStream fs = new FileStream(settingsFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                        {
                            using (XmlTextWriter xWriter = new XmlTextWriter(fs, Encoding.UTF8))
                            {
                                xWriter.Formatting = Formatting.Indented;
                                xWriter.Indentation = 1;
                                xWriter.IndentChar = '\t';
                                xWriter.WriteStartDocument(true);
                                xWriter.WriteStartElement("Config");    // <Config>
                                xWriter.WriteStartElement("SettingsSerializer");    // <Config><SettingsSerializer>
                                xmlSettings.ExportToXml(xWriter);   // Fill the SettingsSerializer XML
                                xWriter.WriteEndElement();  // </SettingsSerializer>
                                xWriter.WriteEndElement();  // </Config>
                                xWriter.WriteEndDocument(); // Tie up loose ends (shouldn't be any)
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ActGlobals.oFormActMain.WriteExceptionLog(ex, "Failed to save file in entirety");
                    }
                });
            if (this.InvokeRequired)
                this.Invoke(action);
            else
                action.Invoke();
        }
        #endregion

        #region String Parsing

        /// <summary>
        /// Parsess the date and time based on the EverQuest character log time stamp format
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns>DateTime</returns>
        internal DateTime ParseEQTimeStampFromLog(String timeStamp)
        {
            DateTime.TryParseExact(timeStamp, Properties.PluginRegex.eqDateTimeStampFormat, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.AssumeLocal, out DateTime currentEQTimeStamp);
            return currentEQTimeStamp;
        }

        #endregion

        #region File System Watcher

        private FileSystemWatcher watcherForDebugChangeRoster;

        private void DirectoryPathTxtBox_TextChanged(object sender, EventArgs e)
        {
            if (Directory.Exists(directoryPathTB.Text))
            {
                SetWatcherToDirectory();
                ChangePluginStatusLabel($"EverQuest install directory changed to {directoryPathTB.Text}");

            }
            else
            {
                ChangePluginStatusLabel("EverQuest install directory is missing from plugin settings.");
            }
        }

        private void SetWatcherToDirectory()
        {
            if (Directory.Exists(EverQuestDirectoryPath))
            {
                watcherForDebugChangeRoster.Path = Path.GetFullPath(String.Join(Path.DirectorySeparatorChar.ToString(), new String[] { EverQuestDirectoryPath, "Logs" }));
                watcherForDebugChangeRoster.Filter = Properties.PluginRegex.DbgFileName;
                watcherForDebugChangeRoster.IncludeSubdirectories = false;
                watcherForDebugChangeRoster.NotifyFilter = NotifyFilters.LastWrite;
            }
            else
            {
                //watcherForDebugFile.Dispose();
                watcherForDebugChangeRoster?.Dispose();
            }
        }
        #endregion

        #region UI Elements
        #region Control Event Methods

        private void OnMouseEnterButtonArea(object sender, EventArgs e)
        {
            ChangeSetOptionsHelpText("Click to select EverQuest directory");
        }

        private void OnMouseLeaveButtonArea(object sender, EventArgs e)
        {
            ChangeSetOptionsHelpText(Properties.PluginRegex.MouseLeave);
        }

        private void OnMouseEnterDirectory(object sender, EventArgs e)
        {
            ChangeSetOptionsHelpText($"EverQuest directory path currently set");
        }

        private void ChangeSetOptionsHelpText(String text)
        {
            Action changeText = () => ActGlobals.oFormActMain.SetOptionsHelpText(text);

            if (ActGlobals.oFormActMain.InvokeRequired)
            {
                ActGlobals.oFormActMain.Invoke(changeText);
            }
            else
            {
                changeText.Invoke();
            }
        }

        private void OnWatcherCreated(object sender, FileSystemEventArgs e)
        {
            if (ActGlobals.oFormActMain.ActiveZone.ActiveEncounter == null)
            {
                ChangePluginStatusLabel("No active encounter and no raid allies will be tracked.");
                return;
            }
            ChangePluginStatusLabel($"Reading {e.FullPath}");
            Regex filename = new Regex(Properties.PluginRegex.DbgFileName);
            Match m = filename.Match(e.Name);
            if (e.ChangeType.HasFlag(WatcherChangeTypes.Created) && m.Success)
            {
                List<CombatantData> data = new List<CombatantData>();

                using (StreamReader sr = new StreamReader(new FileStream(e.FullPath, FileMode.Open)))
                {
                    String line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        Match raidLineMatch = raidAllyLineRegex.Match(line);
                        CombatantData combatantData = new CombatantData(raidLineMatch.Groups["playerName"].Value, ActGlobals.oFormActMain.ActiveZone.ActiveEncounter);

                        if (combatantData != null && combatantData.Name != ActGlobals.charName)
                            data.Add(combatantData);
                    }
                }

                ActGlobals.oFormActMain.ActiveZone.ActiveEncounter.SetAllies(data);
            }
        }

        #endregion
        private void SelectDirectoryClick(object sender, EventArgs e)
        {
            using (var EverQuestInstallPath = new FolderBrowserDialog())
            {
                DialogResult dr = EverQuestInstallPath.ShowDialog();

                if (dr == DialogResult.OK)
                {
                    EverQuestDirectoryPath = EverQuestInstallPath.SelectedPath;
                    if (Directory.Exists(EverQuestDirectoryPath))
                    {
                        ChangeDirectoryPathAfterDialog(EverQuestInstallPath.SelectedPath);
                        SetWatcherToDirectory();
                        SaveSettings();
                    }
                    else
                        throw new DirectoryNotFoundException($"{EverQuestDirectoryPath} does not exist.");
                }
                else
                {
                    MessageBox.Show($"Dialog result was not an OK message.", "Result was not ok directory path was not changed by Dialog.", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ChangeDirectoryPathAfterDialog(String status)
        {
            ChangeTextInControl(directoryPathTB, status);
        }

        private void ChangeTextInControl(Control control, String text)
        {
            switch (control.InvokeRequired)
            {
                case true:
                    this.directoryPathTB.Invoke(new Action(() =>
                    {
                        control.Text = text;
                    }));
                    break;
                case false:
                    control.Text = text;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// updates the status label with thread safety based on whether the plugin needs to invoke the codes in separate thread to update the user interface control
        /// </summary>
        /// <param name="status"></param>
        private void ChangePluginStatusLabel(String status)
        {
            ChangeTextInControl(lblStatus, status);
        }

        #endregion
    }
}
