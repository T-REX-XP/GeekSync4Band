using System.ComponentModel;
using System.Drawing;

namespace BarChart
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class CLabelProperty
    {
        private bool bVisible;
        public bool Visible
        {
            get { return bVisible; }
            set { bVisible = value; }
        }

        private Font font;
        public Font Font
        {
            get { return font; }
            set 
            {
                FontSet(value);
                if (font != null) fFontDefaultSize = font.Size;
            }
        }

        private Color color;
        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

        public CLabelProperty()
        {
            FontDefaultSize = 8;
            Font = new Font("Tahoma", FontDefaultSize, FontStyle.Bold);

            Color = Color.FromArgb(255, 255, 255, 255);
            Visible = true;
        }
        private float fFontDefaultSize;

        [Browsable(false)]
        public float FontDefaultSize
        {
            get { return fFontDefaultSize; }
            set { fFontDefaultSize = value; }
        }

        // Updates current font, but does not change default size
        public void FontSet(Font font)
        {
            if (this.font != null)
            {
                this.font.Dispose();
                this.font = null;
            }

            this.font = new Font(font, font.Style);
        }

        public void FontSetSize(float fNewSize)
        {
            if (font == null)
            {
                font = new Font("Tahoma", fNewSize, FontStyle.Bold);

            }
            else
            {
                if (font.Size == fNewSize) return;

                Font old = font;
                font = new Font(old.FontFamily, fNewSize, old.Style);

                old.Dispose();
                old = null;
            }
        }

        public void FontReset()
        {
            string strFace = "Tahoma";
            FontStyle style = FontStyle.Bold;

            if (font != null)
            {
                if (font.Size == fFontDefaultSize) return;

                if (font.Name != strFace) strFace = font.Name;
                if (font.Style != style) style = font.Style;

                font.Dispose();
                font = null;

            }
            if (FontDefaultSize <= 0)
            {
                font = null;
                return;
            }

            font = new Font(strFace, FontDefaultSize, style);
        }
    }
}