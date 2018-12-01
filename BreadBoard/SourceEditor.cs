using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BreadBoard
{
    public partial class SourceEditor : Form
    {
        public string Source
        {
            get { return SourceBox.Text; }
            set { SourceBox.Text = value; }
        }

        public SourceEditor()
        {
            InitializeComponent();

            SourceBox.Font = new Font(FontFamily.GenericMonospace, SourceBox.Font.Size);

            Monitor();
        }

        private const int MonitorSleepTime = 50;
        private async void Monitor()
        {
            int lastPosition = -1;

            while (true)
            {
                int position = SourceBox.SelectionStart;
                if (position != lastPosition)
                {
                    lastPosition = position;

                    PositionLabel.Text = string.Format("Line {0}", SourceBox.GetLineFromCharIndex(position) + 1);
                }

                await Task.Delay(MonitorSleepTime);
            }
        }
    }
}
