using System.Collections;

namespace Sentry.Internal;
// Workaround for the fact that setting a list in config options appends instead of replaces.
// See https://github.com/dotnet/runtime/issues/36569

internal class AutoClearingList<T> : IList<T>
{
    private readonly IList<T> _list = new List<T>();

    private bool _clearOnNextAdd;

    public void Add(T item)
    {
        if (_clearOnNextAdd)
        {
            Clear();
            _clearOnNextAdd = false;
        }

        _list.Add(item);
    }

    public AutoClearingList<T> ClearOnNextAdd()
    {
        _clearOnNextAdd = true;
        return this;
    }

    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_list).GetEnumerator();

    public void Clear() => _list.Clear();

    public bool Contains(T item) => _list.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

    public bool Remove(T item) => _list.Remove(item);

    public int Count => _list.Count;

    public bool IsReadOnly => _list.IsReadOnly;

    public int IndexOf(T item) => _list.IndexOf(item);

    public void Insert(int index, T item) => _list.Insert(index, item);

    public void RemoveAt(int index) => _list.RemoveAt(index);

    public T this[int index]
    {
        get => _list[index];
        set => _list[index] = value;
    }
}
