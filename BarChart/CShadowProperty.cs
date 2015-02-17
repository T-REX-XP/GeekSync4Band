using System.ComponentModel;
using System.Drawing;

namespace BarChart
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class CShadowProperty
    {
        // Drawing modes of the shadow
        public enum Modes{None, Inner, Outer, Both}
       
        #region Fields

        private int nSizeOuter;
        private int nSizeInner;

        private Color colorOuter;
        private Color colorInner;

        private Pen pen;
        private Pen penBack;

        private Modes mode;

        private RectangleF rectOuter;
        private RectangleF rectInner;

        #endregion

        #region Properties

        
        [Browsable(true)]
        public Color ColorOuter
        {
            get { return colorOuter; }
            set { colorOuter = value;}
        }

        [Browsable(true)]
        public Color ColorInner
        {
            get { return colorInner; }
            set { colorInner = value; }
        }

        [Browsable(true)]
        public int WidthOuter
        {
            get { return nSizeOuter; }
            set { nSizeOuter = value; }
        }
       
        [Browsable(true)]
        public int WidthInner
        {
            get { return nSizeInner; }
            set { nSizeInner = value; }
        }
       
        [Browsable(true)]
        public Modes Mode
        {
            get { return mode; }
            set { mode = value; }
        }
       
        #endregion

        // Constructor
        public CShadowProperty()
        {
            pen = null;
            
            colorInner = Color.FromArgb(100, 0, 0, 0);
            colorOuter = Color.FromArgb(100, 0, 0, 0);
            
            nSizeInner = 5;
            nSizeOuter = 5;
            
            rectInner = RectangleF.Empty;
            rectOuter = RectangleF.Empty;

            mode = Modes.Inner;
        }

        #region Methods
 
        public void SetRect(RectangleF rect, int nIndex/* 0 = inner, 1 = outer*/)
        {
            SetRect(
                rect.X,
                rect.Y,
                rect.Width,
                rect.Height,
                nIndex);
        }

        public void SetRect(float x, float y, float width, float height, int nIndex/* 0 = inner, 1 = outer*/)
        {
            if (nIndex == 0)
            {
                rectInner.X = x;
                rectInner.Y = y;
                rectInner.Width = width;
                rectInner.Height = height;
            }
            else
            {
                rectOuter.X = x;
                rectOuter.Y = y;
                rectOuter.Width = width;
                rectOuter.Height = height;
            }
        }

        public void Draw(Graphics gr, Color colorBK)
        {
            if (mode == Modes.None) return;
            else
            {
                if (mode == Modes.Outer || mode == Modes.Both)
                {
                    if (nSizeOuter <= 0 || nSizeOuter > colorOuter.A) return;
                    DrawOuterShadow(gr, colorBK);
                }
                if (mode == Modes.Inner || mode == Modes.Both)
                {
                    if (nSizeInner <= 0 || nSizeInner > colorInner.A) return;
                    DrawInnerShadow(gr);
                }
            }
        }

        private void DrawInnerShadow(Graphics gr)
        {
            if (rectInner == null || rectInner == Rectangle.Empty) return;
            if (pen == null) pen = new Pen(colorInner);
            if (pen.Color != colorInner) pen.Color = colorInner;

            Rectangle rect = new Rectangle((int)(rectInner.X + pen.Width / 2), (int)(rectInner.Y + pen.Width / 2), (int)(rectInner.Width - pen.Width), (int)(rectInner.Height - pen.Width));

            int nStep = colorInner.A / nSizeInner;
            if (nStep <= 0) nStep = 1;
            for (int i = colorInner.A; i > 0; i -= nStep)
            {
                pen.Color = Color.FromArgb(i/*alpha*/, pen.Color);
                gr.DrawRectangle(pen, rect);

                rect.Inflate(-1, -1);
            }
        }

        private void DrawOuterShadow(Graphics gr, Color colorBK)
        {
            if (rectOuter == null || rectOuter == Rectangle.Empty) return;
            //if (this.pen == null) this.pen = new Pen(colorInner);

            // clear background
            if (penBack == null || penBack.Width!=nSizeOuter || penBack.Color!=colorBK) penBack = new Pen(colorBK, nSizeOuter);
            gr.DrawRectangle(penBack, new Rectangle((int)(rectOuter.X + penBack.Width / 2), (int)(rectOuter.Y + penBack.Width / 2), (int)(rectOuter.Width - penBack.Width), (int)(rectOuter.Height - penBack.Width)));
            
            // draw shadow
            if (pen == null) pen = new Pen(colorOuter, 1);
            if (pen.Color != colorOuter) pen.Color = colorOuter;
            Rectangle rect = new Rectangle((int)(rectOuter.X + pen.Width / 2), (int)(rectOuter.Y + pen.Width / 2), (int)(rectOuter.Width - pen.Width), (int)(rectOuter.Height - pen.Width));

            int nStep = colorOuter.A / nSizeOuter;
            if (nStep <= 0) nStep = 1;
            for (int i = 0; i < colorOuter.A; i += nStep)
            {
                pen.Color = Color.FromArgb(i/*alpha*/, pen.Color);
                gr.DrawRectangle(pen, rect);

                rect.Inflate(-1, -1);
            }
        }

        #endregion
    }
}