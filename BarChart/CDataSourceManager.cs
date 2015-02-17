using System.Windows.Forms;

namespace BarChart
{
    public class CDataSourceManager
    {
        #region Fields

        // Refrence to parent for notifying it of events or asking it to reDraw
        private object owner;

        // A data connection object to relate us to chart DataSource ( and it's related stuff like CurrencyManager)
        private CDataConnection data;

        // An object that interpret data of the connected datasourceto respond to it's events and feed chart with true data
        //private object dataEventHandler;

        #endregion

        public object DataSource
        {
            get
            {
                return data.DataSource;
            }
            // Parent calls 'ConnectTo' rather than set
        }

        public string DataMember
        {
            get
            {
                if (data == null) return null;

                return data.DataMember;
            }
            // Parent calls 'ConnectTo' rather than set
        }

        public CDataConnection DataConnection
        {
            get { return data; }
        }

        public object DataEventHandler
        {
            get 
            {
                if (data == null)
                {
                    return null;
                }

                return data.DataEventHandler; 
            }

            set 
            {
                if (data == null)
                {
                    data = new CDataConnection((UserControl)owner, value);
                }
                else
                {
                    data.DataEventHandler = value;
                }
            }
        }

        internal void ConnectTo(object dataSource, string dataMember)
        {
            if (data == null)
            {
                data = new CDataConnection((UserControl)owner, null);
            }

            data.SetDataSource(dataSource, dataMember);
        }

        public CDataSourceManager(HBarChart owner)
        {
            this.owner = owner;
        }
    }
}