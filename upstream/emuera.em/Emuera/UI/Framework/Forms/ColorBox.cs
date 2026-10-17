using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

namespace MinorShift.Emuera.Forms
{
	public partial class ColorBox : UserControl
	{
		public ColorBox()
		{
			InitializeComponent();
		}
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Color SelectingColor
		{
			get { return pictureBox1.BackColor; }
			set { pictureBox1.BackColor = value; }
		}
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string ButtonText
		{
			get { return button.Text; }
			set { button.Text = value; }
		}

		private void button_Click(object sender, EventArgs e)
		{
			colorDialog.Color = pictureBox1.BackColor;
			if (colorDialog.ShowDialog() == DialogResult.OK)
				pictureBox1.BackColor = colorDialog.Color;
		}
	}
}
