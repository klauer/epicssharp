using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Collections;

namespace PBCaGw.Services
{
    public delegate void ConcurrentBagModification<T>(ObservableConcurrentBag<T> bag,T newItem);

    public class ObservableConcurrentBag<T> : IEnumerable<T>
    {
        readonly ConcurrentBag<T> data=new ConcurrentBag<T>();
        public event ConcurrentBagModification<T> BagModified;

        public IEnumerator<T> GetEnumerator()
        {
// ReSharper disable AssignNullToNotNullAttribute
            return data.GetEnumerator();
// ReSharper restore AssignNullToNotNullAttribute
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
// ReSharper disable AssignNullToNotNullAttribute
            return data.GetEnumerator();
// ReSharper restore AssignNullToNotNullAttribute
        }

        public void Add(T item)
        {
            data.Add(item);
            if (BagModified != null)
                BagModified(this, item);
        }

        public void CopyTo(T[] array, int index)
        {
            data.CopyTo(array,index);
        }

        public T[] ToArray()
        {
            return data.ToArray();
        }

        public int Count
        {
            get { return data.Count; }
        }
    }
}
