using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;

namespace BarChart
{
    public class CDataConnection
    {
        #region Enums

        public enum DataSourceStates { None, Initializing, Initialized }
        public enum ConnectionStates { None, Initializing, Initialized }

        #endregion

        #region Fields
        
        //underlying data
        private CDataColumnCollection columns;
        private ArrayList rows;

        private int nLastSelectedRowIndex;


        // We need a refrence to owner of this class. Owner should has BindingContext
        UserControl parent;

        // Refrence to a DataManager class that will recieve messages of this class
        private object dataEventHandler;

        // Please note that corrency refers to 'being current' here, not $, Euro, Rials, etc.
        // Maybe they wanted to say concurrency. Anyway my English is not goodenough to decide
        protected CurrencyManager currencyManager = null;

        // Must have for supporting DataSource
        object dataSource;
        string dataMember = String.Empty;

        // For initializable datasources
        private DataSourceStates dataSourceState = DataSourceStates.None;

        // Current initialization state befor connecting to a datasource
        private ConnectionStates connectionState = ConnectionStates.None;
        
        #endregion

        #region Properties

        public object DataSource
        {
            get
            {
                return dataSource;
            }
        }

        public string DataMember
        {
            get
            {
                return dataMember;
            }
        }

        public DataSourceStates DataSourceState
        {
            get { return dataSourceState; }
        }
        
        public ConnectionStates ConnectionState
        {
            get { return connectionState; }
        }

        public CurrencyManager CurrencyManager
        {
            get { return currencyManager; }
        }

        public CDataColumnCollection Columns
        {
            get { return columns; }
        }

        public ArrayList Rows
        {
            get { return rows; }
        }

        public int LastSelectedRowIndex
        {
            get { return nLastSelectedRowIndex; }
        }

        public object DataEventHandler
        {
            get { return dataEventHandler; }
            set { SetEventHandler(value);  }
        }

        #endregion

        #region Methods

        // Will be called uppon changes in datasource or datamember by parent
        // and also in DataSource_Initialized function internally
        public void SetDataSource(object dataSource, string dataMember)
        {
            // Enter only if another initialization is not in progress
            // Any one has time for adding race condition checkout here?
            if (connectionState == ConnectionStates.Initializing) return;
            connectionState = ConnectionStates.Initializing;

            // It is said that some data sources need to be initialized before being used. [OMG]
            ISupportInitializeNotification supportInitialize = this.dataSource as ISupportInitializeNotification;
            if ( supportInitialize != null && dataSourceState == DataSourceStates.Initializing)
            {
                // This function was called by initialize event handler of the datasource
                // so the datasource is probably initialized by now we'll find it out later
                // but befor that we'd better make sure we're not using event any longer
                supportInitialize.Initialized -= new EventHandler(DataSource_Initialized);
            } 


            // Update datasource and member
            if (dataMember == null) dataMember = String.Empty;
            this.dataSource = dataSource;
            this.dataMember = dataMember;

            // Without a BindingContext, how would us have a currencyManager?!
            if (parent.BindingContext == null) return;

            try
            {
                // Stop recieving events from old source if any exists.
                if (currencyManager != null)
                {
                    currencyManager.PositionChanged -= new EventHandler(CurrencyManager_PositionChanged);
                    currencyManager.ListChanged -= new ListChangedEventHandler(CurrencyManager_ListChanged);
                }

                // Update currencyManager if we should
                if (this.dataSource != null && this.dataSource != Convert.DBNull)
                {
                    if (supportInitialize != null && !supportInitialize.IsInitialized)
                    {
                        if (dataSourceState == DataSourceStates.None)
                        {
                            dataSourceState = DataSourceStates.Initializing;
                            supportInitialize.Initialized += new EventHandler(DataSource_Initialized);
                        }
                        // after initialization, this function will be called later and this will be set
                        currencyManager = null;
                    }
                    else
                    {
                        currencyManager = parent.BindingContext[this.dataSource, this.dataMember] as CurrencyManager;
                        IDataConnectionEvents events = dataEventHandler as IDataConnectionEvents;
                        RenewAllData();
                        if (events != null)
                        {
                            events.DataSource_DataBoundCompleted();
                        }
                    }
                }
                else
                {
                    currencyManager = null;
                }

                // I want to recieve all events
                if (currencyManager != null)
                {
                    currencyManager.PositionChanged += new EventHandler(CurrencyManager_PositionChanged);
                    currencyManager.ListChanged += new ListChangedEventHandler(CurrencyManager_ListChanged);
                }
            }
            finally
            {
                connectionState = ConnectionStates.Initialized;
            }
        }

