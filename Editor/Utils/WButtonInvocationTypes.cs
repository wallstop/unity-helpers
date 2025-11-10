namespace WallstopStudios.UnityHelpers.Editor.WButton
{
#if UNITY_EDITOR
    using System;
    using System.Threading;
    using UnityEngine;

    internal enum WButtonInvocationStatus
    {
        Running = 0,
        CancelRequested = 1,
        Completed = 2,
        Faulted = 3,
        Cancelled = 4,
    }

    internal enum WButtonResultKind
    {
        Success = 0,
        Error = 1,
        Cancelled = 2,
    }

    internal sealed class WButtonResultEntry
    {
        internal WButtonResultEntry(
            WButtonResultKind kind,
            DateTime timestamp,
            object value,
            string summary,
            UnityEngine.Object objectReference,
            Exception exception = null
        )
        {
            Kind = kind;
            Timestamp = timestamp;
            Value = value;
            Summary = summary ?? string.Empty;
            ObjectReference = objectReference;
            Exception = exception;
        }

        internal WButtonResultKind Kind { get; }

        internal DateTime Timestamp { get; }

        internal object Value { get; }

        internal string Summary { get; }

        internal UnityEngine.Object ObjectReference { get; }

        internal Exception Exception { get; }
    }

    internal sealed class WButtonInvocationHandle
    {
        internal WButtonInvocationHandle(
            WButtonMethodState methodState,
            UnityEngine.Object target,
            WButtonExecutionKind executionKind,
            CancellationTokenSource cancellationTokenSource
        )
        {
            MethodState = methodState;
            Target = target;
            ExecutionKind = executionKind;
            CancellationTokenSource = cancellationTokenSource;
            SupportsCancellation = cancellationTokenSource != null;
            StartedAt = DateTime.UtcNow;
            Status = WButtonInvocationStatus.Running;
        }

        internal WButtonMethodState MethodState { get; }

        internal UnityEngine.Object Target { get; }

        internal WButtonExecutionKind ExecutionKind { get; }

        internal CancellationTokenSource CancellationTokenSource { get; }

        internal bool SupportsCancellation { get; }

        internal WButtonInvocationStatus Status { get; private set; }

        internal Exception Fault { get; private set; }

        internal object AsyncResult { get; private set; }

        internal DateTime StartedAt { get; }

        internal WButtonCoroutineTicket CoroutineTicket { get; set; }

        internal void Cancel()
        {
            if (!SupportsCancellation)
            {
                return;
            }

            if (Status == WButtonInvocationStatus.Running)
            {
                Status = WButtonInvocationStatus.CancelRequested;
            }

            try
            {
                CancellationTokenSource.Cancel();
            }
            catch
            {
                // Ignore cancellation exceptions thrown by disposed sources.
            }
        }

        internal void MarkCompleted(object result)
        {
            AsyncResult = result;
            Status = WButtonInvocationStatus.Completed;
        }

        internal void MarkFaulted(Exception exception)
        {
            Fault = exception;
            Status = WButtonInvocationStatus.Faulted;
        }

        internal void MarkCancelled()
        {
            Status = WButtonInvocationStatus.Cancelled;
        }
    }
#endif
}
