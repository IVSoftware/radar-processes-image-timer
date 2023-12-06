namespace radar
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblNextTimeDownload = new Label();
            SuspendLayout();
            // 
            // labelCountdown
            // 
            lblNextTimeDownload.BackColor = Color.Azure;
            lblNextTimeDownload.Dock = DockStyle.Top;
            lblNextTimeDownload.Font = new Font("Segoe UI Light", 16F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblNextTimeDownload.Location = new Point(10, 10);
            lblNextTimeDownload.Name = "lblNextTimeDownload";
            lblNextTimeDownload.Size = new Size(458, 70);
            lblNextTimeDownload.TabIndex = 0;
            lblNextTimeDownload.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(478, 244);
            Controls.Add(lblNextTimeDownload);
            Name = "MainForm";
            Padding = new Padding(10);
            Text = "Main Form";
            ResumeLayout(false);
        }

        #endregion

        private Label lblNextTimeDownload;
    }
}
