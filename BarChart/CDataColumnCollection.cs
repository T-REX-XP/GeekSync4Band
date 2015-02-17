using System.Collections;
using System.Collections.Generic;

namespace BarChart
{
    /// <summary>
    /// CDataColumnCollection is a collection of columns of the data source
    /// </summary>
    public class CDataColumnCollection : IList<CDataColumnItem>
    {
        private List<CDataColumnItem> items;

        public CDataColumnCollection()
        {
            items = new List<CDataColumnItem>();
        }

        #region IList<CDataColumnItem> Members

        public int IndexOf(CDataColumnItem item)
        {
            return items.IndexOf(item);
        }

        public void Insert(int index, CDataColumnItem item)
        {
            items.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            items.RemoveAt(index);
        }

        public CDataColumnItem this[int index]
        {
            get
            {
                return items[index];
            }
            set
            {
                items[index] = value;
            }
        }

        #endregion

        #region ICollection<CDataColumnItem> Members

        public void Add(CDataColumnItem item)
        {
            items.Add(item);
        }

        public void Clear()
        {
            items.Clear();
        }

        public bool Contains(CDataColumnItem item)
        {
            return items.Contains(item);
        }

        public void CopyTo(CDataColumnItem[] array, int arrayIndex)
        {
            items.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return items.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(CDataColumnItem item)
        {
            return items.Remove(item);
        }

        #endregion

        #region IEnumerable<CDataColumnItem> Members

        public IEnumerator<CDataColumnItem> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        #endregion
    }
}