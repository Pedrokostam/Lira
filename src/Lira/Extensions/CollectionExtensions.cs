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
public static class CollectionExtensions
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
    private static async Task LoadWorklogsImpl(IEnumerable<IssueCommon> issues, LiraClient lira, Func<IssueCommon, LiraClient, Task> functor)
    {
        int parallelism = 20;
        ParallelOptions parallelOptions = new()
        {
            CancellationToken = lira.CancellationTokenSource.Token,
            MaxDegreeOfParallelism = parallelism,
        };
        await Parallel.ForEachAsync(issues.Where(x=>!x.WorklogsLoaded), parallelOptions,
            async (x, token) => await functor(x, lira).ConfigureAwait(false)).ConfigureAwait(false);
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
    public static async Task LoadWorklogs(this IEnumerable<IssueCommon> issues, LiraClient lira)
    {
        await LoadWorklogsImpl(issues, lira, (i, l) => i.LoadWorklogs(l)).ConfigureAwait(false);
    }

    //public static async Task LoadWorklogsRecurse(this IList<Issue> issues, LiraClient lira)
    //{
    //    await LoadWorklogsImpl(issues, lira, (i, l) => i.LoadWorklogsRecurse(l)).ConfigureAwait(false);
    //}

    //public static async Task LoadWorklogs(this IAsyncEnumerable<Issue> issues, Lira lira, ConcurrentBag<Issue> output)
    //{
    //    await LoadWorklogsImpl(issues, lira, (i, l) => i.LoadWorklogs(l.Client),output);
    //}

    //public static async Task LoadWorklogsRecurse(this IAsyncEnumerable<Issue> issues, Lira lira, ConcurrentBag<Issue> output)
    //{
    //    await LoadWorklogsImpl(issues, lira, (i, l) => i.LoadWorklogsRecurse(l.Client),output);
    //}
    public static ImmutableArray<T> ToImmutableArray<T>(this IEnumerable<T> items, int knownCount)
    {
        var builder = ImmutableArray.CreateBuilder<T>(initialCapacity: knownCount);
        builder.AddRange(items);
        return builder.ToImmutableArray();
    }

}
