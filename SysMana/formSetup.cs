using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Net;
using Microsoft.VisualBasic;
using GenericForms;

namespace SysMana
{
    public partial class formSetup : Form
    {
        Func<string, Image> LoadImg;
        Action<Image> DisposeImg;
        Action LoadMeters, LoadOptions, InitData;
        DataSources data;
        List<Meter> meters;
        bool initiating = false, ignoreChanges = false;

        Color backColor;
        Font font;
        VertAlign align;
        int refresh, opacity, fixedH, updateNotifs;
        bool topMost, transparent, showChangelog;


        public void Init(List<Meter> meters, int refresh, int opacity, int fixedH, Color backColor, VertAlign align, bool topMost, bool transparent, Font font, DataSources data, Action LoadMeters, Action LoadOptions, Action InitData, Func<string, Image> LoadImg, Action<Image> DisposeImg, int updateNotifs, bool showChangelog)
        {
            initiating = true;

            this.data = data;
            this.LoadMeters = LoadMeters;
            this.LoadOptions = LoadOptions;
            this.InitData = InitData;
            this.LoadImg = LoadImg;
            this.DisposeImg = DisposeImg;

            //copy meters list
            this.meters = meters;

            listMeters.Items.Clear();
            foreach (Meter meter in meters)
                listMeters.Items.Add(meter.Data);

            //load system fonts
            comboFont.Items.Clear();

            foreach (FontFamily fontFamily in System.Drawing.FontFamily.Families)
                comboFont.Items.Add(fontFamily.Name);

            //display general options
            numRefresh.Value = refresh;
            numOpacity.Value = opacity;
            numFixedH.Value = fixedH;
            picBackColor.BackColor = backColor;
            comboVertAlign.Text = align.ToString();
            checkTopMost.Checked = topMost;
            checkTransparent.Checked = transparent;
            trackUpdate.Value = updateNotifs;
            checkShowChangelog.Checked = showChangelog;

            comboFont.Text = font.Name;
            numFontSize.Value = (int)font.Size;
            checkFontBold.Checked = font.Bold;
            checkFontItalic.Checked = font.Italic;
            checkFontUnderline.Checked = font.Underline;
            checkFontStrikeout.Checked = font.Strikeout;

            //note current values of general options
            this.refresh = refresh;
            this.opacity = opacity;
            this.fixedH = fixedH;
            this.backColor = backColor;
            this.align = align;
            this.topMost = topMost;
            this.transparent = transparent;
            this.font = font;
            this.updateNotifs = updateNotifs;
            this.showChangelog = showChangelog;

            //runs at startup?
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (rkApp.GetValue("SysMana") != null)
                checkRunAtStartup.Checked = true;

            initiating = false;

            buttMeterSaveChanges.Enabled = false;
            buttOptionsSaveChanges.Enabled = false;
        }

        void checkForMeterChanges()
        {
            if (ignoreChanges)
                buttMeterSaveChanges.Enabled = false;
            else
            {
                Meter meter = meters[listMeters.SelectedIndex];

                buttMeterSaveChanges.Enabled =
                    numLeftMargin.Value != meter.LeftMargin ||
                    numTopMargin.Value != meter.TopMargin ||
                    numDataMin.Value != meter.Min ||
                    numDataMax.Value != meter.Max ||
                    numZoom.Value != meter.Zoom ||
                    comboClickAction.Text != meter.ClickAction ||
                    comboDragFileAction.Text != meter.DragFileAction ||
                    comboMWheelAction.Text != meter.MouseWheelAction ||
                    comboDataSource.Text != meter.Data ||
                    (comboDataSubsource.Visible && comboDataSubsource.Text != meter.DataSubsource) ||
                    comboVisualization.Text != meter.Vis ||
                    txtPrefix.Text != meter.Prefix ||
                    txtPostfix.Text != meter.Postfix ||
                    checkOnlyValue.Checked != meter.OnlyValue ||
                    txtSpinnerImage.Text != meter.Spinner ||
                    numSpinMin.Value != meter.MinSpin ||
                    numSpinMax.Value != meter.MaxSpin ||
                    txtBackground.Text != meter.Background ||
                    txtForeground.Text != meter.Foreground ||
                    comboProgressVector.Text != meter.Vector ||
                    numGraphW.Value != meter.GraphW ||
                    numGraphH.Value != meter.GraphH ||
                    numGraphStepW.Value != meter.GraphStepW ||
                    numGraphLineW.Value != meter.GraphLineW ||
                    numStepInterval.Value != meter.GraphInterval ||
                    picGraphColor.BackColor != meter.GraphLineColor ||
                    checkGraphBorder.Checked != meter.GraphBorder ||
                    txtGraphTexture.Text != meter.GraphTex ||
                    rdbGraphTextureFront.Checked != meter.GraphTexFront ||
                    rdbClock24HourFormat.Checked != meter.Clock24HrFormat ||
                    checkClockPlaySounds.Checked != meter.ClockPlaySounds ||
                    checkClockPlaySoundsOnStartup.Checked != meter.ClockPlaySoundsOnStartup ||
                    txtClockLatitude.Text != meter.ClockLatitude.ToString() ||
                    txtClockLongitude.Text != meter.ClockLongitude.ToString() ||
                    txtClockTimeZone.Text != meter.ClockTimeZone.ToString();
            }
        }

