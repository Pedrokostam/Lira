using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lira.Objects;

namespace Lira.Extensions;
internal static class CollectionExtensions
{
    public static ImmutableList<T> AddRangeNotNull<T>(this ImmutableList<T> list, IEnumerable<T>? range)
    {
        if (range is null)
        {
            return list;
        }
        return list.AddRange(range);
    }
    public static ImmutableArray<T> AddRangeNotNull<T>(this ImmutableArray<T> list, IEnumerable<T>? range)
    {
        if (range is null)
        {
            return list;
        }
        return list.AddRange(range);
    }
#if NETSTANDARD2_0
    private static IEnumerable<T> GetChunk<T>(IEnumerator<T> enumerator, int chunkSize)
    {
        var count = 0;

        do
        {
            yield return enumerator.Current;
            count++;
        } while (count < chunkSize && enumerator.MoveNext());
    }
    public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (chunkSize <= 0)
            throw new ArgumentException("Chunk size must be greater than 0.", nameof(chunkSize));

        using var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            yield return GetChunk(enumerator, chunkSize).ToList();
        }
    }
#endif
    private static async Task LoadWorklogsImpl(IEnumerable<Issue> issues, LiraClient lira, Func<Issue, LiraClient, Task> functor)
    {
        int parallelism = 20;
#if NETSTANDARD2_0
        // not exactly the same as ForEachAsync - we split the work once at the start.
        // If one of the elements in a chunk lags, we won't be able to move to a new chunk.
        // But still better than calling it 1 by 1
        foreach (var chunk in issues.Chunk(parallelism))
        {
            await Task.WhenAll(chunk.Select(x => x.LoadWorklogs(lira))).ConfigureAwait(false);
        }
#else
        ParallelOptions parallelOptions = new()
        {
            CancellationToken = lira.CancellationTokenSource.Token,
            MaxDegreeOfParallelism = parallelism,
        };
        await Parallel.ForEachAsync(issues, parallelOptions,
            async (x, token) => await functor(x, lira).ConfigureAwait(false)).ConfigureAwait(false);
#endif
    }

    //    private static async Task LoadWorklogsImpl(IAsyncEnumerable<Issue> issues, Lira lira, Func<Issue,Lira,Task> functor, ConcurrentBag<Issue>? output=null)
    //    {
    //        int parallelism = 20;
    //#if NETSTANDARD2_0
    //        //// not exactly the same as ForEachAsync - we split the work once at the start.
    //        //// If one of the elements in a chunk lags, we won't be able to move to a new chunk.
    //        //// But still better than calling it 1 by 1
    //        //foreach (var chunk in issues.Chunk(parallelism))
    //        //{
    //        //    await Task.WhenAll(chunk.Select(x => x.LoadWorklogs(lira.Client)));
    //        //}
    //        await Task.CompletedTask;
    //#else
    //        ParallelOptions parallelOptions = new()
    //        {
    //            CancellationToken = lira.CancellationTokenSource.Token,
    //            MaxDegreeOfParallelism = parallelism,
    //        };
    //        await Parallel.ForEachAsync(issues, parallelOptions,
    //            async (x, token) =>
    //            {
    //                await functor(x, lira);
    //                output?.Add(x);
    //            });

    //#endif
    //    }
    public static async Task LoadWorklogs(this IList<Issue> issues, LiraClient lira)
    {
        await LoadWorklogsImpl(issues, lira, (i, l) => i.LoadWorklogs(l)).ConfigureAwait(false);
    }

    public static async Task LoadWorklogsRecurse(this IList<Issue> issues, LiraClient lira)
    {
        await LoadWorklogsImpl(issues, lira, (i, l) => i.LoadWorklogsRecurse(l)).ConfigureAwait(false);
    }

    //public static async Task LoadWorklogs(this IAsyncEnumerable<Issue> issues, Lira lira, ConcurrentBag<Issue> output)
    //{
    //    await LoadWorklogsImpl(issues, lira, (i, l) => i.LoadWorklogs(l.Client),output);
    //}

    //public static async Task LoadWorklogsRecurse(this IAsyncEnumerable<Issue> issues, Lira lira, ConcurrentBag<Issue> output)
    //{
    //    await LoadWorklogsImpl(issues, lira, (i, l) => i.LoadWorklogsRecurse(l.Client),output);
    //}
}
