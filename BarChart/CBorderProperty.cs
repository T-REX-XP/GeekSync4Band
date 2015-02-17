using System.ComponentModel;
using System.Drawing;

namespace BarChart
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class CBorderProperty
    {
        #region Fields

        private int nSize;

        private Color color;

        private bool bVisible;

        private Pen pen;

        private RectangleF rectBound;

        #endregion

        #region Properties

        [Browsable(true)]
        public bool Visible
        {
            get { return bVisible; }
            set { bVisible = value; }
        }
        
        [Browsable(true)]
        public Color Color
        {
            get { return color; }
            set 
            { 
                color = value;
                ResetPen();
            }
        }

        [Browsable(true)]
        public int Width
        {
            get { return nSize; }
            set 
            {
                nSize = value;
                ResetPen();
            }
        }
       
        [Browsable(false)]
        public RectangleF BoundRect
        {
            get { return rectBound; }
            set 
            { 
                rectBound = value;
            }
        }

        #endregion

        // Constructor
        public CBorderProperty()
        {
            pen = null;
            BoundRect = new RectangleF(0, 0, 0, 0);
            Visible = true;
            Color = Color.White;
            Width = 1;
        }

        #region Methods

        // Recreates BK brush
        private void ResetPen()
        {
            // Delete last brush
            if (pen != null)
            {
                pen.Dispose();
                pen = null;
            }

            if (nSize <= 0) return;

            pen = new Pen( color, nSize );
        }

        // Draws background inside visible rectangle of the given graphics
        public void Draw(Graphics gr)
        {
            if (rectBound == null || rectBound == RectangleF.Empty ) rectBound = gr.VisibleClipBounds;

            if (!bVisible) return;

            if (pen == null) ResetPen();
            if (pen == null) return;

            gr.DrawRectangle(pen, rectBound.X+Width/2, rectBound.Y+Width/2, rectBound.Width-Width, rectBound.Height-Width);
        }

        #endregion

        internal void SetRect(Rectangle rect)
        {
            rectBound.X = rect.X;
            rectBound.Y = rect.Y;
            rectBound.Width = rect.Width;
            rectBound.Height = rect.Height;
        }
    }
}