        // If datasource needs to be initialized, this message callback will be added
        // in SetDataSource function and will be called after datasource is finished it's initialization process
        private void DataSource_Initialized(object sender, EventArgs e)
        {
            ISupportInitializeNotification supportInitialize = dataSource as ISupportInitializeNotification;

            if (supportInitialize != null)
            {
                supportInitialize.Initialized -= new EventHandler(DataSource_Initialized);
            }

            dataSourceState = DataSourceStates.Initialized;

            SetDataSource(dataSource, dataMember);
            // we can now inform parent of DatabindingCompleted event
        }

        // This callback is called by CurrencyManager indicating a modification in the data
        private void CurrencyManager_ListChanged(object sender, ListChangedEventArgs e)
        {
            switch(e.ListChangedType)
            {
                case ListChangedType.ItemAdded:
                    AddItem(e.NewIndex);
                    break;

                case ListChangedType.ItemDeleted: 
                    DeleteItem(e.NewIndex);
                    break;

                case ListChangedType.ItemChanged: 
                    UpdateItem(e.NewIndex);
                    break;

                default:
                    // In each of these cases I better reclculate everything
                    
                    // ListChangedType.Reset
                    // ListChangedType.ItemMoved (index changed)
                    // ListChangedType.PropertyDescriptorAdded
                    // ListChangedType.PropertyDescriptorDeleted
                    // ListChangedType.PropertyDescriptorChanged

                    ResetItems();
                    break;
            }
        }

        // This callback is called by CurrencyManager when current row is changed
        private void CurrencyManager_PositionChanged(object sender, EventArgs e)
        {
            OnSelecltedRowChanged();
        }


        /// <summary>
        /// This function gets index of a given property name
        /// </summary>
        /// <param name="dataPropertyName">property name</param>
        /// <returns>index of the given property name</returns>
        public int GetColumnIndex(string dataPropertyName)
        {
            PropertyDescriptorCollection props = currencyManager.GetItemProperties();
            if (props == null) return -1;

            int ret = -1;
            for (int i = 0; i < props.Count; i++)
            {
                if (String.Compare(props[i].Name, dataPropertyName, true, CultureInfo.InvariantCulture) == 0)
                {
                    ret = i;
                    break;
                }
            }

            return ret;
        }
 


        private void UpdateItem(int itemIndex)
        {
            if (columns == null || columns.Count == 0) RenewAllData();
            if (columns == null || columns.Count == 0) return;
            if (rows == null || rows.Count == 0) return;

            // add a row to our rows
            PropertyDescriptorCollection props = currencyManager.GetItemProperties();
            if (props == null) return;

            int nUpdatedColumn = -1;
            int nChanges = 0;
            for (int i = 0; i < columns.Count; i++)
            {
                if (((ArrayList)rows[itemIndex])[i] != props[i].GetValue(currencyManager.List[itemIndex]))
                {
                    ((ArrayList)rows[itemIndex])[i] = props[i].GetValue(currencyManager.List[itemIndex]);
                    nUpdatedColumn = i;
                    nChanges++;
                }
            }

            IDataConnectionEvents events = dataEventHandler as IDataConnectionEvents;
            if (events != null)
            {
                events.DataSource_ItemUpdated(itemIndex, nChanges == 1 ? nUpdatedColumn : -1);
            }
        }

        private void DeleteItem(int itemIndex)
        {
            if (columns == null || columns.Count == 0) RenewAllData();
            if (columns == null || columns.Count == 0) return;
            if (rows == null || rows.Count == 0) return;

            PropertyDescriptorCollection props = currencyManager.GetItemProperties();
            if (props == null) return;
            if (itemIndex >= rows.Count) return;
            
            // Inform parent of the change before deleting the record
            IDataConnectionEvents events = dataEventHandler as IDataConnectionEvents;
            if (events != null)
            {
                events.DataSource_ItemDeleted(itemIndex);
            }

            // Now remove the row
            rows.RemoveAt(itemIndex);
        }

