using System;

namespace BarChart
{
    public class BarEventArgs : EventArgs
    {
        private int nIndex;
        public int BarIndex
        {
            get { return nIndex; }
            set { nIndex = value; }
        }

        private HBarItem bar;
        public HBarItem Bar
        {
            get { return bar; }
        }

        public BarEventArgs()
        {
            bar = null;
            nIndex = -1;
        }

        public BarEventArgs(HBarItem bar, int nBarIndex)
        {
            this.bar = bar;
            BarIndex = nBarIndex;
        }
    }
}