using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class ConcurrentList<T> : IList<T>, IList
{
    private readonly List<T> underlyingList = new List<T>();
    private readonly ConcurrentQueue<T> underlyingQueue;
    private bool isDirty;
    private bool requiresSync;

    public ConcurrentList()
    {
        underlyingQueue = new ConcurrentQueue<T>();
    }

    public ConcurrentList(IEnumerable<T> items)
    {
        underlyingQueue = new ConcurrentQueue<T>(items);
    }

    public T this[int index]
    {
        get
        {
            lock (SyncRoot)
            {
                UpdateLists();
                return underlyingList[index];
            }
        }
        set
        {
            lock (SyncRoot)
            {
                UpdateLists();
                underlyingList[index] = value;
            }
        }
    }

    public int Add(object value)
    {
        if (requiresSync)
            lock (SyncRoot)
                underlyingQueue.Enqueue((T) value);
        else
            underlyingQueue.Enqueue((T) value);
        isDirty = true;
        lock (SyncRoot)
        {
            UpdateLists();
            return underlyingList.IndexOf((T) value);
        }
    }

    public bool Contains(object value)
    {
        lock (SyncRoot)
        {
            UpdateLists();
            return underlyingList.Contains((T) value);
        }
    }

    public int IndexOf(object value)
    {
        lock (SyncRoot)
        {
            UpdateLists();
            return underlyingList.IndexOf((T) value);
        }
    }

    public void Insert(int index, object value)
    {
        lock (SyncRoot)
        {
            UpdateLists();
            underlyingList.Insert(index, (T) value);
        }
    }

    public void Remove(object value)
    {
        lock (SyncRoot)
        {
            UpdateLists();
            underlyingList.Remove((T) value);
        }
    }

    object IList.this[int index]
    {
        get { return ((IList<T>) this)[index]; }
        set { ((IList<T>) this)[index] = (T) value; }
    }

    public bool IsFixedSize
    {
        get { return false; }
    }

    public void CopyTo(Array array, int index)
    {
        lock (SyncRoot)
        {
            UpdateLists();
            underlyingList.CopyTo((T[]) array, index);
        }
    }

    public object SyncRoot { get; } = new object();

    public bool IsSynchronized
    {
        get { return true; }
    }

    public IEnumerator<T> GetEnumerator()
    {
        lock (SyncRoot)
        {
            UpdateLists();
            return underlyingList.GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(T item)
    {
        if (requiresSync)
            lock (SyncRoot)
                underlyingQueue.Enqueue(item);
        else
            underlyingQueue.Enqueue(item);
        isDirty = true;
    }

    public void RemoveAt(int index)
    {
        lock (SyncRoot)
        {
            UpdateLists();
            underlyingList.RemoveAt(index);
        }
    }

    T IList<T>.this[int index]
    {
        get
        {
            lock (SyncRoot)
            {
                UpdateLists();
                return underlyingList[index];
            }
        }
        set
        {
            lock (SyncRoot)
            {
                UpdateLists();
                underlyingList[index] = value;
            }
        }
    }

    public bool IsReadOnly
    {
        get { return false; }
    }

    public void Clear()
    {
        lock (SyncRoot)
        {
            UpdateLists();
            underlyingList.Clear();
        }
    }

    public bool Contains(T item)
    {
        lock (SyncRoot)
        {
            UpdateLists();
            return underlyingList.Contains(item);
        }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        lock (SyncRoot)
        {
            UpdateLists();
            underlyingList.CopyTo(array, arrayIndex);
        }
    }

    public bool Remove(T item)
    {
        lock (SyncRoot)
        {
            UpdateLists();
            return underlyingList.Remove(item);
        }
    }

    public int Count
    {
        get
        {
            lock (SyncRoot)
            {
                UpdateLists();
                return underlyingList.Count;
            }
        }
    }

    public int IndexOf(T item)
    {
        lock (SyncRoot)
        {
            UpdateLists();
            return underlyingList.IndexOf(item);
        }
    }

    public void Insert(int index, T item)
    {
        lock (SyncRoot)
        {
            UpdateLists();
            underlyingList.Insert(index, item);
        }
    }

    private void UpdateLists()
    {
        if (!isDirty)
            return;
        lock (SyncRoot)
        {
            requiresSync = true;
            T temp;
            while (underlyingQueue.TryDequeue(out temp))
                underlyingList.Add(temp);
            requiresSync = false;
        }
    }

    public void AddRange(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            Add(item);
        }
    }

    public List<T> CopyToList()
    {
        return new List<T>(this);
    }
}