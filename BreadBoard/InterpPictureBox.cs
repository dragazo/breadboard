using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace BreadBoard
{
    class InterpPictureBox : PictureBox
    {
        private InterpolationMode _InterpolationMode = InterpolationMode.Default;
        public InterpolationMode InterpolationMode
        {
            get { return _InterpolationMode; }
            set { _InterpolationMode = value; Invalidate(); }
        }

        public InterpPictureBox() : base() { }

        protected override void OnPaint(PaintEventArgs pe)
        {
            pe.Graphics.InterpolationMode = InterpolationMode;
            base.OnPaint(pe);
        }
    }
}
