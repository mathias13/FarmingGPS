namespace FunctionTestApp
{
    partial class TestAppForm
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
            this.label3 = new System.Windows.Forms.Label();
            this.txtBoxPosActRef = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lblDistance = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.btnSetPoints = new System.Windows.Forms.Button();
            this.txtBoxActualPositionBaseline = new System.Windows.Forms.TextBox();
            this.chkBoxActualBaseline = new System.Windows.Forms.CheckBox();
            this.lblFixBaseline = new System.Windows.Forms.Label();
            this.lblFixEstimation = new System.Windows.Forms.Label();
            this.lblCorrectionDist = new System.Windows.Forms.Label();
            this.lblDistanceWest = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.lblDistanceEast = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lblDistanceToPoint = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.lblIAR = new System.Windows.Forms.Label();
            this.lblSats = new System.Windows.Forms.Label();
            this.txtBoxDebug = new System.Windows.Forms.RichTextBox();
            this.chkBoxActualBaselineNED = new System.Windows.Forms.CheckBox();
            this.txtBoxActualBaselineNED = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.chkBoxActualBaselineECEF = new System.Windows.Forms.CheckBox();
            this.txtBoxActualBaselineECEF = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.txtBoxActualPositionBaseline1 = new System.Windows.Forms.TextBox();
            this.btnResetDGNSS = new System.Windows.Forms.Button();
            this.btnResetIAR = new System.Windows.Forms.Button();
            this.txtBoxActualPosition1 = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.chkBoxActual = new System.Windows.Forms.CheckBox();
            this.txtBoxActualPosition = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.chkBoxDebug = new System.Windows.Forms.CheckBox();
            this.dataGridCpu = new System.Windows.Forms.DataGridView();
            this.Name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CPU = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StackFree = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblUARTACrcErr = new System.Windows.Forms.Label();
            this.lblUARTBCrcErr = new System.Windows.Forms.Label();
            this.lblUARTFTDICrcErr = new System.Windows.Forms.Label();
            this.lblUARTFTDIIoErr = new System.Windows.Forms.Label();
            this.lblUARTBIoErr = new System.Windows.Forms.Label();
            this.lblUARTAIoErr = new System.Windows.Forms.Label();
            this.lblUARTFTDITxBuff = new System.Windows.Forms.Label();
            this.lblUARTBTxBuff = new System.Windows.Forms.Label();
            this.lblUARTATxBuff = new System.Windows.Forms.Label();
            this.lblUARTFTDIRxBuff = new System.Windows.Forms.Label();
            this.lblUARTBRxBuff = new System.Windows.Forms.Label();
            this.lblUARTARxBuff = new System.Windows.Forms.Label();
            this.lblUARTFTDITxKbytes = new System.Windows.Forms.Label();
            this.lblUARTBTxKbytes = new System.Windows.Forms.Label();
            this.lblUARTATxKbytes = new System.Windows.Forms.Label();
            this.lblUARTFTDIRxKbytes = new System.Windows.Forms.Label();
            this.lblUARTBRxKbytes = new System.Windows.Forms.Label();
            this.lblUARTARxKbytes = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.lblObsLatency = new System.Windows.Forms.Label();
            this.lblObsLatencyAvg = new System.Windows.Forms.Label();
            this.lblObsLatencyMin = new System.Windows.Forms.Label();
            this.lblObsLatencyMax = new System.Windows.Forms.Label();
            this.chkBoxActivateNtrip = new System.Windows.Forms.CheckBox();
            this.label11 = new System.Windows.Forms.Label();
            this.lblFixPosition = new System.Windows.Forms.Label();
            this.dataGridViewBase = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SNR = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.dataGridViewRover = new System.Windows.Forms.DataGridView();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnReset = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridCpu)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewBase)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewRover)).BeginInit();
            this.SuspendLayout();
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(40, 7);
            this.label3.Margin = new System.Windows.Forms.Padding(3, 7, 3, 7);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(114, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "ActualPositionBaseline";
            // 
            // txtBoxPosActRef
            // 
            this.txtBoxPosActRef.Location = new System.Drawing.Point(43, 237);
            this.txtBoxPosActRef.Name = "txtBoxPosActRef";
            this.txtBoxPosActRef.Size = new System.Drawing.Size(248, 20);
            this.txtBoxPosActRef.TabIndex = 6;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(40, 214);
            this.label1.Margin = new System.Windows.Forms.Padding(3, 7, 3, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(90, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Actual Reference";
            // 
            // lblDistance
            // 
            this.lblDistance.AutoSize = true;
            this.lblDistance.Location = new System.Drawing.Point(352, 310);
            this.lblDistance.Margin = new System.Windows.Forms.Padding(3, 7, 3, 7);
            this.lblDistance.Name = "lblDistance";
            this.lblDistance.Size = new System.Drawing.Size(15, 13);
            this.lblDistance.TabIndex = 11;
            this.lblDistance.Text = "N";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(352, 283);
            this.label6.Margin = new System.Windows.Forms.Padding(3, 7, 3, 7);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(80, 13);
            this.label6.TabIndex = 10;
            this.label6.Text = "Distance to line";
            // 
            // btnSetPoints
            // 
            this.btnSetPoints.Location = new System.Drawing.Point(43, 263);
            this.btnSetPoints.Name = "btnSetPoints";
            this.btnSetPoints.Size = new System.Drawing.Size(75, 23);
            this.btnSetPoints.TabIndex = 16;
            this.btnSetPoints.Text = "Set";
            this.btnSetPoints.UseVisualStyleBackColor = true;
            this.btnSetPoints.Click += new System.EventHandler(this.btnSetPoints_Click);
            // 
            // txtBoxActualPositionBaseline
            // 
            this.txtBoxActualPositionBaseline.Location = new System.Drawing.Point(43, 30);
            this.txtBoxActualPositionBaseline.Name = "txtBoxActualPositionBaseline";
            this.txtBoxActualPositionBaseline.Size = new System.Drawing.Size(248, 20);
            this.txtBoxActualPositionBaseline.TabIndex = 17;
            // 
            // chkBoxActualBaseline
            // 
            this.chkBoxActualBaseline.AutoSize = true;
            this.chkBoxActualBaseline.Location = new System.Drawing.Point(22, 30);
            this.chkBoxActualBaseline.Name = "chkBoxActualBaseline";
            this.chkBoxActualBaseline.Size = new System.Drawing.Size(15, 14);
            this.chkBoxActualBaseline.TabIndex = 19;
            this.chkBoxActualBaseline.UseVisualStyleBackColor = true;
            // 
            // lblFixBaseline
            // 
            this.lblFixBaseline.AutoSize = true;
            this.lblFixBaseline.BackColor = System.Drawing.Color.Red;
            this.lblFixBaseline.Location = new System.Drawing.Point(160, 7);
            this.lblFixBaseline.Name = "lblFixBaseline";
            this.lblFixBaseline.Size = new System.Drawing.Size(23, 13);
            this.lblFixBaseline.TabIndex = 22;
            this.lblFixBaseline.Text = "Fix:";
            // 
            // lblFixEstimation
            // 
            this.lblFixEstimation.AutoSize = true;
            this.lblFixEstimation.BackColor = System.Drawing.Color.Transparent;
            this.lblFixEstimation.Location = new System.Drawing.Point(354, 138);
            this.lblFixEstimation.Name = "lblFixEstimation";
            this.lblFixEstimation.Size = new System.Drawing.Size(73, 13);
            this.lblFixEstimation.TabIndex = 23;
            this.lblFixEstimation.Text = "Fix estimation:";
            // 
            // lblCorrectionDist
            // 
            this.lblCorrectionDist.AutoSize = true;
            this.lblCorrectionDist.BackColor = System.Drawing.Color.Transparent;
            this.lblCorrectionDist.Location = new System.Drawing.Point(354, 30);
            this.lblCorrectionDist.Name = "lblCorrectionDist";
            this.lblCorrectionDist.Size = new System.Drawing.Size(0, 13);
            this.lblCorrectionDist.TabIndex = 24;
            // 
            // lblDistanceWest
            // 
            this.lblDistanceWest.AutoSize = true;
            this.lblDistanceWest.Location = new System.Drawing.Point(241, 310);
            this.lblDistanceWest.Margin = new System.Windows.Forms.Padding(3, 7, 3, 7);
            this.lblDistanceWest.Name = "lblDistanceWest";
            this.lblDistanceWest.Size = new System.Drawing.Size(15, 13);
            this.lblDistanceWest.TabIndex = 28;
            this.lblDistanceWest.Text = "N";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(241, 283);
            this.label4.Margin = new System.Windows.Forms.Padding(3, 7, 3, 7);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(105, 13);
            this.label4.TabIndex = 27;
            this.label4.Text = "Distance to line west";
            // 
            // lblDistanceEast
            // 
            this.lblDistanceEast.AutoSize = true;
            this.lblDistanceEast.Location = new System.Drawing.Point(437, 310);
            this.lblDistanceEast.Margin = new System.Windows.Forms.Padding(3, 7, 3, 7);
            this.lblDistanceEast.Name = "lblDistanceEast";
            this.lblDistanceEast.Size = new System.Drawing.Size(15, 13);
            this.lblDistanceEast.TabIndex = 30;
            this.lblDistanceEast.Text = "N";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(437, 283);
            this.label5.Margin = new System.Windows.Forms.Padding(3, 7, 3, 7);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(103, 13);
            this.label5.TabIndex = 29;
            this.label5.Text = "Distance to line east";
            // 
            // lblDistanceToPoint
            // 
            this.lblDistanceToPoint.AutoSize = true;
            this.lblDistanceToPoint.Location = new System.Drawing.Point(352, 241);
            this.lblDistanceToPoint.Margin = new System.Windows.Forms.Padding(3, 7, 3, 7);
            this.lblDistanceToPoint.Name = "lblDistanceToPoint";
            this.lblDistanceToPoint.Size = new System.Drawing.Size(15, 13);
            this.lblDistanceToPoint.TabIndex = 32;
            this.lblDistanceToPoint.Text = "N";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(352, 214);
            this.label7.Margin = new System.Windows.Forms.Padding(3, 7, 3, 7);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(87, 13);
            this.label7.TabIndex = 31;
            this.label7.Text = "Distance to point";
            // 
            // lblIAR
            // 
            this.lblIAR.AutoSize = true;
            this.lblIAR.BackColor = System.Drawing.Color.Transparent;
            this.lblIAR.Location = new System.Drawing.Point(354, 161);
            this.lblIAR.Name = "lblIAR";
            this.lblIAR.Size = new System.Drawing.Size(75, 13);
            this.lblIAR.TabIndex = 33;
            this.lblIAR.Text = "IAR hypotesis:";
            // 
            // lblSats
            // 
            this.lblSats.AutoSize = true;
            this.lblSats.BackColor = System.Drawing.Color.Transparent;
            this.lblSats.Location = new System.Drawing.Point(352, 183);
            this.lblSats.Name = "lblSats";
            this.lblSats.Size = new System.Drawing.Size(81, 13);
            this.lblSats.TabIndex = 34;
            this.lblSats.Text = "Number of sats:";
            // 
            // txtBoxDebug
            // 
            this.txtBoxDebug.DetectUrls = false;
            this.txtBoxDebug.Location = new System.Drawing.Point(32, 372);
            this.txtBoxDebug.Name = "txtBoxDebug";
            this.txtBoxDebug.ReadOnly = true;
            this.txtBoxDebug.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.txtBoxDebug.Size = new System.Drawing.Size(513, 290);
            this.txtBoxDebug.TabIndex = 35;
            this.txtBoxDebug.Text = "";
            // 
            // chkBoxActualBaselineNED
            // 
            this.chkBoxActualBaselineNED.AutoSize = true;
            this.chkBoxActualBaselineNED.Location = new System.Drawing.Point(22, 138);
            this.chkBoxActualBaselineNED.Name = "chkBoxActualBaselineNED";
            this.chkBoxActualBaselineNED.Size = new System.Drawing.Size(15, 14);
            this.chkBoxActualBaselineNED.TabIndex = 38;
            this.chkBoxActualBaselineNED.UseVisualStyleBackColor = true;
            // 
            // txtBoxActualBaselineNED
            // 
            this.txtBoxActualBaselineNED.Location = new System.Drawing.Point(43, 138);
            this.txtBoxActualBaselineNED.Name = "txtBoxActualBaselineNED";
            this.txtBoxActualBaselineNED.Size = new System.Drawing.Size(248, 20);
            this.txtBoxActualBaselineNED.TabIndex = 37;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(40, 115);
            this.label2.Margin = new System.Windows.Forms.Padding(3, 7, 3, 7);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(106, 13);
            this.label2.TabIndex = 36;
            this.label2.Text = "Actual Baseline NED";
            // 
            // chkBoxActualBaselineECEF
            // 
            this.chkBoxActualBaselineECEF.AutoSize = true;
            this.chkBoxActualBaselineECEF.Location = new System.Drawing.Point(22, 184);
            this.chkBoxActualBaselineECEF.Name = "chkBoxActualBaselineECEF";
            this.chkBoxActualBaselineECEF.Size = new System.Drawing.Size(15, 14);
            this.chkBoxActualBaselineECEF.TabIndex = 41;
            this.chkBoxActualBaselineECEF.UseVisualStyleBackColor = true;
            // 
            // txtBoxActualBaselineECEF
            // 
            this.txtBoxActualBaselineECEF.Location = new System.Drawing.Point(43, 184);
            this.txtBoxActualBaselineECEF.Name = "txtBoxActualBaselineECEF";
            this.txtBoxActualBaselineECEF.Size = new System.Drawing.Size(248, 20);
            this.txtBoxActualBaselineECEF.TabIndex = 40;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(40, 161);
            this.label8.Margin = new System.Windows.Forms.Padding(3, 7, 3, 7);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(110, 13);
            this.label8.TabIndex = 39;
            this.label8.Text = "Actual Baseline ECEF";
            // 
            // txtBoxActualPositionBaseline1
            // 
            this.txtBoxActualPositionBaseline1.Location = new System.Drawing.Point(305, 30);
            this.txtBoxActualPositionBaseline1.Name = "txtBoxActualPositionBaseline1";
            this.txtBoxActualPositionBaseline1.Size = new System.Drawing.Size(248, 20);
            this.txtBoxActualPositionBaseline1.TabIndex = 42;
            // 
            // btnResetDGNSS
            // 
            this.btnResetDGNSS.Location = new System.Drawing.Point(23, 343);
            this.btnResetDGNSS.Name = "btnResetDGNSS";
            this.btnResetDGNSS.Size = new System.Drawing.Size(124, 23);
            this.btnResetDGNSS.TabIndex = 43;
            this.btnResetDGNSS.Text = "Reset DGNSS";
            this.btnResetDGNSS.UseVisualStyleBackColor = true;
            this.btnResetDGNSS.Click += new System.EventHandler(this.btnResetDGNSS_Click);
            // 
            // btnResetIAR
            // 
            this.btnResetIAR.Location = new System.Drawing.Point(153, 343);
            this.btnResetIAR.Name = "btnResetIAR";
            this.btnResetIAR.Size = new System.Drawing.Size(124, 23);
            this.btnResetIAR.TabIndex = 44;
            this.btnResetIAR.Text = "Reset IAR";
            this.btnResetIAR.UseVisualStyleBackColor = true;
            this.btnResetIAR.Click += new System.EventHandler(this.btnResetIAR_Click);
            // 
            // txtBoxActualPosition1
            // 
            this.txtBoxActualPosition1.Location = new System.Drawing.Point(305, 85);
            this.txtBoxActualPosition1.Name = "txtBoxActualPosition1";
            this.txtBoxActualPosition1.Size = new System.Drawing.Size(248, 20);
            this.txtBoxActualPosition1.TabIndex = 50;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.BackColor = System.Drawing.Color.Transparent;
            this.label9.Location = new System.Drawing.Point(354, 85);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(0, 13);
            this.label9.TabIndex = 49;
            // 
            // chkBoxActual
            // 
            this.chkBoxActual.AutoSize = true;
            this.chkBoxActual.Location = new System.Drawing.Point(22, 85);
            this.chkBoxActual.Name = "chkBoxActual";
            this.chkBoxActual.Size = new System.Drawing.Size(15, 14);
            this.chkBoxActual.TabIndex = 48;
            this.chkBoxActual.UseVisualStyleBackColor = true;
            // 
            // txtBoxActualPosition
            // 
            this.txtBoxActualPosition.Location = new System.Drawing.Point(43, 85);
            this.txtBoxActualPosition.Name = "txtBoxActualPosition";
            this.txtBoxActualPosition.Size = new System.Drawing.Size(248, 20);
            this.txtBoxActualPosition.TabIndex = 47;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(40, 62);
            this.label10.Margin = new System.Windows.Forms.Padding(3, 7, 3, 7);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(74, 13);
            this.label10.TabIndex = 46;
            this.label10.Text = "ActualPosition";
            // 
            // chkBoxDebug
            // 
            this.chkBoxDebug.AutoSize = true;
            this.chkBoxDebug.Location = new System.Drawing.Point(11, 375);
            this.chkBoxDebug.Name = "chkBoxDebug";
            this.chkBoxDebug.Size = new System.Drawing.Size(15, 14);
            this.chkBoxDebug.TabIndex = 52;
            this.chkBoxDebug.UseVisualStyleBackColor = true;
            // 
            // dataGridCpu
            // 
            this.dataGridCpu.AllowUserToAddRows = false;
            this.dataGridCpu.AllowUserToDeleteRows = false;
            this.dataGridCpu.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dataGridCpu.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dataGridCpu.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridCpu.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Name,
            this.CPU,
            this.StackFree});
            this.dataGridCpu.Location = new System.Drawing.Point(585, 30);
            this.dataGridCpu.Name = "dataGridCpu";
            this.dataGridCpu.Size = new System.Drawing.Size(363, 144);
            this.dataGridCpu.TabIndex = 53;
            // 
            // Name
            // 
            this.Name.HeaderText = "Name";
            this.Name.Name = "Name";
            this.Name.ReadOnly = true;
            this.Name.Width = 60;
            // 
            // CPU
            // 
            this.CPU.HeaderText = "CPU %";
            this.CPU.Name = "CPU";
            this.CPU.ReadOnly = true;
            this.CPU.Width = 65;
            // 
            // StackFree
            // 
            this.StackFree.HeaderText = "StackFree";
            this.StackFree.Name = "StackFree";
            this.StackFree.ReadOnly = true;
            this.StackFree.Width = 81;
            // 
            // lblUARTACrcErr
            // 
            this.lblUARTACrcErr.AutoSize = true;
            this.lblUARTACrcErr.BackColor = System.Drawing.Color.Transparent;
            this.lblUARTACrcErr.Location = new System.Drawing.Point(13, 16);
            this.lblUARTACrcErr.Name = "lblUARTACrcErr";
            this.lblUARTACrcErr.Size = new System.Drawing.Size(62, 13);
            this.lblUARTACrcErr.TabIndex = 54;
            this.lblUARTACrcErr.Text = "CRC Errors:";
            // 
            // lblUARTBCrcErr
            // 
            this.lblUARTBCrcErr.AutoSize = true;
            this.lblUARTBCrcErr.BackColor = System.Drawing.Color.Transparent;
            this.lblUARTBCrcErr.Location = new System.Drawing.Point(14, 16);
            this.lblUARTBCrcErr.Name = "lblUARTBCrcErr";
            this.lblUARTBCrcErr.Size = new System.Drawing.Size(62, 13);
            this.lblUARTBCrcErr.TabIndex = 55;
            this.lblUARTBCrcErr.Text = "CRC Errors:";
            // 
            // lblUARTFTDICrcErr
            // 
            this.lblUARTFTDICrcErr.AutoSize = true;
            this.lblUARTFTDICrcErr.BackColor = System.Drawing.Color.Transparent;
            this.lblUARTFTDICrcErr.Location = new System.Drawing.Point(13, 16);
            this.lblUARTFTDICrcErr.Name = "lblUARTFTDICrcErr";
            this.lblUARTFTDICrcErr.Size = new System.Drawing.Size(62, 13);
            this.lblUARTFTDICrcErr.TabIndex = 56;
            this.lblUARTFTDICrcErr.Text = "CRC Errors:";
            // 
            // lblUARTFTDIIoErr
            // 
            this.lblUARTFTDIIoErr.AutoSize = true;
            this.lblUARTFTDIIoErr.BackColor = System.Drawing.Color.Transparent;
            this.lblUARTFTDIIoErr.Location = new System.Drawing.Point(24, 29);
            this.lblUARTFTDIIoErr.Name = "lblUARTFTDIIoErr";
            this.lblUARTFTDIIoErr.Size = new System.Drawing.Size(51, 13);
            this.lblUARTFTDIIoErr.TabIndex = 59;
            this.lblUARTFTDIIoErr.Text = "IO Errors:";
            // 
            // lblUARTBIoErr
            // 
            this.lblUARTBIoErr.AutoSize = true;
            this.lblUARTBIoErr.BackColor = System.Drawing.Color.Transparent;
            this.lblUARTBIoErr.Location = new System.Drawing.Point(25, 29);
            this.lblUARTBIoErr.Name = "lblUARTBIoErr";
            this.lblUARTBIoErr.Size = new System.Drawing.Size(51, 13);
            this.lblUARTBIoErr.TabIndex = 58;
            this.lblUARTBIoErr.Text = "IO Errors:";
            // 
            // lblUARTAIoErr
            // 
            this.lblUARTAIoErr.AutoSize = true;
            this.lblUARTAIoErr.BackColor = System.Drawing.Color.Transparent;
            this.lblUARTAIoErr.Location = new System.Drawing.Point(24, 29);
            this.lblUARTAIoErr.Name = "lblUARTAIoErr";
            this.lblUARTAIoErr.Size = new System.Drawing.Size(51, 13);
            this.lblUARTAIoErr.TabIndex = 57;
            this.lblUARTAIoErr.Text = "IO Errors:";
            // 
            // lblUARTFTDITxBuff
            // 
            this.lblUARTFTDITxBuff.AutoSize = true;
            this.lblUARTFTDITxBuff.BackColor = System.Drawing.Color.Transparent;
            this.lblUARTFTDITxBuff.Location = new System.Drawing.Point(9, 42);
            this.lblUARTFTDITxBuff.Name = "lblUARTFTDITxBuff";
            this.lblUARTFTDITxBuff.Size = new System.Drawing.Size(66, 13);
            this.lblUARTFTDITxBuff.TabIndex = 62;
            this.lblUARTFTDITxBuff.Text = "TX Buffer %:";
            // 
            // lblUARTBTxBuff
            // 
            this.lblUARTBTxBuff.AutoSize = true;
            this.lblUARTBTxBuff.BackColor = System.Drawing.Color.Transparent;
            this.lblUARTBTxBuff.Location = new System.Drawing.Point(10, 42);
            this.lblUARTBTxBuff.Name = "lblUARTBTxBuff";
            this.lblUARTBTxBuff.Size = new System.Drawing.Size(66, 13);
            this.lblUARTBTxBuff.TabIndex = 61;
            this.lblUARTBTxBuff.Text = "TX Buffer %:";
            // 
            // lblUARTATxBuff
            // 
            this.lblUARTATxBuff.AutoSize = true;
            this.lblUARTATxBuff.BackColor = System.Drawing.Color.Transparent;
            this.lblUARTATxBuff.Location = new System.Drawing.Point(9, 42);
            this.lblUARTATxBuff.Name = "lblUARTATxBuff";
            this.lblUARTATxBuff.Size = new System.Drawing.Size(66, 13);
            this.lblUARTATxBuff.TabIndex = 60;
            this.lblUARTATxBuff.Text = "TX Buffer %:";
            // 
            // lblUARTFTDIRxBuff
            // 
            this.lblUARTFTDIRxBuff.AutoSize = true;
            this.lblUARTFTDIRxBuff.BackColor = System.Drawing.Color.Transparent;
            this.lblUARTFTDIRxBuff.Location = new System.Drawing.Point(8, 55);
            this.lblUARTFTDIRxBuff.Name = "lblUARTFTDIRxBuff";
            this.lblUARTFTDIRxBuff.Size = new System.Drawing.Size(67, 13);
            this.lblUARTFTDIRxBuff.TabIndex = 66;
            this.lblUARTFTDIRxBuff.Text = "RX Buffer %:";
            // 
            // lblUARTBRxBuff
            // 
            this.lblUARTBRxBuff.AutoSize = true;
            this.lblUARTBRxBuff.BackColor = System.Drawing.Color.Transparent;
            this.lblUARTBRxBuff.Location = new System.Drawing.Point(9, 55);
            this.lblUARTBRxBuff.Name = "lblUARTBRxBuff";
            this.lblUARTBRxBuff.Size = new System.Drawing.Size(67, 13);
            this.lblUARTBRxBuff.TabIndex = 65;
            this.lblUARTBRxBuff.Text = "RX Buffer %:";
            // 
            // lblUARTARxBuff
            // 
            this.lblUARTARxBuff.AutoSize = true;
            this.lblUARTARxBuff.BackColor = System.Drawing.Color.Transparent;
            this.lblUARTARxBuff.Location = new System.Drawing.Point(8, 55);
            this.lblUARTARxBuff.Name = "lblUARTARxBuff";
            this.lblUARTARxBuff.Size = new System.Drawing.Size(67, 13);
            this.lblUARTARxBuff.TabIndex = 64;
            this.lblUARTARxBuff.Text = "RX Buffer %:";
            // 
            // lblUARTFTDITxKbytes
            // 
            this.lblUARTFTDITxKbytes.AutoSize = true;
            this.lblUARTFTDITxKbytes.BackColor = System.Drawing.Color.Transparent;
            this.lblUARTFTDITxKbytes.Location = new System.Drawing.Point(5, 68);
            this.lblUARTFTDITxKbytes.Name = "lblUARTFTDITxKbytes";
            this.lblUARTFTDITxKbytes.Size = new System.Drawing.Size(70, 13);
            this.lblUARTFTDITxKbytes.TabIndex = 69;
            this.lblUARTFTDITxKbytes.Text = "TX KBytes/s:";
            // 
            // lblUARTBTxKbytes
            // 
            this.lblUARTBTxKbytes.AutoSize = true;
            this.lblUARTBTxKbytes.BackColor = System.Drawing.Color.Transparent;
            this.lblUARTBTxKbytes.Location = new System.Drawing.Point(6, 68);
            this.lblUARTBTxKbytes.Name = "lblUARTBTxKbytes";
            this.lblUARTBTxKbytes.Size = new System.Drawing.Size(70, 13);
            this.lblUARTBTxKbytes.TabIndex = 68;
            this.lblUARTBTxKbytes.Text = "TX KBytes/s:";
            // 
            // lblUARTATxKbytes
            // 
            this.lblUARTATxKbytes.AutoSize = true;
            this.lblUARTATxKbytes.BackColor = System.Drawing.Color.Transparent;
            this.lblUARTATxKbytes.Location = new System.Drawing.Point(5, 68);
            this.lblUARTATxKbytes.Name = "lblUARTATxKbytes";
            this.lblUARTATxKbytes.Size = new System.Drawing.Size(70, 13);
            this.lblUARTATxKbytes.TabIndex = 67;
            this.lblUARTATxKbytes.Text = "TX KBytes/s:";
            // 
            // lblUARTFTDIRxKbytes
            // 
            this.lblUARTFTDIRxKbytes.AutoSize = true;
            this.lblUARTFTDIRxKbytes.BackColor = System.Drawing.Color.Transparent;
            this.lblUARTFTDIRxKbytes.Location = new System.Drawing.Point(4, 81);
            this.lblUARTFTDIRxKbytes.Name = "lblUARTFTDIRxKbytes";
            this.lblUARTFTDIRxKbytes.Size = new System.Drawing.Size(71, 13);
            this.lblUARTFTDIRxKbytes.TabIndex = 72;
            this.lblUARTFTDIRxKbytes.Text = "RX KBytes/s:";
            // 
            // lblUARTBRxKbytes
            // 
            this.lblUARTBRxKbytes.AutoSize = true;
            this.lblUARTBRxKbytes.BackColor = System.Drawing.Color.Transparent;
            this.lblUARTBRxKbytes.Location = new System.Drawing.Point(5, 81);
            this.lblUARTBRxKbytes.Name = "lblUARTBRxKbytes";
            this.lblUARTBRxKbytes.Size = new System.Drawing.Size(71, 13);
            this.lblUARTBRxKbytes.TabIndex = 71;
            this.lblUARTBRxKbytes.Text = "RX KBytes/s:";
            // 
            // lblUARTARxKbytes
            // 
            this.lblUARTARxKbytes.AutoSize = true;
            this.lblUARTARxKbytes.BackColor = System.Drawing.Color.Transparent;
            this.lblUARTARxKbytes.Location = new System.Drawing.Point(4, 81);
            this.lblUARTARxKbytes.Name = "lblUARTARxKbytes";
            this.lblUARTARxKbytes.Size = new System.Drawing.Size(71, 13);
            this.lblUARTARxKbytes.TabIndex = 70;
            this.lblUARTARxKbytes.Text = "RX KBytes/s:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lblUARTACrcErr);
            this.groupBox1.Controls.Add(this.lblUARTAIoErr);
            this.groupBox1.Controls.Add(this.lblUARTARxKbytes);
            this.groupBox1.Controls.Add(this.lblUARTATxBuff);
            this.groupBox1.Controls.Add(this.lblUARTARxBuff);
            this.groupBox1.Controls.Add(this.lblUARTATxKbytes);
            this.groupBox1.Location = new System.Drawing.Point(585, 186);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(117, 100);
            this.groupBox1.TabIndex = 73;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "UARTA";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.lblUARTBCrcErr);
            this.groupBox2.Controls.Add(this.lblUARTBIoErr);
            this.groupBox2.Controls.Add(this.lblUARTBTxBuff);
            this.groupBox2.Controls.Add(this.lblUARTBRxBuff);
            this.groupBox2.Controls.Add(this.lblUARTBTxKbytes);
            this.groupBox2.Controls.Add(this.lblUARTBRxKbytes);
            this.groupBox2.Location = new System.Drawing.Point(708, 186);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(117, 100);
            this.groupBox2.TabIndex = 74;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "UARTB";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.lblUARTFTDICrcErr);
            this.groupBox3.Controls.Add(this.lblUARTFTDIIoErr);
            this.groupBox3.Controls.Add(this.lblUARTFTDITxBuff);
            this.groupBox3.Controls.Add(this.lblUARTFTDIRxBuff);
            this.groupBox3.Controls.Add(this.lblUARTFTDITxKbytes);
            this.groupBox3.Controls.Add(this.lblUARTFTDIRxKbytes);
            this.groupBox3.Location = new System.Drawing.Point(831, 186);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(117, 100);
            this.groupBox3.TabIndex = 74;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "FTDI";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.lblObsLatency);
            this.groupBox4.Controls.Add(this.lblObsLatencyAvg);
            this.groupBox4.Controls.Add(this.lblObsLatencyMin);
            this.groupBox4.Controls.Add(this.lblObsLatencyMax);
            this.groupBox4.Location = new System.Drawing.Point(585, 305);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(189, 80);
            this.groupBox4.TabIndex = 74;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Connection monitor";
            // 
            // lblObsLatency
            // 
            this.lblObsLatency.AutoSize = true;
            this.lblObsLatency.BackColor = System.Drawing.Color.Transparent;
            this.lblObsLatency.Location = new System.Drawing.Point(58, 16);
            this.lblObsLatency.Name = "lblObsLatency";
            this.lblObsLatency.Size = new System.Drawing.Size(73, 13);
            this.lblObsLatency.TabIndex = 54;
            this.lblObsLatency.Text = "OBS Latency:";
            // 
            // lblObsLatencyAvg
            // 
            this.lblObsLatencyAvg.AutoSize = true;
            this.lblObsLatencyAvg.BackColor = System.Drawing.Color.Transparent;
            this.lblObsLatencyAvg.Location = new System.Drawing.Point(14, 29);
            this.lblObsLatencyAvg.Name = "lblObsLatencyAvg";
            this.lblObsLatencyAvg.Size = new System.Drawing.Size(117, 13);
            this.lblObsLatencyAvg.TabIndex = 57;
            this.lblObsLatencyAvg.Text = "OBS Latency (Avg ms):";
            // 
            // lblObsLatencyMin
            // 
            this.lblObsLatencyMin.AutoSize = true;
            this.lblObsLatencyMin.BackColor = System.Drawing.Color.Transparent;
            this.lblObsLatencyMin.Location = new System.Drawing.Point(16, 42);
            this.lblObsLatencyMin.Name = "lblObsLatencyMin";
            this.lblObsLatencyMin.Size = new System.Drawing.Size(115, 13);
            this.lblObsLatencyMin.TabIndex = 60;
            this.lblObsLatencyMin.Text = "OBS Latency (Min ms):";
            // 
            // lblObsLatencyMax
            // 
            this.lblObsLatencyMax.AutoSize = true;
            this.lblObsLatencyMax.BackColor = System.Drawing.Color.Transparent;
            this.lblObsLatencyMax.Location = new System.Drawing.Point(13, 55);
            this.lblObsLatencyMax.Name = "lblObsLatencyMax";
            this.lblObsLatencyMax.Size = new System.Drawing.Size(118, 13);
            this.lblObsLatencyMax.TabIndex = 64;
            this.lblObsLatencyMax.Text = "OBS Latency (Max ms):";
            // 
            // chkBoxActivateNtrip
            // 
            this.chkBoxActivateNtrip.AutoSize = true;
            this.chkBoxActivateNtrip.Location = new System.Drawing.Point(22, 296);
            this.chkBoxActivateNtrip.Name = "chkBoxActivateNtrip";
            this.chkBoxActivateNtrip.Size = new System.Drawing.Size(15, 14);
            this.chkBoxActivateNtrip.TabIndex = 76;
            this.chkBoxActivateNtrip.UseVisualStyleBackColor = true;
            this.chkBoxActivateNtrip.CheckedChanged += new System.EventHandler(this.chkBoxActivateNtrip_CheckedChanged);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(40, 296);
            this.label11.Margin = new System.Windows.Forms.Padding(3, 7, 3, 7);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(82, 13);
            this.label11.TabIndex = 75;
            this.label11.Text = "Activate NTRIP";
            // 
            // lblFixPosition
            // 
            this.lblFixPosition.AutoSize = true;
            this.lblFixPosition.BackColor = System.Drawing.Color.Red;
            this.lblFixPosition.Location = new System.Drawing.Point(160, 62);
            this.lblFixPosition.Name = "lblFixPosition";
            this.lblFixPosition.Size = new System.Drawing.Size(23, 13);
            this.lblFixPosition.TabIndex = 77;
            this.lblFixPosition.Text = "Fix:";
            // 
            // dataGridViewBase
            // 
            this.dataGridViewBase.AllowUserToAddRows = false;
            this.dataGridViewBase.AllowUserToDeleteRows = false;
            this.dataGridViewBase.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dataGridViewBase.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dataGridViewBase.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewBase.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1,
            this.SNR});
            this.dataGridViewBase.Location = new System.Drawing.Point(585, 418);
            this.dataGridViewBase.Name = "dataGridViewBase";
            this.dataGridViewBase.Size = new System.Drawing.Size(170, 241);
            this.dataGridViewBase.TabIndex = 78;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.HeaderText = "PRN";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 55;
            // 
            // SNR
            // 
            this.SNR.HeaderText = "SNR";
            this.SNR.Name = "SNR";
            this.SNR.ReadOnly = true;
            this.SNR.Width = 55;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(582, 395);
            this.label12.Margin = new System.Windows.Forms.Padding(3, 7, 3, 7);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(31, 13);
            this.label12.TabIndex = 79;
            this.label12.Text = "Base";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(769, 395);
            this.label13.Margin = new System.Windows.Forms.Padding(3, 7, 3, 7);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(36, 13);
            this.label13.TabIndex = 81;
            this.label13.Text = "Rover";
            // 
            // dataGridViewRover
            // 
            this.dataGridViewRover.AllowUserToAddRows = false;
            this.dataGridViewRover.AllowUserToDeleteRows = false;
            this.dataGridViewRover.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dataGridViewRover.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dataGridViewRover.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewRover.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn3});
            this.dataGridViewRover.Location = new System.Drawing.Point(772, 418);
            this.dataGridViewRover.Name = "dataGridViewRover";
            this.dataGridViewRover.Size = new System.Drawing.Size(176, 241);
            this.dataGridViewRover.TabIndex = 80;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.HeaderText = "PRN";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 55;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.HeaderText = "SNR";
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 55;
            // 
            // btnReset
            // 
            this.btnReset.Location = new System.Drawing.Point(283, 342);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(124, 23);
            this.btnReset.TabIndex = 85;
            this.btnReset.Text = "Reset";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // TestAppForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(967, 671);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.dataGridViewRover);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.dataGridViewBase);
            this.Controls.Add(this.lblFixPosition);
            this.Controls.Add(this.chkBoxActivateNtrip);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.dataGridCpu);
            this.Controls.Add(this.chkBoxDebug);
            this.Controls.Add(this.txtBoxActualPosition1);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.chkBoxActual);
            this.Controls.Add(this.txtBoxActualPosition);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.btnResetIAR);
            this.Controls.Add(this.btnResetDGNSS);
            this.Controls.Add(this.txtBoxActualPositionBaseline1);
            this.Controls.Add(this.chkBoxActualBaselineECEF);
            this.Controls.Add(this.txtBoxActualBaselineECEF);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.chkBoxActualBaselineNED);
            this.Controls.Add(this.txtBoxActualBaselineNED);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtBoxDebug);
            this.Controls.Add(this.lblSats);
            this.Controls.Add(this.lblIAR);
            this.Controls.Add(this.lblDistanceToPoint);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.lblDistanceEast);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lblDistanceWest);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.lblCorrectionDist);
            this.Controls.Add(this.lblFixEstimation);
            this.Controls.Add(this.lblFixBaseline);
            this.Controls.Add(this.chkBoxActualBaseline);
            this.Controls.Add(this.txtBoxActualPositionBaseline);
            this.Controls.Add(this.btnSetPoints);
            this.Controls.Add(this.lblDistance);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtBoxPosActRef);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.groupBox1);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridCpu)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewBase)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewRover)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtBoxPosActRef;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblDistance;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnSetPoints;
        private System.Windows.Forms.TextBox txtBoxActualPositionBaseline;
        private System.Windows.Forms.CheckBox chkBoxActualBaseline;
        private System.Windows.Forms.Label lblFixBaseline;
        private System.Windows.Forms.Label lblFixEstimation;
        private System.Windows.Forms.Label lblCorrectionDist;
        private System.Windows.Forms.Label lblDistanceWest;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label lblDistanceEast;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblDistanceToPoint;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label lblIAR;
        private System.Windows.Forms.Label lblSats;
        private System.Windows.Forms.RichTextBox txtBoxDebug;
        private System.Windows.Forms.CheckBox chkBoxActualBaselineNED;
        private System.Windows.Forms.TextBox txtBoxActualBaselineNED;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox chkBoxActualBaselineECEF;
        private System.Windows.Forms.TextBox txtBoxActualBaselineECEF;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtBoxActualPositionBaseline1;
        private System.Windows.Forms.Button btnResetDGNSS;
        private System.Windows.Forms.Button btnResetIAR;
        private System.Windows.Forms.TextBox txtBoxActualPosition1;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.CheckBox chkBoxActual;
        private System.Windows.Forms.TextBox txtBoxActualPosition;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.CheckBox chkBoxDebug;
        private System.Windows.Forms.DataGridView dataGridCpu;
        private System.Windows.Forms.DataGridViewTextBoxColumn Name;
        private System.Windows.Forms.DataGridViewTextBoxColumn CPU;
        private System.Windows.Forms.DataGridViewTextBoxColumn StackFree;
        private System.Windows.Forms.Label lblUARTACrcErr;
        private System.Windows.Forms.Label lblUARTBCrcErr;
        private System.Windows.Forms.Label lblUARTFTDICrcErr;
        private System.Windows.Forms.Label lblUARTFTDIIoErr;
        private System.Windows.Forms.Label lblUARTBIoErr;
        private System.Windows.Forms.Label lblUARTAIoErr;
        private System.Windows.Forms.Label lblUARTFTDITxBuff;
        private System.Windows.Forms.Label lblUARTBTxBuff;
        private System.Windows.Forms.Label lblUARTATxBuff;
        private System.Windows.Forms.Label lblUARTFTDIRxBuff;
        private System.Windows.Forms.Label lblUARTBRxBuff;
        private System.Windows.Forms.Label lblUARTARxBuff;
        private System.Windows.Forms.Label lblUARTFTDITxKbytes;
        private System.Windows.Forms.Label lblUARTBTxKbytes;
        private System.Windows.Forms.Label lblUARTATxKbytes;
        private System.Windows.Forms.Label lblUARTFTDIRxKbytes;
        private System.Windows.Forms.Label lblUARTBRxKbytes;
        private System.Windows.Forms.Label lblUARTARxKbytes;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label lblObsLatency;
        private System.Windows.Forms.Label lblObsLatencyAvg;
        private System.Windows.Forms.Label lblObsLatencyMin;
        private System.Windows.Forms.Label lblObsLatencyMax;
        private System.Windows.Forms.CheckBox chkBoxActivateNtrip;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label lblFixPosition;
        private System.Windows.Forms.DataGridView dataGridViewBase;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.DataGridView dataGridViewRover;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn SNR;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.Button btnReset;
    }
}

