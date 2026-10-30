namespace Lira.Toggl
{
    internal readonly record struct CacheItem<T>(T Item, DateTimeOffset Fetched)
    {
        public CacheItem(T item) : this(item, DateTimeOffset.UtcNow)
        {
        }
    }
}
