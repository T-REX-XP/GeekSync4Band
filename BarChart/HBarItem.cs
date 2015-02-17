using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace BarChart
{
    public class HBarItem : ICloneable
    {
        #region Fields

        // A refrence to parent, in case of any changes that parent should know
        private HBarItems parent;

        // Actual bar rect inside the bounding rectangle
        protected RectangleF rectBar;

        // Bounding rectangle of the bar (Bar.Left, Chart.Top, Bar.Width, Chart.Height)
        public RectangleF BoundRect;
        //protected RectangleF rectBound;

        // Rectangle inside which value of this bar will be drawn
        public RectangleF ValueRect;
        
        // Rectangle inside which Label of this bar will be drawn
        public RectangleF LabelRect;
        
        // Main color of the bar. Might be used to create a gradient
        protected Color colorBar;

        // Label under the bar
        protected string strLabel;

        // A double value of the bar positive or negative
        protected double dValue;

        bool bShowBorder;

        // A set of GDI objects to draw the bar
        private Color ColorBacklightEnd;
        private Color ColorGlowStart;
        private Color ColorGlowEnd;
        private Color ColorFillBK;
        private Color ColorBorder;

        SolidBrush brushFill;

        private RectangleF rectGradient;
        private GraphicsPath pathGradient;
        private PathGradientBrush brushGradient;
        private Color[] ColorGradientSurround;

        private LinearGradientBrush brushGlow;
        private RectangleF rectGlow;
        private PointF gradientCenterPoint;

        #endregion

        #region Properties

        public HBarItems Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        public RectangleF BarRect
        {
            get { return rectBar; }
            set 
            { 
                rectBar = value;
                CreateGlowBrush();
            }
        }

        public Color Color
        {
            get { return colorBar; }
            set 
            { 
                colorBar = value;

                ColorFillBK = GetDarkerColor(Color, 85);
                ColorBorder = GetDarkerColor(Color, 100);

                if (brushFill != null)
                {
                    brushFill.Dispose();
                    brushFill = null;
                }
                brushFill = new SolidBrush(ColorFillBK);
            }
        }
        
        [Localizable(true)]
        public string Label
        {
            get { return strLabel; }
            set { strLabel = value; }
        }
        
        public double Value
        {
            get { return dValue; }
            set 
            {
                dValue = value; 
                if (Parent != null) Parent.ShouldReCalculate = true;
            }
        }

        public bool ShowBorder
        {
            get { return bShowBorder; }
            set { bShowBorder = value; }
        }
        
        #endregion

        #region Constructors

        // Constructors
        public HBarItem(double dValue, string strLabel, Color colorBar, RectangleF rectfBar, RectangleF rectfBound, HBarItems Parent)
            : this(dValue, strLabel, colorBar, rectfBar, rectfBound)
        {
            this.Parent = Parent;
        }
      
        public HBarItem(double dValue, string strLabel, Color colorBar, RectangleF rectfBar, RectangleF rectfBound)
            : this(dValue, strLabel, colorBar, rectfBar)
        {
            BarRect = rectfBound;
        }

        public HBarItem(double dValue, string strLabel, Color colorBar, RectangleF barRect)
            : this(dValue, strLabel, colorBar)
        {
            rectBar = barRect;
        }

        public HBarItem(double dValue, string strLabel, Color colorBar)
            : this()
        {
            Value = dValue;
            Label = strLabel;
            Color = colorBar;
        }

        public HBarItem()
        {
            colorBar = Color.Empty;

            Value = 0;

            Label = string.Empty;

            Parent = null;

            ColorBacklightEnd = Color.FromArgb(80, 0, 0, 0);
            ColorGradientSurround = new Color[] { ColorBacklightEnd };

            ShowBorder = true;

            BarRect = RectangleF.Empty;
            BoundRect = new RectangleF(0, 0, 0, 0);
        }

        #endregion

        #region ICloneable Members

        object ICloneable.Clone()
        {
            return new HBarItem(Value, Label, Color, BarRect, BoundRect);
        }

        public object Clone()
        {
            return new HBarItem(Value, Label, Color, BarRect, BoundRect);
        }

        #endregion

        #region Methods

        // In case chart uses a theme that needs a gradient
        private void CreateGradientBrush()
        {
            // Reset all objects
            if (pathGradient == null)
            {
                pathGradient = new GraphicsPath();
                //pathGradient.Dispose();
                //pathGradient = null;
            }
            if (brushGradient != null)
            {
                brushGradient.Dispose();
                brushGradient = null;
            }

            // Create or reset objects
            rectGradient.X = rectBar.Left - rectBar.Width / 8;
            rectGradient.Y = rectBar.Top - rectBar.Height / 2;
            rectGradient.Width = rectBar.Width * 2;
            rectGradient.Height = rectBar.Height * 2;

            gradientCenterPoint.X = rectBar.Right;
            gradientCenterPoint.Y = rectBar.Top + rectBar.Height / 2;

            pathGradient.Reset();
            pathGradient.AddEllipse( rectGradient );

            brushGradient = new PathGradientBrush(pathGradient);
            brushGradient.CenterPoint = gradientCenterPoint;
            brushGradient.CenterColor = Color;
            brushGradient.SurroundColors = ColorGradientSurround;
        }

        // In case chart uses Glass theme
        void CreateGlowBrush()
        {
            if (rectBar.Height <= 0) rectBar.Height = 1;

            // Caculate Glow density
            int nAlphaStart = (int)(185 + 5 * BarRect.Width / 24),
                nAlphaEnd = (int)(10 + 4 * BarRect.Width / 24);

            if (nAlphaStart > 255) nAlphaStart = 255;
            else if (nAlphaStart < 0) nAlphaStart = 0;

            if (nAlphaEnd > 255) nAlphaEnd = 255;
            else if (nAlphaEnd < 0) nAlphaEnd = 0;
            
            ColorGlowStart = Color.FromArgb(nAlphaEnd, 255, 255, 255);
            ColorGlowEnd = Color.FromArgb(nAlphaStart, 255, 255, 255);

            if (brushGlow != null)
            {
                brushGlow.Dispose();
                brushGlow = null;
            }

            rectGlow = new RectangleF(rectBar.Left, rectBar.Top, rectBar.Width / 2, rectBar.Height);
            brushGlow = new LinearGradientBrush(
                new PointF(rectGlow.Right + 1, rectGlow.Top), 
                new PointF(rectGlow.Left - 1, rectGlow.Top),
                ColorGlowStart, ColorGlowEnd);
        }

        // Draws a bar item. This function does not draw label or value of a bar
        public void Draw(Graphics gr)
        {
            if (BarRect.Width <= 0 || BarRect.Height <= 0) return;
           
            // Draw fill color
            if (parent.DrawingMode == HBarItems.DrawingModes.Solid)
            {
                gr.FillRectangle(new SolidBrush(Color), BarRect);
            }
            else
            {
                gr.FillRectangle(brushFill, BarRect);
            }

            // Draw gradients
            if (parent.DrawingMode == HBarItems.DrawingModes.Glass ||
                parent.DrawingMode == HBarItems.DrawingModes.Rubber)
            {
                CreateGradientBrush();
                gr.FillRectangle(brushGradient, BarRect);
            }

            if (parent.DrawingMode == HBarItems.DrawingModes.Glass)
            {
                gr.FillRectangle(brushGlow, rectGlow);
            }
            
            // Draw border
            if (ShowBorder)
            {
                gr.DrawRectangle(
                    new Pen(ColorBorder, 1),
                    rectBar.Left, rectBar.Top, rectBar.Width, rectBar.Height);
            }
        }

        // Decrease all RGB values as much as 'intensity' says
        private Color GetDarkerColor(Color color, byte intensity)
        {
            int r, g, b;

            r = color.R - intensity;
            g = color.G - intensity;
            b = color.B - intensity;

            if (r > 255 || r < 0) r *= -1;
            if (g > 255 || g < 0) g *= -1;
            if (b > 255 || b < 0) b *= -1;

            return Color.FromArgb(255, (byte)r, (byte)g, (byte)b);
        }

        #endregion
    }
}