using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace BarChart
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class CBackgroundProperty
    {
        #region Fields

        // How to paint background
        public enum PaintingModes
        {
            SolidColor,
            LinearGradient,
            RadialGradient
        }

        // Firs Gradient color( at the moment top)
        private Color gradientColor1;

        // Second gradient color (at the moment, bottom)
        private Color gradientColor2;

        // Color for solid background
        private Color solidColor;

        // Defines painting mode of this background object
        private PaintingModes paintingMode;

        // Background brush
        private Brush brush;

        private RectangleF rectBound;

        [Browsable(false)]
        private RectangleF rectGradient;
        
        [Browsable(false)]
        GraphicsPath pathGradient;

        [Browsable(false)]
        PointF radialCenterPoint;
        #endregion

        #region Properties

        [Browsable(true)]
        public PaintingModes PaintingMode
        {
            get { return paintingMode; }
            set 
            {
                if (value != paintingMode)
                {
                    paintingMode = value;
                    ResetBrush();
                }
            }
        }
        
        [Browsable(true)]
        public Color SolidColor
        {
            get { return solidColor; }
            set 
            { 
                solidColor = value;
                ResetBrush();
            }
        }
        
        [Browsable(true)]
        public Color GradientColor2
        {
            get { return gradientColor2; }
            set 
            { 
                gradientColor2 = value;
                ResetBrush();
            }
        }
       
        [Browsable(true)]
        public Color GradientColor1
        {
            get { return gradientColor1; }
            set 
            { 
                gradientColor1 = value;
                ResetBrush();
            }
        }
       
        [Browsable(false)]
        public RectangleF BoundRect
        {
            get { return rectBound; }
        }

        #endregion

        public void SetBoundRect(RectangleF boundRect)
        {
            rectBound.X = boundRect.X;
            rectBound.Y = boundRect.Y;
            rectBound.Width = boundRect.Width;
            rectBound.Height = boundRect.Height;
        }

        // Constructor
        public CBackgroundProperty()
        {
            brush = null;

            paintingMode = PaintingModes.RadialGradient;
            gradientColor1 = Color.FromArgb(255, 140, 210, 245);
            gradientColor2 = Color.FromArgb(255, 0, 30, 90);
            solidColor = gradientColor2;

            rectGradient = RectangleF.Empty;
            pathGradient = new GraphicsPath();
        }

        #region Methods

        // Recreates BK brush
        private void ResetBrush()
        {
            // Delete last brush
            if (brush != null)
            {
                brush.Dispose();
                brush = null;
            }

            // Create backgroud brush
            if (PaintingMode == PaintingModes.LinearGradient)
            {
                if (BoundRect.Height <= 0) return;

                brush = new LinearGradientBrush(
                    new Point((int)BoundRect.X, (int)BoundRect.Y),
                    new Point((int)BoundRect.X, (int)BoundRect.Bottom),
                    GradientColor1, GradientColor2);
            }
            else if (PaintingMode == PaintingModes.RadialGradient)
            {
                CreateGradientBrush();
            }
            else
            {
                brush = new SolidBrush(SolidColor);
            }
        }

        private void CreateGradientBrush()
        {
            if (rectBound == null || rectBound.Width<1 || rectBound.Height<1) return;
            
            PathGradientBrush brushGradient;

            rectGradient.X = rectBound.Left - rectBound.Width / 2;
            rectGradient.Y = rectBound.Top - rectBound.Height / 3;
            rectGradient.Width = rectBound.Width * 2;
            rectGradient.Height = rectBound.Height + rectBound.Height / 2;

            radialCenterPoint.X = rectBound.Left + rectBound.Width / 2;
            radialCenterPoint.Y = rectBound.Top + rectBound.Height / 2;

            pathGradient.Reset();
            pathGradient.AddEllipse(rectGradient);

            brushGradient = new PathGradientBrush(pathGradient);
            brushGradient.CenterPoint = radialCenterPoint;
            brushGradient.CenterColor = gradientColor1;
            brushGradient.SurroundColors = new Color[] { gradientColor2 };

            brush = brushGradient;
            brushGradient = null;

        }

        // Draws background inside visible rectangle of the given graphics
        public void Draw(Graphics gr, RectangleF rectBound)
        {
            SetBoundRect(rectBound);
            
            ResetBrush();
            if (brush == null) return;

            gr.FillRectangle(brush, rectBound);
        }

        #endregion
    }
}