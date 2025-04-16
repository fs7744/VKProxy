using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace VKProxy.Core.Infrastructure;

public static class ValueTaskExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task GetAsTask(this in ValueTask<FlushResult> valueTask)
    {
        if (valueTask.IsCompletedSuccessfully)
        {
            valueTask.GetAwaiter().GetResult();
            return Task.CompletedTask;
        }
        else
        {
            return valueTask.AsTask();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask GetAsValueTask(this in ValueTask<FlushResult> valueTask)
    {
        if (valueTask.IsCompletedSuccessfully)
        {
            valueTask.GetAwaiter().GetResult();
            return default;
        }
        else
        {
            return new ValueTask(valueTask.AsTask());
        }
    }
}