        private void AddItem(int itemIndex)
        {
            if (columns == null || columns.Count == 0) RenewAllData();
            if (columns == null || columns.Count == 0) return;

            // add a row to our rows
            PropertyDescriptorCollection props = currencyManager.GetItemProperties();
            if (props == null) return;

            ArrayList row = new ArrayList(columns.Count);
            for (int i=0;i<columns.Count;i++)
            {
                row.Add(props[i].GetValue(currencyManager.List[itemIndex]));
            }
            rows.Insert(itemIndex, row);
            row = null;

            IDataConnectionEvents events = dataEventHandler as IDataConnectionEvents;
            if (events != null)
            {
                events.DataSource_ItemAdded(itemIndex);
            }
        }

        private void OnSelecltedRowChanged()
        {
            if (nLastSelectedRowIndex != currencyManager.Position)
            {
                ResetColumns();
                ResetRows();
            }

            IDataConnectionEvents events = dataEventHandler as IDataConnectionEvents;
            if (events != null)
            {
                events.DataSource_SelectedRowChanged(currencyManager.Position);
            }

            nLastSelectedRowIndex = currencyManager.Position;
        }

        private void ResetItems()
        {
            RenewAllData();

            IDataConnectionEvents events = dataEventHandler as IDataConnectionEvents;
            if (events != null)
            {
                if (events != null) events.DataSource_ResetItems();
            }
        }

        private void RenewAllData()
        {
            // Update internal data
            ResetColumns();
            ResetRows();
        }

        /// <summary>
        /// WARNING: THIS IS SLOW. Use for few amound of data only
        /// Using current bound data, populates Rows field of this class by rows of the datasource
        /// </summary>
        private void ResetRows()
        {
            rows.Clear();
            //this.props[boundColumnIndex].GetValue(this.currencyManager[rowIndex]);

            if (columns == null || columns.Count == 0) return;
            if (currencyManager == null) return;
            
            PropertyDescriptorCollection props = currencyManager.GetItemProperties();
            if (props == null) return;

            ArrayList row;
            for (int i = 0; i < currencyManager.List.Count; i++)
            {
                row = new ArrayList(columns.Count);

                for (int j = 0; j < columns.Count; j++)
                {
                    row.Add(props[j].GetValue(currencyManager.List[i]));
                }
                rows.Add(row);
            }
        }

        /// <summary>
        /// Using current bound data, populates Columns field of this class by columns of the datasource
        /// </summary>
        private void ResetColumns()
        {
            Columns.Clear();

            CDataColumnItem item = null;

            if (currencyManager == null) return;
            
            PropertyDescriptorCollection props = currencyManager.GetItemProperties();
            if (props == null) return;

            for (int i=0; i<props.Count; i++)
            {
                item = new CDataColumnItem();
                item.BoundIndex = i;
                item.Converter = props[i].Converter;
                item.DisplayName = props[i].DisplayName;
                item.IsReadonly = props[i].IsReadOnly;
                item.Name = props[i].Name;
                item.ValueType = props[i].PropertyType;

                Columns.Add(item);
                item = null;
            }

        }

        #endregion

        #region Constructors/Distructors

        public CDataConnection(UserControl parent, object dataEventHandler) :this()
        {
            this.parent = parent;

            SetEventHandler(dataEventHandler);
        }

        private void SetEventHandler(object dataEventHandler)
        {
            if (this.dataEventHandler != dataEventHandler)
            {
                if (this.dataEventHandler != null)
                {
                    this.dataEventHandler = null;
                }

                this.dataEventHandler = dataEventHandler;
                IDataConnectionEvents eh = dataEventHandler as IDataConnectionEvents;
                if (eh != null)
                {
                    eh.SetData(parent, this);
                }
            }
        }

        public CDataConnection()
        {
            dataEventHandler = null;
            parent = null;
            columns = new CDataColumnCollection();
            rows = new ArrayList();
        }

        public void Dispose()
        {
            if (currencyManager != null)
            {
                currencyManager.PositionChanged -= new EventHandler(CurrencyManager_PositionChanged);
                currencyManager.ListChanged -= new ListChangedEventHandler(CurrencyManager_ListChanged);
            }
            currencyManager = null;

            if (rows != null)
            {
                for (int i=0;i<rows.Count;i++)
                {
                    ((ArrayList)rows[i]).Clear();
                }
                rows.Clear();
            }

            if (columns != null && columns.Count > 0) columns.Clear();
        }

        #endregion
    }
}