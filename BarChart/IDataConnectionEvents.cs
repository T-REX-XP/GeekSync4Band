namespace BarChart
{
    /// <summary>
    /// A data manager should impelement these functions and respond to events by updating chart
    /// </summary>
    internal interface IDataConnectionEvents
    {
        // ListChangedType.ItemChanged. If nColIndex==-1 more than one column changed. Update all row
        void DataSource_ItemUpdated(int nRowIndex, int nColIndex);
        
        // ListChangedType.ItemDeleted 
        void DataSource_ItemDeleted(int nItemIndex);

        // ListChangedType.ItemAdded
        void DataSource_ItemAdded(int nItemIndex);

        // ListChangedType.ItemChanged
        void DataSource_SelectedRowChanged(int nPosition);

        // ListChangedType.Reset
        // ListChangedType.ItemMoved (index changed)
        // ListChangedType.PropertyDescriptorAdded
        // ListChangedType.PropertyDescriptorDeleted
        // ListChangedType.PropertyDescriptorChanged
        void DataSource_ResetItems();

        // Initialization finished successfully
        void DataSource_DataBoundCompleted();

        // Will be called by owner to get refrences to this class
        void SetData(object chart, object dataConnection);
    }
}