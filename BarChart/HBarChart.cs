using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace BarChart
{
    [
    ComplexBindingProperties("DataSource", "DataMember"), 
    ToolboxBitmap(typeof(HBarChart), "BarChart.bmp")]
    public partial class HBarChart : UserControl
    {
        #region Fields
        private CDescriptionProperty description;
        private CLabelProperty label;
        private CValueProperty values;
        private CBackgroundProperty background;
        private BarSizingMode sizingMode;
        private CBorderProperty border;
        private CShadowProperty shadow;
        private int nBarWidth;
        private int nBarsGap;
        private CDataSourceManager dataSourceManager;

        // 
        private Rectangle rectBK;
        // visible area of the chart
        private Rectangle bounds;
        private RectangleF rectDesc;

        // A collection of all bars data
        //protected HItems bars;
        protected HBarItems bars;

        // Tooltip of the chart
        protected ToolTip tooltip;

        // A back buffer for double buffering to have flicker-free drawing
        private Bitmap bmpBackBuffer;

        // Used in MouseMove event to trak index of the last bar under the mouse
        [Browsable(false)]
        private int nLastVisitedBarIndex;

        // It seems that since tooltip class draws the tip over mouse cursor
        // the MouseMove event is raised constantly, if I call setTooltip to
        // change tooltip text in MouseMove event. To ignore Tooltip from being
        // repeatedly redrawing, while I don't have time to make it owner drawn
        // and prevent drawing over cursor, I'll ignor move events after first one.
        [Browsable(false)]
        private Point ptLastTooltipMouseLoction;
        
        #endregion

        #region Properties

        /// <summary>
        /// Underling collection of all bars, a list of HBarItem objects each of which correspond to a bar.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content), Category("Bar Chart"), Description("A collection of chart items. A bar for each item will be drawn.")]
        public HBarItems Items
        {
            get { return bars; }
            set { bars = value;}
        }

        /// <summary>
        /// A description line of text at the bottom of the chart.
        /// </summary>
         [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content), Category("Bar Chart"), Description("Look and feel of the description line at the bottom of the chart.")]
        public CDescriptionProperty Description
        {
            get { return description; }
            set { description = value; }
        }

        /// <summary>
        /// A boredr around chart main rectangle.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content), Category("Bar Chart"), Description("Settings of the border around the chart.")]
        public CBorderProperty Border
        {
            get { return border; }
            set { border = value; }
        }

        /// <summary>
        /// Outer and inner shadows around chart border.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content), Category("Bar Chart"), Description("Settings of the shadows around the chart.")]
        public CShadowProperty Shadow
        {
            get { return shadow; }
            set { shadow = value; }
        }

        /// <summary>
        /// Settings of the text drawn for each bar describing what the bar is displaying.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content), Category("Bar Chart"), Description("Look and feel of the label at the bottom of each bar.")]
        public CLabelProperty Label
        {
            get { return label; }
            set { label = value; }
        }

        /// <summary>
        /// Settings of values(or %) each bar displays.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content), Category("Bar Chart"), Description("Look and feel of the Value/Percent presented at the top of each bar.")]
        public CValueProperty Values
        {
            get { return values; }
            set { values = value; }
        }

        /// <summary>
        /// Background of the chart, might be a solid color, linear gradient or radial gradient.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content), Category("Bar Chart"), Description("Chart background style and colors.")]
        public CBackgroundProperty Background
        {
            get { return background; }
            set { background = value; }
        }

        /// <summary>
        /// Default width of each bar. Has no effect in Autoscale sizing mode.
        /// </summary>
        [Browsable(true)]
        [Category("Bar Chart")]
        public int BarWidth
        {
            get { return nBarWidth; }
            set { nBarWidth = value; }
        }

        /// <summary>
        /// Space between bars of the bar graph, and between bars and borders of chart
        /// </summary>
        [Browsable(true)]
        [Category("Bar Chart")]
        public int BarsGap
        {
            get { return nBarsGap; }
            set { nBarsGap = value; }
        }

        /// <summary>
        /// Tooltip class of the chart
        /// </summary>
        [Browsable(true)]
        [Category("Bar Chart")]
        public ToolTip BarTooltip
        {
            get { return tooltip; }
            set { tooltip = value; }
        }

        public enum BarSizingMode
        {
            Normal,         // Use variable values for width of the bar
            AutoScale       // Automatically calculate the bounding rectangle and fit all bars inside the control
        }

        /// <summary>
        /// Enumerator defining sizing capabilities of the chart.
        /// </summary>
        [Browsable(true)]
        [Category("Bar Chart")]
        public BarSizingMode SizingMode
        {
            get { return sizingMode; }
            set { sizingMode = value; }
        }

        /// <summary>
        /// Gets number of bars of the chart
        /// </summary>
        [Browsable(false)]
        [Category("Bar Chart")]
        public int Count
        {
            get { return bars.Count; }
        }

        /// <summary>
        /// get or set data member of the connected data source. Chart reads data of this data member.
        /// </summary>
        [
         DefaultValue(""),
         Category("Bar Chart"),
		 Editor("System.Windows.Forms.Design.DataMemberListEditor, System.Design",
			 "System.Drawing.Design.UITypeEditor, System.Drawing"),
         Description("Defines data member of the connected data source. Chart reads data of this data member.")
        ]
        public string DataMember
        {
            get
            {
                if (dataSourceManager == null)
                {
                    return String.Empty;
                }
                else
                {
                    return dataSourceManager.DataMember;
                }
            }
            set
            {
                if (value != DataMember)
                {
                    if (dataSourceManager == null)
                    {
                        CreateChartForEachRow eventHandler = new CreateChartForEachRow();
                        dataSourceManager = new CDataSourceManager(this);
                        dataSourceManager.DataEventHandler = eventHandler;
                    }
                    dataSourceManager.ConnectTo(DataSource, value);
                }
            }
        }

        /// <summary>
        /// Get or Set Data Source to connected to.
        /// </summary>
        [
         DefaultValue(null),
         RefreshProperties(RefreshProperties.Repaint),
         AttributeProvider(typeof(IListSource)),
         Category("Bar Chart"),
         Description("Defines Data Source to connected to."),
		 TypeConverter("System.Windows.Forms.Design.DataSourceConverter, System.Design")
        ]
        public object DataSource
        {
            get
            {
                if (dataSourceManager == null)
                {
                    return null;
                }
                else
                {
                    return dataSourceManager.DataSource;
                }
            }
            set
            {
                if (value != DataSource)
                {
                    if (dataSourceManager == null)
                    {
                        CreateChartForEachRow eventHandler = new CreateChartForEachRow();
                        dataSourceManager = new CDataSourceManager(this);

                        dataSourceManager.DataEventHandler = eventHandler;
                        dataSourceManager.ConnectTo(value, DataMember);
                    }
                    else
                    {
                        dataSourceManager.ConnectTo(value, DataMember);
                        if (value == null)
                        {
                            dataSourceManager = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Data manger is responsible for coordinating dataconnection and data event handler.
        /// </summary>
        [Browsable(false)]
        public CDataSourceManager DataSourceManager
        {
            get { return dataSourceManager; }
        }

        #endregion //"Properties"

        #region CustomEvents

        /// <summary>
        /// Delegate type of the barchart bar related events
        /// </summary>
        /// <param name="sender">The HBarChart who sent the event</param>
        /// <param name="e">BarEventArgs that contains event information</param>
        public delegate void OnBarEvent(object sender, BarEventArgs e);

        /// <summary>
        /// Mouse moved into territory of a bar :-)
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content), Category("Bar Chart"), Description("Mouse is now over a bar rectangle starting from top of the chart, left of the bar and ending right of the bar and bottom of the chart.")]
        public event OnBarEvent BarMouseEnter;

        /// <summary>
        /// Mouse just left a bar
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content), Category("Bar Chart"), Description("Mouse just hovered out a bar.")]
        public event OnBarEvent BarMouseLeave;

        /// <summary>
        /// Mouse click event occured on a bar
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content), Category("Bar Chart"), Description("Mouse click event occurd while mouse is over a bar.")]
        public event OnBarEvent BarClicked;

        /// <summary>
        /// Mouse double click on a bar
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content), Category("Bar Chart"), Description("Mouse double click event occurd while mouse is over a bar")]
        public event OnBarEvent BarDoubleClicked;

        // A bar clicked
        private void RaiseClickEvent(HBarItem bar, int nIndex)
        {
            if (BarClicked != null)
            {
                BarClicked(this, new BarEventArgs(bar, nIndex));
            }
        }

        // A bar double clicked
        private void RaiseDoubleClickEvent(HBarItem bar, int nIndex)
        {
            if (BarDoubleClicked != null)
            {
                BarDoubleClicked(this, new BarEventArgs(bar, nIndex));
            }
        }

        // Mouse moved over a bar
        private void RaiseHoverInEvent(HBarItem bar, int nIndex)
        {
            if (BarMouseEnter != null)
            {
                BarMouseEnter(this, new BarEventArgs(bar, nIndex));
            }
        }

        // Mouse moved out over a bar
        private void RaiseHoverOutEvent(HBarItem bar, int nIndex)
        {
            if (BarMouseLeave != null)
            {
                BarMouseLeave(this, new BarEventArgs(bar, nIndex));
            }
        }

        #endregion // Events

        #region Methods
        
        /// <summary>
        /// Causes the modifications to be trigered to GUI of the chart.
        /// </summary>
        public void RedrawChart()
        {
            if (bmpBackBuffer != null)
            {
                bmpBackBuffer.Dispose();
                bmpBackBuffer = null;
            }

            Refresh();
        }

        /// <summary>
        /// Add a new item(bar) to the chart
        /// </summary>
        /// <param name="dValue">Double value of the new bar</param>
        /// <param name="strLabel">Label description of the bar</param>
        /// <param name="colorBar">Color of the bar</param>
        public void Add(double dValue, string strLabel, Color colorBar)
        {
            bars.Add(new HBarItem(dValue, strLabel, colorBar));
        }

        /// <summary>
        /// Remove a bar by it's a zero based index from chart
        /// </summary>
        /// <param name="nIndex">Index of the bar to be removed</param>
        /// <returns>true if item removed or false in case of any error, most likely index out of range</returns>
        public bool RemoveAt(int nIndex)
        {
            if (nIndex < 0 || nIndex >= bars.Count) return false;

            bars.RemoveAt(nIndex);
            return true;
        }

        /// <summary>
        /// Retrieve a bar by it's a zero based index
        /// </summary>
        /// <param name="nIndex">Index of the bar</param>
        /// <param name="bar">Out parameter. Will hold the bar after retrieving it</param>
        /// <returns>true if bar exists and retrieved, otherwise false</returns>
        public bool GetAt(int nIndex, out HBarItem bar)
        {
            bar = null;
            if (nIndex < 0 || nIndex >= bars.Count) return false;

            bar = bars[nIndex];
            return true;
        }

        /// <summary>
        /// Change current value of a bar
        /// </summary>
        /// <param name="nIndex">Zero based index of the bar</param>
        /// <param name="dNewValue">New value to replace existing value of the bar</param>
        /// <returns>true if changed successfully</returns>
        public bool ModifyAt(int nIndex, double dNewValue)
        {
            if (nIndex < 0 || nIndex >= bars.Count) return false;

            bars[nIndex].Value = dNewValue;
            return true;
        }

        /// <summary>
        /// Change a bar with a new one
        /// </summary>
        /// <param name="nIndex">Zero based index of the bar</param>
        /// <param name="barNew">New properties of the bar</param>
        /// <returns></returns>
        public bool ModifyAt(int nIndex, HBarItem barNew)
        {
            if (nIndex < 0 || nIndex >= bars.Count) return false;

            bars.RemoveAt(nIndex);
            bars.Insert(nIndex, barNew);

            return true;
        }

        /// <summary>
        /// Insert a new bar at a specified zero based index
        /// </summary>
        /// <param name="nIndex">Zero based index of the bar</param>
        /// <param name="dValue">New value of the bar</param>
        /// <param name="strLabel">Label of the bar</param>
        /// <param name="colorBar">Color of the bar</param>
        /// <returns>true if bar inserted otherwise false.</returns>
        public bool InsertAt(int nIndex, double dValue, string strLabel, Color colorBar)
        {
            if (nIndex < 0 || nIndex >= bars.Count) return false;

            bars.Insert(nIndex, new HBarItem(dValue, strLabel, colorBar));

            return true;
        }

        /// <summary>
        /// Prints the chart in a WYSIWYG manner
        /// </summary>
        /// <param name="bFitToPaper">If true, chart will fill whole paper surface</param>
        /// <param name="strDocName">A name for print job document.</param>
        /// <returns></returns>
        public bool Print(bool bFitToPaper, string strDocName)
        {
            CPrinter printer = new CPrinter();

            // Ask user to select a printer and set options for it
            printer.ShowOptions();
            
            // Customize the document and sizing mode
            printer.Document.DocumentName = strDocName;
            printer.FitToPaper = bFitToPaper;

            // Create and prepare a bitmap to be printed into printer DC
            Bitmap bmpChart;
            if (bFitToPaper)
            {
                // Full screen
                bmpChart = new Bitmap(
                    printer.Document.DefaultPageSettings.Bounds.Width,
                    printer.Document.DefaultPageSettings.Bounds.Height);
            }
            else
            {
                // WYSIWYG
                bmpChart = (Bitmap)bmpBackBuffer.Clone();
            }
            // Draw On the bitmap
            DrawChart(ref bmpChart);
            
            // Set bitmap for printing
            printer.BmpBuffer = bmpChart;
            
            // Ask printer class to print its bitmap.
            bool bRet = false;
            bRet =  printer.Print();

            // Remove bitmap from memory
            bmpChart.Dispose();
            bmpChart = null;

            return bRet;
        }

        // Will be called when chart is being resized. We need to redraw the chart.
        private void OnSize(object sender, EventArgs e)
        {
            RedrawChart();
        }

        // Wanna connect this to a data source?
        protected override void OnBindingContextChanged(EventArgs e)
        {
            try
            {
                if (dataSourceManager != null)
                {
                    try
                    {
                        dataSourceManager.ConnectTo(DataSource, DataMember);
                    }
                    catch (ArgumentException)
                    {
                        if (DesignMode)
                        {
                            DataMember = String.Empty;
                            return;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                else
                {
                    base.OnBindingContextChanged(e);
                }
            }
            finally
            {
            }
        }

        // To add a null bar
        internal void Add(object nullObject)
        {
            // UNDONE: Char must display something like a question mark here
            //         so that users know that no value is available for this bar
            Add(0.0, "N/A", Color.Black);
        }

        #endregion

        #region constructors
        
        // Constructor
        public HBarChart()
        {
            bounds = new Rectangle(0, 0, 0, 0);
            border = new CBorderProperty();
            shadow = new CShadowProperty();
            rectDesc = new RectangleF(0, 0, 0, 0);
            
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.ResizeRedraw, true);

            // Designer
            InitializeComponent();

            description = new CDescriptionProperty();
            label = new CLabelProperty();
            values = new CValueProperty();
            background = new CBackgroundProperty();

            // Initialize members
            nBarWidth = 24;
            nBarsGap = 4;

            SizingMode = BarSizingMode.Normal;

            //fontTooltip = new Font("Verdana", 12);

            bars = new HBarItems();

            bmpBackBuffer = null;

            ptLastTooltipMouseLoction = new Point(0, 0);
            tooltip = new ToolTip();
            tooltip.IsBalloon = true;
//            tooltip.ShowAlways = true;
            tooltip.InitialDelay = 0;
            tooltip.ReshowDelay = 0;
//            tooltip.AutoPopDelay = Int32.MaxValue;

            nLastVisitedBarIndex = -1;
        }

        #endregion

        #region Drawings
        // Control needs repainting
        private void OnPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (bmpBackBuffer==null)
            {
                // Redraw the char into back buffer
                DrawChart(ref bmpBackBuffer);
            }

            // Blot the buffer to view
            if (bmpBackBuffer != null)
            {
                /*e.Graphics.DrawImageUnscaled(bmpBackBuffer, 0, 0);*/
               
                e.Graphics.DrawImage(
                    bmpBackBuffer, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
            }
        }

        // Draws a chart on the given bitmap
        private void DrawChart(ref Bitmap bmp)
        {
           
            if (bmp == null)
            {
                bmp = new Bitmap(ClientSize.Width, ClientSize.Height);
            }

            using (Graphics gr = Graphics.FromImage(bmp))
            {
                CalculateBound(bmp.Size);

                // Draw background
                Background.Draw(gr, rectBK);

                //Draw graph and all texts
                DrawBars(gr, bmp.Size);
            }
        }

        // Calculates bounding rectangle of the chart, border and shadows
        private void CalculateBound(Size sizeClient)
        {
            // Calculate bounding rectangle
            bounds = new Rectangle(0, 0, sizeClient.Width, sizeClient.Height);

            if (shadow.Mode == CShadowProperty.Modes.Outer || shadow.Mode == CShadowProperty.Modes.Both)
            {
                shadow.SetRect(bounds, 1);

                bounds.X += shadow.WidthOuter;
                bounds.Y += shadow.WidthOuter;
                bounds.Width -= 2 * shadow.WidthOuter;
                bounds.Height -= 2 * shadow.WidthOuter;
            }
            rectBK = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);

            if (border != null && Border.Visible)
            {
                border.SetRect(bounds);

                bounds.X += Border.Width;
                bounds.Y += Border.Width;
                bounds.Width -= 2 * Border.Width;
                bounds.Height -= 2 * Border.Width;
            }

            if (shadow.Mode == CShadowProperty.Modes.Inner || shadow.Mode == CShadowProperty.Modes.Both)
            {
                shadow.SetRect(bounds, 0);
                /*
                this.bounds.X += this.shadow.WidthInner;
                this.bounds.Y += this.shadow.WidthInner;
                this.bounds.Width -= 2 * this.shadow.WidthInner;
                this.bounds.Height -= 2 * this.shadow.WidthInner;*/
            }

        }

        // Draws bars of the chart along with labels and values
        private void DrawBars(Graphics gr, Size sizeChart)
        {
            if (description==null) return;
            if (label==null) return;
            if (values == null) return;

            // Store some original values
            int nLastBarGaps = nBarsGap;

            // Other calculations
            if (SizingMode == BarSizingMode.AutoScale)
            {
                int nbarWidthTemp = nBarWidth;

                // Calculate gap size
                if (bars.Count > 0)
                {
                    nBarsGap = 4 + (12 * bounds.Width) / (int)(345 * bars.Count * 7);
                    if (nBarsGap > 50) nBarsGap = 50;

                    // Calculate maximum bar size
                    nBarWidth = (bounds.Width - ((bars.Count + 1) * nBarsGap)) / bars.Count;
                    if (nBarWidth <= 0) nBarWidth = 24;
                }

                // Calcuate font sizes & create fonts
                CreateLabelFont(gr, new Size(nBarWidth, 0));
                CreateValueFont(gr, new Size(nBarWidth, 0));
                CreateDescFont(gr, bounds.Size);
                
                CalculatePositions(gr);
                
                nBarWidth = nbarWidthTemp;
            }
            else
            {
                if (values.Font == null || values.Font.Size != values.FontDefaultSize)
                {
                    Values.FontReset();
                }
                if (Label.Font == null || Label.Font.Size != Label.FontDefaultSize)
                {
                    Label.FontReset();
                }
                if (Description.Font == null || Description.Font.Size != Description.FontDefaultSize)
                {
                    Description.FontReset();
                }

                CalculatePositions(gr);
            }

            shadow.Draw(gr, BackColor);

            // Draw description line
            if (Description.Visible && Description.Font!=null)
            {
                StringFormat stringFormat = StringFormat.GenericDefault;
                stringFormat.LineAlignment = StringAlignment.Center;
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.Trimming = StringTrimming.None;
                stringFormat.FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.LineLimit;

                gr.DrawString(
                    description.Text,
                    description.Font,
                    new SolidBrush(description.Color),
                    rectDesc,
                    stringFormat);
            }

            foreach (HBarItem bar in bars)
            {
                // Draw the bar itself
                bar.Draw(gr);

                // Draw label
                if (Label.Visible)
                {
                    gr.DrawString(
                        bar.Label,
                        Label.Font,
                        new SolidBrush(Label.Color),
                        bar.LabelRect);
                }

                // Draw value or %
                if (Values.Visible)
                {
                    string strValue = string.Empty;
                    if (Values.Mode == CValueProperty.ValueMode.Digit)
                    {
                        strValue = bar.Value.ToString("F1");
                    }
                    else if (Values.Mode == CValueProperty.ValueMode.Percent)
                    {
                        if (bars.ABSTotal != 0) strValue =
                            ((double)(bar.Value / bars.ABSTotal)).ToString("P1",
                            CultureInfo.CurrentCulture);
                    }
                    gr.DrawString(
                        strValue,
                        Values.Font,
                        new SolidBrush(Values.Color),
                        bar.ValueRect);
                }

            }

            // Draw chart border
            border.Draw(gr);

            // restore values that changed during the transition to auto scale mode
            nBarsGap = nLastBarGaps;
        }

        // Calculates bounding rectangles of bars, values, labels for 
        // positive, negative or 0 values and also chart description line
        private void CalculatePositions(Graphics gr)
        {
            int         i           = 0;
            bool        bHasNegative= (bars.Maximum < 0 || bars.Minimum < 0);
            bool        bAllNegative= (bars.Maximum < 0 && bars.Minimum < 0);
            float       fBoundTH    = 0;
            float       fBarTH      = 0;
            int         nStartX;//     = (bounds.Size.Width - bars.Count * nBarWidth - (bars.Count + 1) * nBarsGap) / 2;

            // Where all bars start
            nStartX = bounds.X + (bounds.Width - bars.Count * nBarWidth - (bars.Count + 1) * nBarsGap) / 2;

            // Calculating Desc rect
            if ( Description != null && Description.Visible && Description.Font != null && gr != null)
            {
                rectDesc.X = bounds.X + nBarsGap;
                rectDesc.Y = bounds.Bottom - 2 * nBarsGap - Description.Font.GetHeight(gr);
                rectDesc.Width = bounds.Size.Width - 2 * nBarsGap;
                rectDesc.Height = description.Font.GetHeight(gr) + 2 * nBarsGap;
            }
            else rectDesc = RectangleF.Empty;

            foreach (HBarItem bar in bars)
            {
                if (bHasNegative)
                {
                    // Calculating Bar.BoundRect for each bar
                    bar.BoundRect.X = nStartX + i * nBarWidth + (i + 1) * nBarsGap;
                    bar.BoundRect.Width = nBarWidth;
                    if (bAllNegative)
                    {
                        bar.BoundRect.Height = bounds.Height - rectDesc.Height;
                        bar.BoundRect.Y = bounds.Y + nBarsGap;
                    }
                    else
                    {
                        bar.BoundRect.Height = (bounds.Height - rectDesc.Height) / 2 + Label.Font.GetHeight(gr) + nBarsGap / 2;
                        if (bar.Value > 0)
                        {
                            bar.BoundRect.Y = bounds.Y + nBarsGap;
                        }
                        else
                        {
                            bar.BoundRect.Y = (bounds.Height - rectDesc.Height) / 2 - Label.Font.GetHeight(gr) - nBarsGap/2;
                        }
                    }

                    // Calculating Bar.LabelRect for each bar
                    bar.LabelRect.X = bar.BoundRect.X;
                    bar.LabelRect.Width = bar.BoundRect.Width + nBarsGap;
                    bar.LabelRect.Height = Label.Font.GetHeight(gr);
                    if (bAllNegative) bar.LabelRect.Y = nBarsGap;
                    else if (bar.Value >= 0) bar.LabelRect.Y = bar.BoundRect.Bottom - nBarsGap / 2 - bar.LabelRect.Height;
                    else bar.LabelRect.Y = bounds.Y + bar.BoundRect.Top;

                    // Calculating Bar.BarRect for each bar
                    fBoundTH = bar.BoundRect.Height - 2 * nBarsGap - bar.LabelRect.Height - values.Font.GetHeight(gr);
                    fBarTH = (float)((Math.Abs(bar.Value) * fBoundTH) / bars.ABSMaximum);
                    if (!(fBarTH >= 0)) fBarTH = 0;
                    if (bAllNegative)
                    {
                        bar.BarRect = new RectangleF(
                            bar.BoundRect.X,
                            bar.LabelRect.Bottom+nBarsGap,
                            bar.BoundRect.Width,
                            fBarTH);

                        // Calculating Bar.ValueRect for each bar
                        bar.ValueRect.X = bar.BoundRect.X;
                        bar.ValueRect.Y = bar.BarRect.Bottom + nBarsGap;
                        bar.ValueRect.Width = bar.BoundRect.Width;
                        bar.ValueRect.Height = values.Font.GetHeight(gr);
                    }
                    else
                    {
                        bar.BarRect = new RectangleF(
                            bar.BoundRect.X,
                            bounds.Y + (bar.Value > 0 ? (bounds.Height - rectDesc.Height) / 2 - fBarTH : (bounds.Height - rectDesc.Height) / 2),
                            bar.BoundRect.Width,
                            fBarTH);

                        // Calculating Bar.ValueRect for each bar
                        bar.ValueRect.X = bar.BoundRect.X;
                        bar.ValueRect.Y = (bar.Value > 0 ? bar.BarRect.Top - values.Font.GetHeight(gr): bar.BarRect.Bottom+nBarsGap/2);
                        bar.ValueRect.Width = bar.BoundRect.Width + nBarsGap;
                        bar.ValueRect.Height = values.Font.GetHeight(gr);
                    }
                }
                else
                {
                    // Calculating Bar.BoundRect for each bar
                    bar.BoundRect.X = nStartX + i * nBarWidth + (i + 1) * nBarsGap;
                    bar.BoundRect.Y = bounds.Y + nBarsGap;
                    bar.BoundRect.Width = nBarWidth;
                    bar.BoundRect.Height = bounds.Height - rectDesc.Height;

                    // Calculating Bar.LabelRect for each bar
                    if (Label.Visible)
                    {
                        bar.LabelRect.X = bar.BoundRect.X;
                        bar.LabelRect.Y = bounds.Bottom - rectDesc.Height - Label.Font.GetHeight(gr);
                        bar.LabelRect.Width = bar.BoundRect.Width + nBarsGap;
                        bar.LabelRect.Height = Label.Font.GetHeight(gr);
                    }
                    else bar.LabelRect = RectangleF.Empty;

                    // Calculating Bar.BoundRect for each bar
                    fBoundTH = bar.BoundRect.Height - 2*nBarsGap - bar.LabelRect.Height - (values.Visible? values.Font.GetHeight(gr):0);
                    fBarTH = (float)((Math.Abs(bar.Value) * fBoundTH) / bars.ABSMaximum);
                    if (!(fBarTH >= 0)) fBarTH = 0;
                    bar.BarRect = new RectangleF(
                        bar.BoundRect.X,
                        bar.BoundRect.Y + fBoundTH - fBarTH + (values.Visible ? values.Font.GetHeight(gr) : 0),
                        bar.BoundRect.Width,
                        fBarTH);

                    // Calculating Bar.ValueRect for each bar
                    if (Values.Visible)
                    {
                        bar.ValueRect.X = bar.BoundRect.X;
                        bar.ValueRect.Y = bar.BarRect.Top-values.Font.GetHeight(gr) -nBarsGap/2;
                        bar.ValueRect.Width = bar.BoundRect.Width + 2*nBarsGap;
                        bar.ValueRect.Height = values.Font.GetHeight(gr);
                    }
                    else bar.ValueRect = RectangleF.Empty;
                }
                 
                i++;
            }
        }

        // In Autoscale sizing mode, calculates best size for values and creates the font
        private void CreateValueFont(Graphics gr, SizeF sizeBar)
        {
            float fSize1 = 100 + (sizeBar.Width / 24);

            float sizeMax = 0;
            float sizeText;
            string strValue = string.Empty;

            for (int i = 0; i < bars.Count; i++)
            {
                if (Values.Mode == CValueProperty.ValueMode.Digit)
                {
                    strValue = String.Format("{0:F1}", bars[i].Value);
                }
                else if (Values.Mode == CValueProperty.ValueMode.Percent && bars.ABSTotal > 0)
                {
                    strValue = ((double)(bars[i].Value / bars.ABSTotal)).ToString("P1", CultureInfo.CurrentCulture);
                }

                sizeText = gr.MeasureString(strValue, Values.Font).Width;
                if (sizeText > sizeMax)
                {
                    sizeMax = sizeText;
                }
            }

            sizeText = (Values.Font.Size * (sizeBar.Width / sizeMax));

            if (fSize1 <= 0 && sizeText <= 0) return;
            else if (fSize1 <= 0) Values.FontSetSize(sizeText);
            else if (sizeText <= 0) Values.FontSetSize(fSize1);
            else Values.FontSetSize((fSize1 > sizeText ? sizeText : fSize1));

        }

        // In Autoscale sizing mode, calculates best size for labels and creates the font
        private void CreateLabelFont(Graphics gr, SizeF sizeBar)
        {
            float fSize1 = 100 + (sizeBar.Width / 24);
            float sizeMax = 0;
            float sizeText;
            for (int i = 0; i < bars.Count; i++)
            {
                sizeText = gr.MeasureString(bars[i].Label, Label.Font).Width;
                if (sizeText > sizeMax)
                {
                    sizeMax = sizeText;
                }
            }

            //sizeText = sizeMax;
            float fWidthRatio = sizeBar.Width / sizeMax;
            sizeText = (Label.Font.Size * fWidthRatio);

            if (fSize1 <= 0 &&  sizeText<= 0) return;
            else if (fSize1 <= 0) Label.FontSetSize(sizeText);
            else if (sizeText <= 0) Label.FontSetSize(fSize1);
            else Label.FontSetSize((fSize1 > sizeText ? sizeText : fSize1));

        }

        // In Autoscale sizing mode, calculates best size for description font and creates it. used in auto scaling mode
        private void CreateDescFont(Graphics gr, SizeF sizeBound)
        {
            float fSize1 = sizeBound.Height / 15;

            float fWidthRatio = (sizeBound.Width - 2 * nBarsGap) / gr.MeasureString(description.Text, description.Font).Width;
            float fSize2 = (description.Font.Size * fWidthRatio);

            if (fSize1 <= 0 && fSize2 <= 0) return;
            else if (fSize1 <= 0) description.FontSetSize( fSize2 );
            else if (fSize2 <= 0) description.FontSetSize( fSize1 );
            else description.FontSetSize( (fSize1>fSize2?fSize2:fSize1) );
        }

        // Draw a bar with label & value 
        private void DrawBar(Graphics gr, HBarItem bar)
        {
            // Draw the bar itself
            bar.Draw(gr);

            // Draw label
            if (Label.Visible)
            {
                float nLabelHeight = Label.Font.GetHeight(gr);
                gr.DrawString(
                    bar.Label,
                    Label.Font,
                    new SolidBrush(Label.Color),
                    new RectangleF(
                        bar.BarRect.X,
                        bar.BarRect.Bottom + nBarsGap,
                        bar.BarRect.Width,
                        nLabelHeight));
            }

            // Draw value or %
            if (Values.Visible)
            {
                string strValue = string.Empty;
                if (Values.Mode == CValueProperty.ValueMode.Digit)
                {
                    strValue = bar.Value.ToString("F1");
                }
                else if (Values.Mode == CValueProperty.ValueMode.Percent)
                {
                    if (bars.Total > 0) strValue =
                        ((double)(bar.Value / bars.Total)).ToString("P1", 
                        CultureInfo.CurrentCulture);
                }

                float fValueHeight = Values.Font.GetHeight(gr);
                gr.DrawString(
                    strValue,
                    Values.Font,
                    new SolidBrush(Values.Color),
                    new RectangleF(
                        bar.BarRect.X,
                        bar.BarRect.Top - fValueHeight - 1,
                        bar.BarRect.Width + 2 * nBarsGap,
                        fValueHeight));
            }
        }
        
        // Prevent the control to draw any backgrounds
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // Do nothing
        }

        #endregion  // Drawings

        #region MouseEvents

        // Resets tooltip text and display position
        private void SetCurrTooltip(HBarItem bar)
        {
            if (bar == null)
            {
                tooltip.Hide(this);
                tooltip.RemoveAll();

            }
            else
            {
                //tooltip.Active = true;
                string strCaption = string.Empty;
                string strPercent = string.Empty;

                if (bars.Total > 0)
                {
                    strPercent = ((double)(bar.Value / bars.Total)).
                        ToString("P2", CultureInfo.CurrentCulture);
                }

                strCaption = String.Format("{0}\r\n{1}", bar.Value, strPercent);
                
                // This seems not to be working in Vista. I hope it will after that
                if (Environment.OSVersion.Version.Major!=6)
                {
                    tooltip.Hide(this);
                    tooltip.RemoveAll();            
                }

                tooltip.ToolTipTitle = bar.Label;
                tooltip.SetToolTip(this, strCaption);
            }
        }

        // Is Mouse pointer inside a bar rectangle?
        private HBarItem HitTest(Point MousePoint, out int nIndex)
        {
            //HBarData bar;
            nIndex = -1;
            for (int i = 0; i < bars.Count; i++)
            {
                //if (bars.GetAt(i, out bar))
                //{
                    if (bars[i].BoundRect.Contains(MousePoint))
                    {
                        nIndex = i;
                        return bars[i];
                    }
                //}
            }

            return null;
        }

        // Why this function is called when mouse is not moving but is just over control?
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            // If mouse is actually moving!
            if (ptLastTooltipMouseLoction != e.Location)
            {
                ptLastTooltipMouseLoction = e.Location;

                int nIndex;
                HBarItem bar = HitTest(e.Location, out nIndex);

                if (bar != null)
                {
                    // So mouse is inside a bar
                    #region HoverInEvent

                    if (nLastVisitedBarIndex >= 0)
                    {
                        if (nIndex != nLastVisitedBarIndex)
                        {
                            // If moved into a new bar, from another bar
                            // How odd, it didn't notice it's over empty space, I miss SetCapture.
                            //OnBarLeave();
                            OnBarEnter(bar, nIndex);
                        }
                        else
                        {
                            // Moving along a bar
                            // Logically should be enabled, but will cause a bug in vista so no tooltip will be visible any longer
                            //SetCurrTooltip(null);
                            //SetCurrTooltip(bar);
                        }
                    }
                    else
                    {
                        // If moved into a bar from empty space
                        OnBarEnter(bar, nIndex);
                    }
                    #endregion
                    
                    SetCurrTooltip(bar);
                }
                else
                {
                    // Mouse moving in empty space
                    OnBarLeave();
                }
            }
            else
            {
                // Funny! Mouse is not moving. It's just placed over this control.
            }
        }

        // Mouse entered a bar
        private void OnBarEnter(HBarItem bar, int nIndex)
        {
            nLastVisitedBarIndex = nIndex;
            RaiseHoverInEvent(bar, nIndex);

            SetCurrTooltip(bar);
        }

        // Mouse left a bar
        private void OnBarLeave()
        {
            if (nLastVisitedBarIndex >= 0)
            {
                //HBarItem barEvent;
                //if (bars.GetAt(nLastVisitedBarIndex, out barEvent))
                //{
                    SetCurrTooltip(null);
                    RaiseHoverOutEvent(bars[nLastVisitedBarIndex], nLastVisitedBarIndex);
                    nLastVisitedBarIndex = -1;

                //}
            }
        }

        // Mouse leaving control VISIBLE AREA
        private void OnMouseLeave(object sender, EventArgs e)
        {
            // Calculate 
            Rectangle rectControlWnd = RectangleToScreen(ClientRectangle); 
            /*new Rectangle();
            rectControlWnd.Location = PointToScreen(Location);
            rectControlWnd.Width = this.ClientSize.Width;
            rectControlWnd.Height = this.ClientSize.Height;*/

            if (!rectControlWnd.Contains(Cursor.Position))
            {
                SetCurrTooltip(null);
                OnBarLeave();
            }
        }

        // Mouse clicked on control
        private void OnClick(object sender, MouseEventArgs e)
        {
            int nIndex;

            HBarItem bar = HitTest(e.Location, out nIndex);
            if (bar != null)
            {
                RaiseClickEvent(bar, nIndex);
            }
        }

        // Mouse double click on control
        private void OnDoubleClick(object sender, MouseEventArgs e)
        {
            int nIndex;

            HBarItem bar = HitTest(e.Location, out nIndex);
            if (bar != null)
            {
                RaiseDoubleClickEvent(bar, nIndex);
            }

        }

        #endregion // MouseEvents

        #region overrides and events

        // Update outer shadow if exists
        private void HBarChart_BackColorChanged(object sender, EventArgs e)
        {
            if (shadow != null &&
                (shadow.Mode == CShadowProperty.Modes.Both ||
                shadow.Mode == CShadowProperty.Modes.Outer))
            {
                RedrawChart();
            }
        }
        
        #endregion

    }

    #region Items

    // Each item will present a bar of the bar chart

    // This collection holds all bars. To be a datasource it implements 
    // IList interface.

    #endregion

    #region GUIElements

    // A set of classes for storing GUI elements and properties in BarChart class

    #endregion

    #region EventArgs

    #endregion

    #region Print

    // A print helper class for a bitmap buffer. Landscape or portrate, but
    // does NOT support any angles in between. It does not check for maximum printable
    // pages, which I think might cause spoolsv.exe to fail if overflows.

    #endregion

    #region DataSource

    // This class acts as an interface between chart GUI and Datasource. 
    // DataSource will be controled by the 'data' field of this class. 
    // Chart will be accessed by the 'owner' field of the class. So this
    // class relates 'owner' to 'data'. To do that it uses another class.
    // In fact there's another class in between. A class that impelements 
    // 'IDataConnectionEvents'.
    //
    // Chart(owner) <-> DataSourceManager <-> IDataConnectionEvents
    //                          ^
    //                          |
    //                          v
    //                   CDataConnection
    //
    //
    // The key reason to use 'dataEventHandler' is to be able to add another 
    // way of handling data later on. To add another data interpretor, we need to:
    // 1. Add a new scheme name to 'InterpretSchemes' enum. This class will recieve datasource events.
    // 2. Add a new class that impelements 'IDataConnectionEvents' interface.
    // 3. Instantiate a member of the newly created class(step 2) in 'SetDataInterpreterMode'
    // function of this class.
    //
    // The 'SetDataInterpreterMode' function cause this class to send events 
    // to your class if your class impelemented 'IDataConnectionEvents' interface.
    // It's your responsibility to respond to these events. The way you handle these 
    // events identifys the way the chart handles input data.

    // A class to connect to a datasource, interact with it, retrieve data
    // and recieve events. It sends events to it's member of type
    // IDataConnectionEvents.

    // This class defines behavior of the chart in response to data events

    #endregion
}
