using System.Collections;
using System.Collections.Concurrent;

namespace NitroCache.Library;

/// <summary>
/// Thread-safe HashSet implementation using ConcurrentDictionary
/// </summary>
/// <typeparam name="T">The type of items in the set</typeparam>
public class ConcurrentHashSet<T> : IEnumerable<T> where T : notnull
{
    private readonly ConcurrentDictionary<T, byte> _dictionary = new();

    /// <summary>
    /// Adds an item to the set
    /// </summary>
    /// <param name="item">The item to add</param>
    /// <returns>True if the item was added, false if it already existed</returns>
    public bool Add(T item)
    {
        return _dictionary.TryAdd(item, 0);
    }

    /// <summary>
    /// Removes an item from the set
    /// </summary>
    /// <param name="item">The item to remove</param>
    /// <returns>True if the item was removed, false if it didn't exist</returns>
    public bool Remove(T item)
    {
        return _dictionary.TryRemove(item, out _);
    }

    /// <summary>
    /// Checks if the set contains an item
    /// </summary>
    /// <param name="item">The item to check</param>
    /// <returns>True if the set contains the item</returns>
    public bool Contains(T item)
    {
        return _dictionary.ContainsKey(item);
    }

    /// <summary>
    /// Gets the number of items in the set
    /// </summary>
    public int Count => _dictionary.Count;

    /// <summary>
    /// Clears all items from the set
    /// </summary>
    public void Clear()
    {
        _dictionary.Clear();
    }

    /// <summary>
    /// Returns an enumerator for the set
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        return _dictionary.Keys.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Converts the set to a list
    /// </summary>
    public List<T> ToList()
    {
        return _dictionary.Keys.ToList();
    }
}