        void checkForGeneralOptionsChanges()
        {
            buttOptionsSaveChanges.Enabled =
                numRefresh.Value != refresh ||
                numOpacity.Value != opacity ||
                numFixedH.Value != fixedH ||
                picBackColor.BackColor != backColor ||
                comboVertAlign.Text != align.ToString() ||
                checkTopMost.Checked != topMost ||
                checkTransparent.Checked != transparent ||
                comboFont.Text != font.Name ||
                numFontSize.Value != (int)font.Size ||
                checkFontBold.Checked != font.Bold ||
                checkFontItalic.Checked != font.Italic ||
                checkFontUnderline.Checked != font.Underline ||
                checkFontStrikeout.Checked != font.Strikeout ||
                trackUpdate.Value != updateNotifs ||
                checkShowChangelog.Checked != showChangelog;
        }

        void saveMeters()
        {
            StreamWriter file = new StreamWriter(Application.StartupPath + "\\meters.txt");

            foreach (Meter meter in meters)
                file.WriteLine(meter.FormatForFile());
            
            file.Close();
        }

        void delMeter()
        {
            if (listMeters.SelectedIndex != -1 && MessageBox.Show("Are you sure you want to permanently delete this meter?", listMeters.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == System.Windows.Forms.DialogResult.Yes)
            {
                meters.RemoveAt(listMeters.SelectedIndex);
                saveMeters();

                listMeters.Items.RemoveAt(listMeters.SelectedIndex);
            }
        }

        bool tryToLoadImg(PictureBox pic, string path)
        {
            if (File.Exists(path))
                try
                {
                    pic.Image = Bitmap.FromFile(path);
                }
                catch
                {
                    return false;
                }
            else
                return false;

            return true;
        }

        void checkImage(TextBox txtBox, PictureBox picBox)
        {
            if (!ignoreChanges && !tryToLoadImg(picBox, txtBox.Text))
                MessageBox.Show("Invalid image. Please specify an image in one of the following formats: jpg, bmp, png, gif.");

            checkForMeterChanges();
        }

        void showOpenDiag(TextBox txtBox, PictureBox picBox)
        {
            if (txtBox.Text != "" && File.Exists(txtBox.Text))
                openDialog.InitialDirectory = Path.GetDirectoryName(txtBox.Text);

            if (openDialog.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
            {
                txtBox.Text = openDialog.FileName;
                checkImage(txtBox, picBox);
            }
        }

        void checkIfNewImg(ref string path)
        {
            if (path.Contains('\\') && File.Exists(path))
            {
                string dest = Application.StartupPath + "\\imgs\\" + Path.GetFileName(path);
                int i = 1;

                while (File.Exists(dest))
                    dest = Application.StartupPath + "\\imgs\\" + Path.GetFileNameWithoutExtension(path) + "_" + i++ + Path.GetExtension(path);

                File.Copy(path, dest);
                path = Path.GetFileName(dest);
            }
        }

        void reloadImgSeq()
        {
            if (meters[listMeters.SelectedIndex].ImgSeqDir != "" && Directory.Exists(Application.StartupPath + "\\imgs\\" + meters[listMeters.SelectedIndex].ImgSeqDir))
            {
                meters[listMeters.SelectedIndex].LoadResources();
                dispImgSeq();
            }
        }

        void dispImgSeq()
        {
            if (meters[listMeters.SelectedIndex].ImgSeqDir == "" || !Directory.Exists(Application.StartupPath + "\\imgs\\" + meters[listMeters.SelectedIndex].ImgSeqDir))
            {
                buttOpenImgSeqDir.Text = "Sequence is empty";
                buttOpenImgSeqDir.Enabled = false;
                lblImgSequence.Text = "0 images in sequence";
            }
            else
            {
                buttOpenImgSeqDir.Text = "Open images directory";
                buttOpenImgSeqDir.Enabled = true;
                lblImgSequence.Text = Directory.GetFiles(Application.StartupPath + "\\imgs\\" + meters[listMeters.SelectedIndex].ImgSeqDir).Length + " images in sequence";
            }
        }

        void showDiskMinMax()
        {
            numDataMin.Value = 0;
            numDataMax.Value = data.GetTotalDiskSpace(comboDataSubsource.Text);
        }

        void refreshUpdateNotifLabel()
        {
            switch (trackUpdate.Value)
            {
                case 0:
                    lblUpdateNotifications.Text = "Always ask";
                    break;
                case 1:
                    lblUpdateNotifications.Text = "Check for update automatically";
                    break;
                case 2:
                    lblUpdateNotifications.Text = "Download update automatically";
                    break;
                case 3:
                    lblUpdateNotifications.Text = "Install update automatically";
                    break;
            }
        }
        
        #region Geolocation search functions
        string dlPage(string URL)
        {
            try
            {
                WebClient web = new WebClient();
                web.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

                return web.DownloadString(URL);
            }
            catch
            {
                return "error";
            }
        }

        double parseCoord(string txt)
        {
            txt = txt.Replace(" ", "");

            double coord = parseCoordElement(ref txt, "&deg;");
            coord += parseCoordElement(ref txt, "\\'") / 60;
            coord += parseCoordElement(ref txt, "&quot;") / 3600;

            if (txt[txt.Length - 1] == 'S' || txt[txt.Length - 1] == 'W')
                coord *= -1;

            return coord;
        }

        double parseCoordElement(ref string txt, string element)
        {
            if (!txt.Contains(element))
                return 0;

            double coordEl = double.Parse(txt.Substring(0, txt.IndexOf(element)));
            txt = txt.Substring(txt.IndexOf(element) + element.Length);

            return coordEl;
        }
        #endregion


        public formSetup()
        {
            InitializeComponent();
        }

        private void checkRunAtStartup_CheckedChanged(object sender, EventArgs e)
        {
            if (!initiating)
            {
                RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (checkRunAtStartup.Checked)
                {
                    rkApp.SetValue("SysMana", Application.ExecutablePath.ToString());
                    MessageBox.Show("SysMana now runs at Windows startup.");
                }
                else
                {
                    rkApp.DeleteValue("SysMana", true);
                    MessageBox.Show("SysMana no longer runs at Windows startup.");
                }
            }
        }

        private void generalOption_ValueChanged(object sender, EventArgs e)
        {
            checkForGeneralOptionsChanges();
        }

        private void generalOption_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                checkForGeneralOptionsChanges();
        }

        private void meterOption_ValueChanged(object sender, EventArgs e)
        {
            checkForMeterChanges();
        }

        private void meterOption_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                checkForMeterChanges();
        }

