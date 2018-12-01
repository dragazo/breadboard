namespace BreadBoard
{
    partial class SourceEditor
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
            this.SourceBox = new System.Windows.Forms.TextBox();
            this.SaveButton = new System.Windows.Forms.Button();
            this.DiscardButton = new System.Windows.Forms.Button();
            this.PositionLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // SourceBox
            // 
            this.SourceBox.AcceptsTab = true;
            this.SourceBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SourceBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SourceBox.HideSelection = false;
            this.SourceBox.Location = new System.Drawing.Point(12, 12);
            this.SourceBox.Multiline = true;
            this.SourceBox.Name = "SourceBox";
            this.SourceBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.SourceBox.Size = new System.Drawing.Size(704, 380);
            this.SourceBox.TabIndex = 0;
            this.SourceBox.WordWrap = false;
            // 
            // SaveButton
            // 
            this.SaveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.SaveButton.Location = new System.Drawing.Point(564, 398);
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.Size = new System.Drawing.Size(152, 34);
            this.SaveButton.TabIndex = 1;
            this.SaveButton.Text = "Save";
            this.SaveButton.UseVisualStyleBackColor = true;
            // 
            // DiscardButton
            // 
            this.DiscardButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.DiscardButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.DiscardButton.Location = new System.Drawing.Point(406, 398);
            this.DiscardButton.Name = "DiscardButton";
            this.DiscardButton.Size = new System.Drawing.Size(152, 34);
            this.DiscardButton.TabIndex = 2;
            this.DiscardButton.Text = "Discard";
            this.DiscardButton.UseVisualStyleBackColor = true;
            // 
            // PositionLabel
            // 
            this.PositionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.PositionLabel.AutoSize = true;
            this.PositionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PositionLabel.Location = new System.Drawing.Point(12, 407);
            this.PositionLabel.Name = "PositionLabel";
            this.PositionLabel.Size = new System.Drawing.Size(132, 25);
            this.PositionLabel.TabIndex = 3;
            this.PositionLabel.Text = "<POSITION>";
            // 
            // SourceEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(728, 444);
            this.Controls.Add(this.PositionLabel);
            this.Controls.Add(this.DiscardButton);
            this.Controls.Add(this.SaveButton);
            this.Controls.Add(this.SourceBox);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.865546F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimumSize = new System.Drawing.Size(355, 200);
            this.Name = "SourceEditor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.WindowsDefaultBounds;
            this.Text = "SourceEditor";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox SourceBox;
        private System.Windows.Forms.Button SaveButton;
        private System.Windows.Forms.Button DiscardButton;
        private System.Windows.Forms.Label PositionLabel;
    }
}