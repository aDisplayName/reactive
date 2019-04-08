﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        public static ValueTask<TSource> FirstOrDefaultAsync<TSource>(this IAsyncEnumerable<TSource> source, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));

            return Core(source, cancellationToken);

            static async ValueTask<TSource> Core(IAsyncEnumerable<TSource> _source, CancellationToken _cancellationToken)
            {
                var first = await TryGetFirst(_source, _cancellationToken).ConfigureAwait(false);

                return first.HasValue ? first.Value : default;
            }
        }

        public static ValueTask<TSource> FirstOrDefaultAsync<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));

            return Core(source, predicate, cancellationToken);

            static async ValueTask<TSource> Core(IAsyncEnumerable<TSource> _source, Func<TSource, bool> _predicate, CancellationToken _cancellationToken)
            {
                var first = await TryGetFirst(_source, _predicate, _cancellationToken).ConfigureAwait(false);

                return first.HasValue ? first.Value : default;
            }
        }

        internal static ValueTask<TSource> FirstOrDefaultAwaitAsyncCore<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, ValueTask<bool>> predicate, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));

            return Core(source, predicate, cancellationToken);

            static async ValueTask<TSource> Core(IAsyncEnumerable<TSource> _source, Func<TSource, ValueTask<bool>> _predicate, CancellationToken _cancellationToken)
            {
                var first = await TryGetFirst(_source, _predicate, _cancellationToken).ConfigureAwait(false);

                return first.HasValue ? first.Value : default;
            }
        }

#if !NO_DEEP_CANCELLATION
        internal static ValueTask<TSource> FirstOrDefaultAwaitWithCancellationAsyncCore<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask<bool>> predicate, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (predicate == null)
                throw Error.ArgumentNull(nameof(predicate));

            return Core(source, predicate, cancellationToken);

            static async ValueTask<TSource> Core(IAsyncEnumerable<TSource> _source, Func<TSource, CancellationToken, ValueTask<bool>> _predicate, CancellationToken _cancellationToken)
            {
                var first = await TryGetFirst(_source, _predicate, _cancellationToken).ConfigureAwait(false);

                return first.HasValue ? first.Value : default;
            }
        }
#endif

        private static ValueTask<Maybe<TSource>> TryGetFirst<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
        {
            if (source is IList<TSource> list)
            {
                if (list.Count > 0)
                {
                    return new ValueTask<Maybe<TSource>>(new Maybe<TSource>(list[0]));
                }
            }
            else if (source is IAsyncPartition<TSource> p)
            {
                return p.TryGetFirstAsync(cancellationToken);
            }
            else
            {
                return Core(source, cancellationToken);

                static async ValueTask<Maybe<TSource>> Core(IAsyncEnumerable<TSource> _source, CancellationToken _cancellationToken)
                {
                    await using (var e = _source.GetConfiguredAsyncEnumerator(_cancellationToken, false))
                    {
                        if (await e.MoveNextAsync())
                        {
                            return new Maybe<TSource>(e.Current);
                        }
                    }

                    return new Maybe<TSource>();
                }
            }

            return new ValueTask<Maybe<TSource>>(new Maybe<TSource>());
        }

        private static async ValueTask<Maybe<TSource>> TryGetFirst<TSource>(IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate, CancellationToken cancellationToken)
        {
            await using (var e = source.GetConfiguredAsyncEnumerator(cancellationToken, false))
            {
                while (await e.MoveNextAsync())
                {
                    var value = e.Current;

                    if (predicate(value))
                    {
                        return new Maybe<TSource>(value);
                    }
                }
            }

            return new Maybe<TSource>();
        }

        private static async ValueTask<Maybe<TSource>> TryGetFirst<TSource>(IAsyncEnumerable<TSource> source, Func<TSource, ValueTask<bool>> predicate, CancellationToken cancellationToken)
        {
            await using (var e = source.GetConfiguredAsyncEnumerator(cancellationToken, false))
            {
                while (await e.MoveNextAsync())
                {
                    var value = e.Current;

                    if (await predicate(value).ConfigureAwait(false))
                    {
                        return new Maybe<TSource>(value);
                    }
                }
            }

            return new Maybe<TSource>();
        }

#if !NO_DEEP_CANCELLATION
        private static async ValueTask<Maybe<TSource>> TryGetFirst<TSource>(IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask<bool>> predicate, CancellationToken cancellationToken)
        {
            await using (var e = source.GetConfiguredAsyncEnumerator(cancellationToken, false))
            {
                while (await e.MoveNextAsync())
                {
                    var value = e.Current;

                    if (await predicate(value, cancellationToken).ConfigureAwait(false))
                    {
                        return new Maybe<TSource>(value);
                    }
                }
            }

            return new Maybe<TSource>();
        }
#endif
    }
}