        private void buttPickBackColor_Click(object sender, EventArgs e)
        {
            colorDialog.Color = picBackColor.BackColor;
            colorDialog.ShowDialog();
            picBackColor.BackColor = colorDialog.Color;

            checkForGeneralOptionsChanges();
        }

        private void buttOptionsSaveChanges_Click(object sender, EventArgs e)
        {
            //update values
            refresh = (int)numRefresh.Value;
            opacity = (int)numOpacity.Value;
            fixedH = (int)numFixedH.Value;
            backColor = picBackColor.BackColor;
            align = (VertAlign)Enum.Parse(typeof(VertAlign), comboVertAlign.Text);
            topMost = checkTopMost.Checked;
            transparent = checkTransparent.Checked;
            updateNotifs = trackUpdate.Value;
            showChangelog = checkShowChangelog.Checked;

            font = new Font(comboFont.Text, (int)numFontSize.Value, Misc.GenFontStyle(checkFontBold.Checked, checkFontItalic.Checked, checkFontUnderline.Checked, checkFontStrikeout.Checked));

            //get most recent form position
            StreamReader fileRdr = new StreamReader(Application.StartupPath + "\\options.txt");
            int left = int.Parse(fileRdr.ReadLine());
            int top = int.Parse(fileRdr.ReadLine());
            fileRdr.Close();

            //save values
            StreamWriter file = new StreamWriter(Application.StartupPath + "\\options.txt");

            file.WriteLine(left);
            file.WriteLine(top);
            file.WriteLine(refresh);
            file.WriteLine(opacity);
            file.WriteLine(fixedH);
            file.WriteLine(backColor.R);
            file.WriteLine(backColor.G);
            file.WriteLine(backColor.B);
            file.WriteLine(align.ToString());
            file.WriteLine(topMost);
            file.WriteLine(transparent);
            file.WriteLine(font.Name);
            file.WriteLine(font.Size);
            file.WriteLine(font.Bold);
            file.WriteLine(font.Italic);
            file.WriteLine(font.Underline);
            file.WriteLine(font.Strikeout);
            file.WriteLine(updateNotifs);
            file.WriteLine(showChangelog);

            file.Close();

            //load new values in main form
            LoadOptions();

            buttOptionsSaveChanges.Enabled = false;
        }

