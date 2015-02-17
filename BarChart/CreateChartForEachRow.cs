using System;
using System.Collections;
using System.Drawing;

namespace BarChart
{
    public class CreateChartForEachRow : IDataConnectionEvents
    {
        private CDataConnection data;
        private HBarChart chart;
        private Color[] colors;

        public CreateChartForEachRow()
        {
            data = null;
            chart = null;

            // A set of colors to select a random one from these.
            colors = new Color[] {
                Color.FromArgb(255, 200, 255, 255), 
                Color.FromArgb(255, 150, 200, 255), 
                Color.FromArgb(255, 100, 100, 200),
                Color.FromArgb(255, 255, 60, 130),
                Color.FromArgb(255, 250, 200, 255),
                Color.FromArgb(255, 255, 255, 0),
                Color.FromArgb(255, 255, 155, 55),
                Color.FromArgb(255, 150, 200, 155),
                Color.FromArgb(255, 255, 255, 200),
                Color.FromArgb(255, 100, 150, 200),
                Color.FromArgb(255, 130, 235, 250),
                Color.FromArgb(255, 150, 240, 80)};
        }

        #region IDataConnectionEvents Members

        public void SetData(object chart, object dataConnection)
        {
            this.chart = chart as HBarChart;
            data = dataConnection as CDataConnection;
        }

        public void DataSource_ItemUpdated(int nRowIndex, int nColIndex)
        {
            if (nRowIndex < 0) return;
            if (chart == null) return;
            if (nRowIndex == data.LastSelectedRowIndex)
            {
                // which column changed?
                if (nColIndex < 0)
                {
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        ArrayList row = (ArrayList)data.Rows[nRowIndex];
                        if (row != null && row[i] != null && row[i] != Convert.DBNull)
                        {
                            chart.ModifyAt(i, Convert.ToDouble(row[i]));
                        }
                        else
                        {
                            chart.ModifyAt(i, 0);
                        }
                    }
                }
                else
                {
                    double dValue = Convert.ToDouble(((ArrayList)data.Rows[nRowIndex])[nColIndex]);
                    chart.ModifyAt(nColIndex, dValue);
                }
                chart.RedrawChart();
            }
        }

        public void DataSource_ItemDeleted(int nItemIndex)
        {
            if (nItemIndex < 0) return;
            if (chart == null) return;
            if (nItemIndex == data.LastSelectedRowIndex)
            {
                chart.Items.Clear();
                chart.RedrawChart();
            }
        }

        public void DataSource_ItemAdded(int nItemIndex)
        {
            // Do nothing, unless it changes current row
            if (data.LastSelectedRowIndex == nItemIndex)
            {
                DataSource_ResetItems();
            }
        }

        public void DataSource_SelectedRowChanged(int nPosition)
        {
            if (nPosition < 0) return;
            if (chart == null) return;

            chart.Items.Clear();

            Random r = new Random(1);
            for (int i = 0; i < data.Columns.Count; i++)
            {
                ArrayList row = (ArrayList)data.Rows[nPosition];
                if (row != null && row[i] != null && row[i] != Convert.DBNull)
                {
                    chart.Add(
                        Convert.ToDouble(row[i]),
                        String.IsNullOrEmpty(data.Columns[i].DisplayName) ? data.Columns[i].Name : data.Columns[i].DisplayName,
                        colors[r.Next(0, colors.Length-1)]);
                }
                else
                {
                    chart.Add(null);
                }
            }

            chart.RedrawChart();
        }

        public void DataSource_ResetItems()
        {
            if (data.LastSelectedRowIndex < 0) return;
            if (chart == null) return;

            chart.Items.Clear();

            Random r = new Random(1);
            for (int i = 0; i < data.Columns.Count; i++)
            {
                ArrayList row = (ArrayList)data.Rows[data.LastSelectedRowIndex];
                if (row != null && row[i] != null && row[i] != Convert.DBNull)
                {
                    chart.Add(
                        Convert.ToDouble(row[i]),
                        String.IsNullOrEmpty(data.Columns[i].DisplayName) ? data.Columns[i].Name : data.Columns[i].DisplayName,
                        colors[r.Next(0, colors.Length - 1)]);
                }
                else
                {
                    chart.Add(null);
                }
            }

            chart.RedrawChart();
        }

        public void DataSource_DataBoundCompleted()
        {
            if (chart == null) return;
            
            // Create chart
            DataSource_ResetItems();
        }

        #endregion
    }
}