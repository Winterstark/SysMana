namespace SysMana
{
    partial class formSysMana
    {
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
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(formSysMana));
            this.timerRefresh = new System.Windows.Forms.Timer(this.components);
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuSetup = new System.Windows.Forms.ToolStripMenuItem();
            this.menuOnTop = new System.Windows.Forms.ToolStripMenuItem();
            this.menuExit = new System.Windows.Forms.ToolStripMenuItem();
            this.tipInfo = new System.Windows.Forms.ToolTip(this.components);
            this.timerEnsureTopMost = new System.Windows.Forms.Timer(this.components);
            this.timerUpdateData = new System.Windows.Forms.Timer(this.components);
            this.menuAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.contextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // timerRefresh
            // 
            this.timerRefresh.Enabled = true;
            this.timerRefresh.Interval = 50;
            this.timerRefresh.Tick += new System.EventHandler(this.timerRefresh_Tick);
            // 
            // contextMenu
            // 
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuOnTop,
            this.toolStripSeparator2,
            this.menuSetup,
            this.menuAbout,
            this.toolStripSeparator1,
            this.menuExit});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(153, 126);
            // 
            // menuSetup
            // 
            this.menuSetup.Name = "menuSetup";
            this.menuSetup.Size = new System.Drawing.Size(152, 22);
            this.menuSetup.Text = "Setup";
            this.menuSetup.Click += new System.EventHandler(this.menuSetup_Click);
            // 
            // menuOnTop
            // 
            this.menuOnTop.Checked = true;
            this.menuOnTop.CheckState = System.Windows.Forms.CheckState.Checked;
            this.menuOnTop.Name = "menuOnTop";
            this.menuOnTop.Size = new System.Drawing.Size(152, 22);
            this.menuOnTop.Text = "On Top";
            this.menuOnTop.Click += new System.EventHandler(this.menuOnTop_Click);
            // 
            // menuExit
            // 
            this.menuExit.Name = "menuExit";
            this.menuExit.Size = new System.Drawing.Size(152, 22);
            this.menuExit.Text = "Exit";
            this.menuExit.Click += new System.EventHandler(this.menuExit_Click);
            // 
            // timerEnsureTopMost
            // 
            this.timerEnsureTopMost.Enabled = true;
            this.timerEnsureTopMost.Interval = 3000;
            this.timerEnsureTopMost.Tick += new System.EventHandler(this.timerEnsureTopMost_Tick);
            // 
            // timerUpdateData
            // 
            this.timerUpdateData.Enabled = true;
            this.timerUpdateData.Tick += new System.EventHandler(this.timerUpdateData_Tick);
            // 
            // menuAbout
            // 
            this.menuAbout.Name = "menuAbout";
            this.menuAbout.Size = new System.Drawing.Size(152, 22);
            this.menuAbout.Text = "About";
            this.menuAbout.Click += new System.EventHandler(this.menuAbout_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(149, 6);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(149, 6);
            // 
            // formSysMana
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(116, 71);
            this.ContextMenuStrip = this.contextMenu;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "formSysMana";
            this.ShowInTaskbar = false;
            this.Text = "SysMana";
            this.TopMost = true;
            this.Activated += new System.EventHandler(this.formSysMana_Activated);
            this.Load += new System.EventHandler(this.formSysMeters_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.formSysMana_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.formSysMana_DragEnter);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.formSysMeters_MouseDown);
            this.MouseEnter += new System.EventHandler(this.formSysMana_MouseEnter);
            this.MouseLeave += new System.EventHandler(this.formSysMana_MouseLeave);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.formSysMeters_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.formSysMeters_MouseUp);
            this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.formSysMeters_MouseWheel);
            this.contextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer timerRefresh;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem menuSetup;
        private System.Windows.Forms.ToolStripMenuItem menuExit;
        private System.Windows.Forms.ToolTip tipInfo;
        private System.Windows.Forms.Timer timerEnsureTopMost;
        private System.Windows.Forms.ToolStripMenuItem menuOnTop;
        private System.Windows.Forms.Timer timerUpdateData;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem menuAbout;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
    }
}

