using Microsoft.Extensions.Logging;
using Orleans;
using System.Diagnostics;

namespace Sample.Host.App
{
    public class LoggingCallFilter : IIncomingGrainCallFilter
    {
        private readonly ILogger log;
        Stopwatch stopwatch = new Stopwatch();

        public LoggingCallFilter(ILogger<LoggingCallFilter> logger)
        {
            this.log = logger;
        }

        public async Task Invoke(IIncomingGrainCallContext context)
        {
            if (context.InterfaceMethod.Name != "UpdateRuntimeStatistics")
            {
                string arguments = string.Empty;

                if (context.Arguments != null && context.Arguments.Length > 0)
                {
                    arguments = string.Join(", ", context.Arguments);
                }


                try
                {
                    stopwatch.Start();
                    await context.Invoke();
                    stopwatch.Stop();

                    var msg = string.Format(
                        "{0}.{1}({2}) returned value {3} in {4}",
                        context.Grain.GetType(),
                        context.InterfaceMethod.Name,
                        arguments,
                        context.Result,
                        stopwatch.ElapsedMilliseconds
                        );
                    this.log.LogInformation(msg);
                }
                catch (Exception exception)
                {
                    var msg = string.Format(
                        "{0}.{1}({2}) threw an exception: {3}",
                        context.Grain.GetType(),
                        context.InterfaceMethod.Name,
                        arguments,
                        exception);
                    this.log.LogInformation(msg);

                    // If this exception is not re-thrown, it is considered to be
                    // handled by this filter.
                    throw;
                }

                stopwatch.Reset();
            }
        }
    }
}
