using System;
using System.Threading.Tasks;

namespace PhotoBank.Services;

public static class RetryHelper
{
    public static async Task<T> RetryAsync<T>(
        Func<Task<T>> action,
        int attempts,
        TimeSpan delay,
        Func<Exception, bool> shouldRetry)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        if (shouldRetry is null) throw new ArgumentNullException(nameof(shouldRetry));

        var currentDelay = delay;

        for (var tryNo = 1; ; tryNo++)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (Exception ex) when (tryNo < attempts && shouldRetry(ex))
            {
            }

            await Task.Delay(currentDelay).ConfigureAwait(false);
            var nextDelay = Math.Min(currentDelay.TotalMilliseconds * 2, 4000);
            currentDelay = TimeSpan.FromMilliseconds(nextDelay);
        }
    }
}
