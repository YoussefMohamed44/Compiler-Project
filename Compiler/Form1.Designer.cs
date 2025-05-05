using static System.Net.Mime.MediaTypeNames;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Compiler
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private Button button1;
        private TextBox textBox1;
        private ListBox listBox1;

        private void InitializeComponent()
        {
            button1 = new Button();
            textBox1 = new TextBox();
            listBox1 = new ListBox();
            SuspendLayout();
            // Button configuration
            this.button1 = new System.Windows.Forms.Button();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(516, 441);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(122, 60);
            this.button1.TabIndex = 1;
            this.button1.Text = "Scan and Parse";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click); // Critical line
            // 
            // textBox1
            // 
            textBox1.Location = new Point(2, 30);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(546, 347);
            textBox1.TabIndex = 4;
            // 
            // listBox1
            // 
            listBox1.FormattingEnabled = true;
            listBox1.ItemHeight = 15;
            listBox1.Location = new Point(592, 30);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(549, 349);
            listBox1.TabIndex = 6;
            // 
            // Form1
            // 
            ClientSize = new Size(1153, 669);
            Controls.Add(listBox1);
            Controls.Add(textBox1);
            Controls.Add(button1);
            Name = "Form1";
            Text = "Compiler";
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion
    }
}