        private void buttAdd_Click(object sender, EventArgs e)
        {
            meters.Add(new Meter("CPU usage", "Text", Application.StartupPath + "\\imgs\\", LoadImg, DisposeImg));
            saveMeters();
            InitData();

            listMeters.Items.Add("CPU usage");
        }

        private void buttDelete_Click(object sender, EventArgs e)
        {
            delMeter();
        }

        private void listMeters_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Delete)
                delMeter();
        }

        private void buttMoveUp_Click(object sender, EventArgs e)
        {
            int ind = listMeters.SelectedIndex;

            Meter temp = meters[ind - 1];
            meters[ind - 1] = meters[ind];
            meters[ind] = temp;

            string tempName = listMeters.Items[ind - 1].ToString();
            listMeters.Items[ind - 1] = listMeters.Items[ind].ToString();
            listMeters.Items[ind] = tempName;

            listMeters.SelectedIndex = ind - 1;
            saveMeters();
        }

        private void buttMoveDown_Click(object sender, EventArgs e)
        {
            int ind = listMeters.SelectedIndex;

            Meter temp = meters[ind + 1];
            meters[ind + 1] = meters[ind];
            meters[ind] = temp;

            string tempName = listMeters.Items[ind + 1].ToString();
            listMeters.Items[ind + 1] = listMeters.Items[ind].ToString();
            listMeters.Items[ind] = tempName;

            listMeters.SelectedIndex = ind + 1;
            saveMeters();
        }

        private void listMeters_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listMeters.SelectedIndex == -1)
            {
                groupMeter.Text = "No meter selected";
                groupMeter.Enabled = false;
                buttMoveUp.Enabled = false;
                buttMoveDown.Enabled = false;

                return;
            }

            ignoreChanges = true;

            groupMeter.Enabled = true;
            groupMeter.Text = "Meter: " + listMeters.Text;

            //general fields
            Meter meter = meters[listMeters.SelectedIndex];

            comboDataSource.Text = meter.Data;
            comboDataSubsource.Text = meter.DataSubsource;
            comboVisualization.Text = meter.Vis;
            comboClickAction.Text = meter.ClickAction;
            comboDragFileAction.Text = meter.DragFileAction;
            comboMWheelAction.Text = meter.MouseWheelAction;
            numLeftMargin.Value = meter.LeftMargin;
            numTopMargin.Value = meter.TopMargin;
            numDataMin.Value = meter.Min;
            numDataMax.Value = meter.Max;
            numZoom.Value = meter.Zoom;

            //visualization-specific fields
            txtPrefix.Text = meter.Prefix;
            txtPostfix.Text = meter.Postfix;
            checkOnlyValue.Checked = meter.OnlyValue;

            txtSpinnerImage.Text = meter.Spinner;
            numSpinMin.Value = meter.MinSpin;
            numSpinMax.Value = meter.MaxSpin;

            txtBackground.Text = meter.Background;
            txtForeground.Text = meter.Foreground;
            comboProgressVector.Text = meter.Vector;

            numGraphW.Value = meter.GraphW;
            numGraphH.Value = meter.GraphH;
            numGraphStepW.Value = meter.GraphStepW;
            numGraphLineW.Value = meter.GraphLineW;
            numStepInterval.Value = meter.GraphInterval;
            picGraphColor.BackColor = meter.GraphLineColor;
            checkGraphBorder.Checked = meter.GraphBorder;
            txtGraphTexture.Text = meter.GraphTex;
            if (meter.GraphTexFront)
                rdbGraphTextureFront.Checked = true;
            else
                rdbGraphTextureBack.Checked = true;

            if (meter.Clock24HrFormat)
                rdbClock24HourFormat.Checked = true;
            else
                rdbClock12HourFormat.Checked = true;
            checkClockPlaySounds.Checked = meter.ClockPlaySounds;
            checkClockPlaySoundsOnStartup.Checked = meter.ClockPlaySoundsOnStartup;
            txtClockLatitude.Text = meter.ClockLatitude.ToString();
            txtClockLongitude.Text = meter.ClockLongitude.ToString();
            txtClockTimeZone.Text = meter.ClockTimeZone.ToString();

            //dispose of any prev imgs
            if (picSpinner.Image != null)
            {
                picSpinner.Image.Dispose();
                picSpinner.Image = null;
            }

            //display any imgs
            tryToLoadImg(picSpinner, Application.StartupPath + "\\imgs\\" + txtSpinnerImage.Text);
            tryToLoadImg(picBackground, Application.StartupPath + "\\imgs\\" + txtBackground.Text);
            tryToLoadImg(picForeground, Application.StartupPath + "\\imgs\\" + txtForeground.Text);
            tryToLoadImg(picGraphTexture, Application.StartupPath + "\\imgs\\" + txtGraphTexture.Text);

            //can move meters up/down??
            buttMoveUp.Enabled = listMeters.SelectedIndex > 0;
            buttMoveDown.Enabled = listMeters.Items.Count > 1 && listMeters.SelectedIndex < listMeters.Items.Count - 1;

            ignoreChanges = false;
        }

        private void comboDataSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            buttChooseTextFile.Visible = false;

            //display data subsource selection if needed
            switch (comboDataSource.Text)
            {
                case "Download speed":
                case "Upload speed":
                    lblDataSubsource.Text = "Connection:";

                    comboDataSubsource.Items.Clear();
                    comboDataSubsource.Items.AddRange(data.ListNetAdapters());
                    
                    lblDataSubsource.Text = "Adapter:";
                    comboDataSubsource.Visible = true;
                    break;
                case "Available disk space":
                case "Used disk space":
                    comboDataSubsource.Items.Clear();
                    comboDataSubsource.Items.AddRange(DriveInfo.GetDrives().ToArray());

                    lblDataSubsource.Text = "Disk:";
                    comboDataSubsource.Visible = true;
                    break;
                case "Audio peak level":
                    comboDataSubsource.Items.Clear();
                    comboDataSubsource.Items.Add("Master");
                    comboDataSubsource.Items.Add("Left channel");
                    comboDataSubsource.Items.Add("Right channel");

                    lblDataSubsource.Text = "Channel:";
                    comboDataSubsource.Visible = true;
                    break;
                case "Text file":
                    comboDataSubsource.Items.Clear();
                    comboDataSubsource.Visible = true;
                    buttChooseTextFile.Visible = true;
                    break;
                default:
                    lblDataSubsource.Text = "";
                    comboDataSubsource.Visible = false;
                    break;
            }

            //display subsource
            bool superIgnoreChanges = false;
            if (ignoreChanges)
                superIgnoreChanges = true;

            ignoreChanges = true;

            comboDataSubsource.Text = meters[listMeters.SelectedIndex].DataSubsource;
            if (comboDataSubsource.SelectedIndex == -1 && comboDataSubsource.Items.Count > 0)
                comboDataSubsource.SelectedIndex = 0;

            if (!superIgnoreChanges)
                ignoreChanges = false;

            //automatically fill in min&max values and prefix&postfix descriptors
            if (comboDataSource.Text != meters[listMeters.SelectedIndex].Data)
            {
                switch (comboDataSource.Text)
                {
                    case "CPU usage":
                    case "Battery percent remaining":
                    case "Wireless signal strength":
                    case "System volume":
                    case "Audio peak level":
                        numDataMin.Value = 0;
                        numDataMax.Value = 100;

                        txtPostfix.Text = "%";
                        break;
                    case "Available memory":
                    case "Used memory":
                        numDataMin.Value = 0;
                        numDataMax.Value = data.TotalRAM;

                        txtPostfix.Text = " MB";
                        break;
                    case "Available disk space":
                    case "Used disk space":
                        showDiskMinMax();

                        txtPostfix.Text = " MB";
                        break;
                    case "Recycle bin file count":
                        numDataMin.Value = 0;

                        txtPostfix.Text = " files";
                        break;
                    case "Recycle bin size":
                        numDataMin.Value = 0;

                        txtPostfix.Text = " MB";
                        break;
                    case "Battery minutes remaining":
                        numDataMin.Value = 0;
                        numDataMax.Value = SystemInformation.PowerStatus.BatteryFullLifetime / 60;

                        txtPostfix.Text = " min";
                        break;
                    case "Download speed":
                    case "Upload speed":
                        txtPostfix.Text = " KB/s";
                        break;
                    case "Text file":
                        numDataMin.Value = 0;
                        numDataMax.Value = 0;

                        txtPostfix.Text = "";

                        if (openTextFile.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
                            comboDataSubsource.Text = openTextFile.FileName;
                        break;
                }

                txtPrefix.Text = comboDataSource.Text + ": ";
            }
            else
            {
                numDataMin.Value = meters[listMeters.SelectedIndex].Min;
                numDataMax.Value = meters[listMeters.SelectedIndex].Max;

                txtPrefix.Text = meters[listMeters.SelectedIndex].Prefix;
                txtPostfix.Text = meters[listMeters.SelectedIndex].Postfix;
            }

            //dota-style clock UI setup
            if (comboDataSource.Text == "Dota-style clock")
            {
                lblDataMin.Visible = false;
                numDataMin.Visible = false;
                lblDataMax.Visible = false;
                numDataMax.Visible = false;

                if (comboVisualization.Items.Count != 1)
                {
                    comboVisualization.Items.Clear();
                    comboVisualization.Items.Add("Dota-style clock");
                    comboVisualization.SelectedIndex = 0;
                }
            }
            else
            {
                lblDataMin.Visible = true;
                numDataMin.Visible = true;
                lblDataMax.Visible = true;
                numDataMax.Visible = true;
                
                if (comboVisualization.Items.Count != 5)
                {
                    comboVisualization.Items.Clear();

                    comboVisualization.Items.Add("Text");
                    comboVisualization.Items.Add("Spinner");
                    comboVisualization.Items.Add("Progress bar");
                    comboVisualization.Items.Add("Image sequence");
                    comboVisualization.Items.Add("Graph");
                }
            }
            
            checkForMeterChanges();
        }

        private void comboDataSubsource_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboDataSource.Text != meters[listMeters.SelectedIndex].Data || comboDataSubsource.Text != meters[listMeters.SelectedIndex].DataSubsource)
                showDiskMinMax();
            else
            {
                numDataMin.Value = meters[listMeters.SelectedIndex].Min;
                numDataMax.Value = meters[listMeters.SelectedIndex].Max;
            }

            checkForMeterChanges();
        }

        private void comboVisualization_SelectedIndexChanged(object sender, EventArgs e)
        {
            checkForMeterChanges();

            //display relevant panel (and other visualization controls)
            switch (comboVisualization.Text)
            {
                case "Text":
                    panelText.Visible = true;
                    panelSpin.Visible = false;
                    panelProgressBar.Visible = false;
                    panelImageSequence.Visible = false;
                    panelGraph.Visible = false;
                    panelDotaClock.Visible = false;

                    numZoom.Enabled = false;
                    lblZoom.Enabled = false;
                    break;
                case "Spinner":
                    panelText.Visible = false;
                    panelSpin.Visible = true;
                    panelProgressBar.Visible = false;
                    panelImageSequence.Visible = false;
                    panelGraph.Visible = false;
                    panelDotaClock.Visible = false;
                    
                    numZoom.Enabled = true;
                    lblZoom.Enabled = true;
                    break;
                case "Progress bar":
                    panelText.Visible = false;
                    panelSpin.Visible = false;
                    panelProgressBar.Visible = true;
                    panelImageSequence.Visible = false;
                    panelGraph.Visible = false;
                    panelDotaClock.Visible = false;

                    numZoom.Enabled = true;
                    lblZoom.Enabled = true;
                    break;
                case "Image sequence":
                    panelText.Visible = false;
                    panelSpin.Visible = false;
                    panelProgressBar.Visible = false;
                    panelImageSequence.Visible = true;
                    panelGraph.Visible = false;
                    panelDotaClock.Visible = false;

                    numZoom.Enabled = true;
                    lblZoom.Enabled = true;

                    dispImgSeq();
                    break;
                case "Graph":
                    panelText.Visible = false;
                    panelSpin.Visible = false;
                    panelProgressBar.Visible = false;
                    panelImageSequence.Visible = false;
                    panelGraph.Visible = true;
                    panelDotaClock.Visible = false;

                    numZoom.Enabled = true;
                    lblZoom.Enabled = true;

                    dispImgSeq();
                    break;
                case "Dota-style clock":
                    panelText.Visible = false;
                    panelSpin.Visible = false;
                    panelProgressBar.Visible = false;
                    panelImageSequence.Visible = false;
                    panelGraph.Visible = false;
                    panelDotaClock.Visible = true;

                    numZoom.Enabled = true;
                    lblZoom.Enabled = true;
                    break;
            }
        }

        private void buttMeterSaveChanges_Click(object sender, EventArgs e)
        {
            if (listMeters.SelectedIndex != -1)
            {
                Meter meter = meters[listMeters.SelectedIndex];

                //general fields
                meter.LeftMargin = (int)numLeftMargin.Value;
                meter.TopMargin = (int)numTopMargin.Value;
                meter.Min = (int)numDataMin.Value;
                meter.Max = (int)numDataMax.Value;
                meter.Zoom = (int)numZoom.Value;
                meter.Data = comboDataSource.Text;
                meter.DataSubsource = comboDataSubsource.Text;
                meter.Vis = comboVisualization.Text;
                meter.ClickAction = comboClickAction.Text;
                meter.DragFileAction = comboDragFileAction.Text;
                meter.MouseWheelAction = comboMWheelAction.Text;

                //visualization-specific fields
                meter.Prefix = txtPrefix.Text;
                meter.Postfix = txtPostfix.Text;
                meter.OnlyValue = checkOnlyValue.Checked;

                meter.Spinner = txtSpinnerImage.Text;
                meter.MinSpin = (int)numSpinMin.Value;
                meter.MaxSpin = (int)numSpinMax.Value;

                meter.Background = txtBackground.Text;
                meter.Foreground = txtForeground.Text;
                meter.Vector = comboProgressVector.Text;

                meter.GraphW = (int)numGraphW.Value;
                meter.GraphH = (int)numGraphH.Value;
                meter.GraphStepW = (int)numGraphStepW.Value;
                meter.GraphLineW = (int)numGraphLineW.Value;
                meter.GraphInterval = (int)numStepInterval.Value;
                meter.GraphLineColor = picGraphColor.BackColor;
                meter.GraphBorder = checkGraphBorder.Checked;
                meter.GraphTex = txtGraphTexture.Text;
                meter.GraphTexFront = rdbGraphTextureFront.Checked;

                meter.Clock24HrFormat = rdbClock24HourFormat.Checked;
                meter.ClockPlaySounds = checkClockPlaySounds.Checked;
                meter.ClockPlaySoundsOnStartup = checkClockPlaySoundsOnStartup.Checked;
                double tempDbl;
                if (double.TryParse(txtClockLatitude.Text, out tempDbl))
                    meter.ClockLatitude = tempDbl;
                if (double.TryParse(txtClockLongitude.Text, out tempDbl))
                    meter.ClockLongitude = tempDbl;
                int tempInt;
                if (int.TryParse(txtClockTimeZone.Text, out tempInt))
                    meter.ClockTimeZone = tempInt;

                //copy any new imgs
                checkIfNewImg(ref meter.Spinner);
                checkIfNewImg(ref meter.Background);
                checkIfNewImg(ref meter.Foreground);
                checkIfNewImg(ref meter.GraphTex);

                //update imgs & colors
                meter.LoadResources();

                //update meter name in list
                listMeters.Items[listMeters.SelectedIndex] = meter.Data;
            }

            saveMeters();
            InitData();

            buttMeterSaveChanges.Enabled = false;
        }

        private void buttLoadSpinnerImg_Click(object sender, EventArgs e)
        {
            showOpenDiag(txtSpinnerImage, picSpinner);
        }

        private void txtSpinnerImage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                checkImage(txtSpinnerImage, picSpinner);
        }

        private void buttLoadBackground_Click(object sender, EventArgs e)
        {
            showOpenDiag(txtBackground, picBackground);
        }

        private void buttLoadForeground_Click(object sender, EventArgs e)
        {
            showOpenDiag(txtForeground, picForeground);
        }

        private void buttAddImages_Click(object sender, EventArgs e)
        {
            openDialog.Multiselect = true;

            if (openDialog.ShowDialog() != System.Windows.Forms.DialogResult.Cancel)
            {
                if (meters[listMeters.SelectedIndex].ImgSeqDir == "" || !Directory.Exists(Application.StartupPath + "\\imgs\\" + meters[listMeters.SelectedIndex].ImgSeqDir))
                {
                    //create new dir
                    string name = Path.GetFileNameWithoutExtension(openDialog.FileNames[0]);
                    int i = 0;

                    if (Directory.Exists(Application.StartupPath + "\\imgs\\" + name))
                    {
                        while (Directory.Exists(Application.StartupPath + "\\imgs\\" + name + "_" + ++i)) ;

                        name += "_" + i;
                    }

                    Directory.CreateDirectory(Application.StartupPath + "\\imgs\\" + name);
                    meters[listMeters.SelectedIndex].ImgSeqDir = name;
                }

                //copy files
                foreach (string file in openDialog.FileNames)
                {
                    string dest = Application.StartupPath + "\\imgs\\" + meters[listMeters.SelectedIndex].ImgSeqDir + "\\" + Path.GetFileName(file);

                    if (!File.Exists(dest))
                        File.Copy(file, dest);
                }

                //update meter
                saveMeters();
                reloadImgSeq();
            }

            openDialog.Multiselect = false;
        }

        private void buttOpenImgSeqDir_Click(object sender, EventArgs e)
        {
            string dir = Application.StartupPath + "\\imgs\\" + meters[listMeters.SelectedIndex].ImgSeqDir;

            if (Directory.Exists(dir))
                Process.Start(dir);
        }

        private void buttDotaClockOpenResDir_Click(object sender, EventArgs e)
        {
            string dir = Application.StartupPath + "\\imgs\\dota_clock";

            if (Directory.Exists(dir))
                Process.Start(dir);
        }

        private void buttImgSeqReload_Click(object sender, EventArgs e)
        {
            reloadImgSeq();
        }

        private void buttGraphColorPick_Click(object sender, EventArgs e)
        {
            colorDialog.Color = picGraphColor.BackColor;
            colorDialog.ShowDialog();
            picGraphColor.BackColor = colorDialog.Color;

            checkForMeterChanges();
        }

        private void comboClickAction_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboClickAction.Text == "Launch program/file...")
            {
                if (openProgramOrFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    timerDelay.Tag = new Tuple<ComboBox, string>(comboClickAction, openProgramOrFile.FileName);
                    timerDelay.Enabled = true;
                }
                else
                    checkForMeterChanges();
            }
            else if (comboClickAction.Text == "Launch web page...")
            {
                timerDelay.Tag = new Tuple<ComboBox, string>(comboClickAction, Interaction.InputBox("Enter URL"));
                timerDelay.Enabled = true;
            }
            else
                checkForMeterChanges();
        }

        private void comboDragFileAction_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboDragFileAction.Text == "Copy to directory..." || comboDragFileAction.Text == "Move to directory...")
            {
                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    timerDelay.Tag = new Tuple<ComboBox, string>(comboDragFileAction, folderDialog.SelectedPath);
                    timerDelay.Enabled = true;
                }
                else
                    checkForMeterChanges();
            }
            else
                checkForMeterChanges();
        }

        private void timerDelay_Tick(object sender, EventArgs e)
        {
            timerDelay.Enabled = false;

            Tuple<ComboBox, string> arg = (Tuple<ComboBox, string>)timerDelay.Tag;
            arg.Item1.Text += arg.Item2;

            checkForMeterChanges();
        }

        private void buttChooseTextFile_Click(object sender, EventArgs e)
        {
            if (openTextFile.ShowDialog() != System.Windows.Forms.DialogResult.Cancel && File.Exists(openTextFile.FileName))
            {
                comboDataSubsource.Text = openTextFile.FileName;

                txtPrefix.Text = Path.GetFileNameWithoutExtension(openTextFile.FileName) + ": ";
                checkForMeterChanges();
            }
        }

        private void buttGraphTexturePick_Click(object sender, EventArgs e)
        {
            showOpenDiag(txtGraphTexture, picGraphTexture);
        }

        private void txtGraphTexture_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                checkImage(txtGraphTexture, picGraphTexture);
        }

        private void formSetup_Load(object sender, EventArgs e)
        {
            Tutorial tutorial = new Tutorial(Application.StartupPath + "\\tutorials\\setup.txt", this);
        }

        private void trackUpdate_Scroll(object sender, EventArgs e)
        {
            refreshUpdateNotifLabel();
            checkForGeneralOptionsChanges();
        }

        private void checkShowChangelog_CheckedChanged(object sender, EventArgs e)
        {
            checkForGeneralOptionsChanges();
        }

        private void buttGetData_Click(object sender, EventArgs e)
        {
            //geocoordinates
            try
            {
                lblWAStatus.Text = "Downloading geocoordinates...";
                this.Refresh();

                string page = dlPage("http://www.wolframalpha.com/input/?i=" + txtLocation.Text + "+coordinates");

                if (page == "error")
                    throw new Exception();

                int lb = page.LastIndexOf('"', page.IndexOf("&deg;")) + 1;
                int ub = page.IndexOf('"', lb);
                string[] coords = page.Substring(lb, ub - lb).Split(',');

                txtClockLatitude.Text = parseCoord(coords[0]).ToString();
                txtClockLongitude.Text = parseCoord(coords[1]).ToString();
            }
            catch
            {
                if (MessageBox.Show("Would you like to open Wolfram|Alpha webpage?", "Error while downloading geocoordinates.", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.Yes)
                    Process.Start("http://www.wolframalpha.com/input/?i=" + txtLocation.Text + "+coordinates");
            }

            try
            {
                //timezone
                lblWAStatus.Text = "Downloading time zone data...";
                this.Refresh();

                string page = dlPage("http://www.wolframalpha.com/input/?i=" + txtLocation.Text + "+timezone");

                if (page == "error")
                    throw new Exception();

                int lb = page.IndexOf("from UTC | ") + 11;
                if (lb == 10)
                    throw new Exception();

                int ub = page.IndexOf(" ", lb);
                if (lb == -1)
                    throw new Exception();

                txtClockTimeZone.Text = page.Substring(lb, ub - lb);

                //dst
                bool plusPrefix = txtClockTimeZone.Text[0] == '+';

                if (TimeZone.CurrentTimeZone.IsDaylightSavingTime(DateTime.Now))
                    txtClockTimeZone.Text = (plusPrefix ? "+" : "") + (int.Parse(txtClockTimeZone.Text) - 1).ToString();
            }
            catch
            {
                if (MessageBox.Show("Would you like to open Wolfram|Alpha webpage?", "Error while downloading timezone information.", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.Yes)
                    Process.Start("http://www.wolframalpha.com/input/?i=" + txtLocation.Text + "+timezone");
            }

            lblWAStatus.Text = "Enter your geocoordinates, or search by location:";
        }
    }
}
