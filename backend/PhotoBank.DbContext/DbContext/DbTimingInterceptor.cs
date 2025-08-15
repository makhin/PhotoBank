using System;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace PhotoBank.DbContext.DbContext
{
    public sealed class DbTimingInterceptor : DbCommandInterceptor
    {
        private readonly ILogger<DbTimingInterceptor> _logger;
        private readonly TimeSpan _warn = TimeSpan.FromMilliseconds(500);

        public DbTimingInterceptor(ILogger<DbTimingInterceptor> logger) => _logger = logger;

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            var sw = ValueStopwatch.StartNew();
            var r = await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
            var elapsed = sw.GetElapsedTime();
            if (elapsed > _warn)
                _logger.LogWarning("SLOW-SQL {Elapsed}ms: {CommandText}", (int)elapsed.TotalMilliseconds, command.CommandText);
            return r;
        }

        private readonly struct ValueStopwatch
        {
            private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
            private readonly long _startTimestamp;

            private ValueStopwatch(long startTimestamp) => _startTimestamp = startTimestamp;

            public static ValueStopwatch StartNew() => new ValueStopwatch(Stopwatch.GetTimestamp());

            public TimeSpan GetElapsedTime()
                => TimeSpan.FromTicks((long)((Stopwatch.GetTimestamp() - _startTimestamp) * TimestampToTicks));
        }
    }
}
