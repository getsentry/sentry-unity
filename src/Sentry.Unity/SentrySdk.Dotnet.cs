using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Sentry.Infrastructure;
using Sentry.Protocol.Envelopes;

namespace Sentry.Unity;

public static partial class SentrySdk
{
    /// <summary>
    /// Flushes the queue of captured events until the timeout set in <see cref="SentryOptions.FlushTimeout"/>
    /// is reached.
    /// </summary>
    /// <remarks>
    /// Blocks synchronously. Prefer <see cref="FlushAsync()"/> in async code.
    /// </remarks>
    [DebuggerStepThrough]
    public static void Flush() => Sentry.SentrySdk.Flush();

    /// <summary>
    /// Flushes the queue of captured events until the timeout is reached.
    /// </summary>
    /// <param name="timeout">The amount of time allowed for flushing.</param>
    /// <remarks>
    /// Blocks synchronously. Prefer <see cref="FlushAsync(TimeSpan)"/> in async code.
    /// </remarks>
    [DebuggerStepThrough]
    public static void Flush(TimeSpan timeout) => Sentry.SentrySdk.Flush(timeout);

    /// <summary>
    /// Flushes the queue of captured events until the timeout set in <see cref="SentryOptions.FlushTimeout"/>
    /// is reached.
    /// </summary>
    /// <returns>A task to await for the flush operation.</returns>
    [DebuggerStepThrough]
    public static Task FlushAsync() => Sentry.SentrySdk.FlushAsync();

    /// <summary>
    /// Flushes the queue of captured events until the timeout is reached.
    /// </summary>
    /// <param name="timeout">The amount of time allowed for flushing.</param>
    /// <returns>A task to await for the flush operation.</returns>
    [DebuggerStepThrough]
    public static Task FlushAsync(TimeSpan timeout) => Sentry.SentrySdk.FlushAsync(timeout);

    /// <summary>
    /// Whether the SDK is enabled or not.
    /// </summary>
    public static bool IsEnabled { [DebuggerStepThrough] get => Sentry.SentrySdk.IsEnabled; }

    /// <summary>
    /// Creates a new scope that will terminate when disposed.
    /// </summary>
    /// <remarks>
    /// Pushes a new scope while inheriting the current scope's data.
    /// </remarks>
    /// <param name="state">A state object to be added to the scope.</param>
    /// <returns>A disposable that when disposed, ends the created scope.</returns>
    [DebuggerStepThrough]
    public static IDisposable PushScope<TState>(TState state) => Sentry.SentrySdk.PushScope(state);

    /// <summary>
    /// Creates a new scope that will terminate when disposed.
    /// </summary>
    /// <returns>A disposable that when disposed, ends the created scope.</returns>
    [DebuggerStepThrough]
    public static IDisposable PushScope() => Sentry.SentrySdk.PushScope();

    /// <summary>
    /// Binds the client to the current scope.
    /// </summary>
    /// <param name="client">The client.</param>
    [DebuggerStepThrough]
    public static void BindClient(ISentryClient client) => Sentry.SentrySdk.BindClient(client);

    /// <summary>
    /// Adds a breadcrumb to the current Scope.
    /// </summary>
    /// <param name="message">
    /// If a message is provided it’s rendered as text and the whitespace is preserved.
    /// Very long text might be abbreviated in the UI.</param>
    /// <param name="category">
    /// Categories are dotted strings that indicate what the crumb is or where it comes from.
    /// Typically it’s a module name or a descriptive string.
    /// For instance ui.click could be used to indicate that a click happened in the UI or flask could be used to indicate that the event originated in the Flask framework.
    /// </param>
    /// <param name="type">
    /// The type of breadcrumb.
    /// The default type is default which indicates no specific handling.
    /// Other types are currently http for HTTP requests and navigation for navigation events.
    /// <seealso href="https://develop.sentry.dev/sdk/event-payloads/breadcrumbs/#breadcrumb-types"/>
    /// </param>
    /// <param name="data">
    /// Data associated with this breadcrumb.
    /// Contains a sub-object whose contents depend on the breadcrumb type.
    /// Additional parameters that are unsupported by the type are rendered as a key/value table.
    /// </param>
    /// <param name="level">Breadcrumb level.</param>
    /// <seealso href="https://develop.sentry.dev/sdk/event-payloads/breadcrumbs/"/>
    [DebuggerStepThrough]
    public static void AddBreadcrumb(
        string message,
        string? category = null,
        string? type = null,
        IDictionary<string, string>? data = null,
        BreadcrumbLevel level = default)
        => Sentry.SentrySdk.AddBreadcrumb(message, category, type, data, level);

    /// <summary>
    /// Adds a breadcrumb to the current scope.
    /// </summary>
    /// <remarks>
    /// This overload is intended to be used by integrations only.
    /// The objective is to allow better testability by allowing control of the timestamp set to the breadcrumb.
    /// </remarks>
    /// <param name="clock">An optional <see cref="ISystemClock"/>.</param>
    /// <param name="message">The message.</param>
    /// <param name="category">The category.</param>
    /// <param name="type">The type.</param>
    /// <param name="data">The data.</param>
    /// <param name="level">The level.</param>
    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void AddBreadcrumb(
        ISystemClock? clock,
        string message,
        string? category = null,
        string? type = null,
        IDictionary<string, string>? data = null,
        BreadcrumbLevel level = default)
        => Sentry.SentrySdk.AddBreadcrumb(clock, message, category, type, data, level);

    /// <summary>
    /// Adds a breadcrumb to the current Scope.
    /// </summary>
    /// <param name="breadcrumb">The breadcrumb to be added</param>
    /// <param name="hint">A hint providing additional context that can be used in the BeforeBreadcrumb callback</param>
    /// <see cref="AddBreadcrumb(string, string?, string?, IDictionary{string, string}?, BreadcrumbLevel)"/>
    [DebuggerStepThrough]
    public static void AddBreadcrumb(Breadcrumb breadcrumb, SentryHint? hint = null)
        => Sentry.SentrySdk.AddBreadcrumb(breadcrumb, hint);

    /// <summary>
    /// Configures the scope through the callback.
    /// </summary>
    /// <param name="configureScope">The configure scope callback.</param>
    [DebuggerStepThrough]
    public static void ConfigureScope(Action<Scope> configureScope)
        => Sentry.SentrySdk.ConfigureScope(configureScope);

    /// <summary>
    /// Configures the scope through the callback.
    /// <example>
    /// <code>
    /// object someValue = ...;
    /// SentrySdk.ConfigureScope(static (scope, arg) => scope.SetExtra("key", arg), someValue);
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="configureScope">The configure scope callback.</param>
    /// <param name="arg">The argument to pass to the configure scope callback.</param>
    public static void ConfigureScope<TArg>(Action<Scope, TArg> configureScope, TArg arg)
        => Sentry.SentrySdk.ConfigureScope(configureScope, arg);

    /// <summary>
    /// Configures the scope through the callback asynchronously.
    /// </summary>
    /// <param name="configureScope">The configure scope callback.</param>
    /// <returns>A task that completes when the callback is done or a completed task if the SDK is disabled.</returns>
    [DebuggerStepThrough]
    public static Task ConfigureScopeAsync(Func<Scope, Task> configureScope)
        => Sentry.SentrySdk.ConfigureScopeAsync(configureScope);

    /// <summary>
    /// Configures the scope through the callback asynchronously.
    /// <example>
    /// <code>
    /// object someValue = ...;
    /// SentrySdk.ConfigureScopeAsync(static async (scope, arg) =>
    /// {
    ///     scope.SetExtra("key", arg);
    /// }, someValue);
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="configureScope">The configure scope callback.</param>
    /// <param name="arg">The argument to pass to the configure scope callback.</param>
    /// <returns>A task that completes when the callback is done or a completed task if the SDK is disabled.</returns>
    [DebuggerStepThrough]
    public static Task ConfigureScopeAsync<TArg>(Func<Scope, TArg, Task> configureScope, TArg arg)
        => Sentry.SentrySdk.ConfigureScopeAsync(configureScope, arg);

    /// <summary>
    /// Sets a tag on the current scope.
    /// </summary>
    [DebuggerStepThrough]
    public static void SetTag(string key, string value)
        => Sentry.SentrySdk.SetTag(key, value);

    /// <summary>
    /// Removes a tag from the current scope.
    /// </summary>
    [DebuggerStepThrough]
    public static void UnsetTag(string key)
        => Sentry.SentrySdk.UnsetTag(key);

    /// <inheritdoc cref="ISentryClient.CaptureEnvelope"/>
    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool CaptureEnvelope(Envelope envelope)
        => Sentry.SentrySdk.CaptureEnvelope(envelope);

    /// <summary>
    /// Captures the event, passing a hint, using the specified scope.
    /// </summary>
    /// <param name="evt">The event.</param>
    /// <param name="scope">The scope.</param>
    /// <param name="hint">a hint for the event.</param>
    /// <returns>The Id of the event.</returns>
    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static SentryId CaptureEvent(SentryEvent evt, Scope? scope = null, SentryHint? hint = null)
        => Sentry.SentrySdk.CaptureEvent(evt, scope, hint);

    /// <summary>
    /// Captures an event with a configurable scope.
    /// </summary>
    /// <remarks>
    /// This allows modifying a scope without affecting other events.
    /// </remarks>
    /// <param name="evt">The event.</param>
    /// <param name="configureScope">The callback to configure the scope.</param>
    /// <returns>The Id of the event.</returns>
    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static SentryId CaptureEvent(SentryEvent evt, Action<Scope> configureScope)
        => Sentry.SentrySdk.CaptureEvent(evt, null, configureScope);

    /// <summary>
    /// Captures an event with a configurable scope.
    /// </summary>
    /// <remarks>
    /// This allows modifying a scope without affecting other events.
    /// </remarks>
    /// <param name="evt">The event.</param>
    /// <param name="hint">An optional hint to be provided with the event</param>
    /// <param name="configureScope">The callback to configure the scope.</param>
    /// <returns>The Id of the event.</returns>
    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static SentryId CaptureEvent(SentryEvent evt, SentryHint? hint, Action<Scope> configureScope)
        => Sentry.SentrySdk.CaptureEvent(evt, hint, configureScope);

    /// <summary>
    /// Captures the exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The Id of the event.</returns>
    [DebuggerStepThrough]
    public static SentryId CaptureException(Exception exception)
        => Sentry.SentrySdk.CaptureException(exception);

    /// <summary>
    /// Captures the exception with a configurable scope.
    /// </summary>
    /// <remarks>
    /// This allows modifying a scope without affecting other events.
    /// </remarks>
    /// <param name="exception">The exception.</param>
    /// <param name="configureScope">The callback to configure the scope.</param>
    /// <returns>The Id of the event.</returns>
    [DebuggerStepThrough]
    public static SentryId CaptureException(Exception exception, Action<Scope> configureScope)
        => Sentry.SentrySdk.CaptureException(exception, configureScope);

    /// <summary>
    /// Captures the message.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="level">The message level.</param>
    /// <returns>The Id of the event.</returns>
    [DebuggerStepThrough]
    public static SentryId CaptureMessage(string message, SentryLevel level = SentryLevel.Info)
        => Sentry.SentrySdk.CaptureMessage(message, level);

    /// <summary>
    /// Captures the message with a configurable scope.
    /// </summary>
    /// <remarks>
    /// This allows modifying a scope without affecting other events.
    /// </remarks>
    /// <param name="message">The message to send.</param>
    /// <param name="configureScope">The callback to configure the scope.</param>
    /// <param name="level">The message level.</param>
    /// <returns>The Id of the event.</returns>
    [DebuggerStepThrough]
    public static SentryId CaptureMessage(string message, Action<Scope> configureScope, SentryLevel level = SentryLevel.Info)
        => Sentry.SentrySdk.CaptureMessage(message, configureScope, level);

    /// <summary>
    /// Captures feedback from the user.
    /// </summary>
    [DebuggerStepThrough]
    public static void CaptureFeedback(SentryFeedback feedback, Action<Scope> configureScope, SentryHint? hint = null)
        => Sentry.SentrySdk.CaptureFeedback(feedback, configureScope, hint);

    /// <summary>
    /// Captures feedback from the user.
    /// </summary>
    [DebuggerStepThrough]
    public static void CaptureFeedback(SentryFeedback feedback, Scope? scope = null, SentryHint? hint = null)
        => Sentry.SentrySdk.CaptureFeedback(feedback, scope, hint);

    /// <summary>
    /// Captures feedback from the user.
    /// </summary>
    [DebuggerStepThrough]
    public static void CaptureFeedback(string message, string? contactEmail = null, string? name = null,
        string? replayId = null, string? url = null, SentryId? associatedEventId = null, Scope? scope = null,
        SentryHint? hint = null)
        => Sentry.SentrySdk.CaptureFeedback(new SentryFeedback(message, contactEmail, name, replayId, url, associatedEventId),
            scope, hint);

    /// <summary>
    /// Captures a transaction.
    /// </summary>
    /// <remarks>
    /// Note: this method is NOT meant to be called from user code!
    /// Instead, call <see cref="ISpan.Finish()"/> on the transaction.
    /// </remarks>
    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void CaptureTransaction(SentryTransaction transaction)
        => Sentry.SentrySdk.CaptureTransaction(transaction);

    /// <summary>
    /// Captures a transaction.
    /// </summary>
    /// <remarks>
    /// Note: this method is NOT meant to be called from user code!
    /// Instead, call <see cref="ISpan.Finish()"/> on the transaction.
    /// </remarks>
    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void CaptureTransaction(SentryTransaction transaction, Scope? scope, SentryHint? hint)
        => Sentry.SentrySdk.CaptureTransaction(transaction, scope, hint);

    /// <summary>
    /// Captures a session update.
    /// </summary>
    [DebuggerStepThrough]
    public static void CaptureSession(SessionUpdate sessionUpdate)
        => Sentry.SentrySdk.CaptureSession(sessionUpdate);

    /// <summary>
    /// Captures a check-in.
    /// </summary>
    /// <remarks>
    /// Capturing a check-in returns an ID. The ID can be used to update the status. I.e. to update a check-in you
    /// captured from `CheckInStatus.InProgress` to `CheckInStatus.Ok`.
    /// </remarks>
    /// <param name="monitorSlug">The monitor slug of the check-in.</param>
    /// <param name="status">The status of the check-in.</param>
    /// <param name="sentryId">The <see cref="SentryId"/> associated with the check-in.</param>
    /// <param name="duration">The duration of the check-in.</param>
    /// <param name="scope">The scope of the check-in.</param>
    /// <param name="configureMonitorOptions">The optional monitor config used to create a Check-In programmatically.</param>
    /// <returns>The ID of the check-in.</returns>
    [DebuggerStepThrough]
    public static SentryId CaptureCheckIn(string monitorSlug,
        CheckInStatus status,
        SentryId? sentryId = null,
        TimeSpan? duration = null,
        Scope? scope = null,
        Action<SentryMonitorOptions>? configureMonitorOptions = null)
        => Sentry.SentrySdk.CaptureCheckIn(
            monitorSlug,
            status,
            sentryId,
            duration,
            scope,
            configureMonitorOptions);

    /// <summary>
    /// Starts a transaction.
    /// </summary>
    [DebuggerStepThrough]
    public static ITransactionTracer StartTransaction(
        ITransactionContext context,
        IReadOnlyDictionary<string, object?> customSamplingContext)
        => Sentry.SentrySdk.StartTransaction(context, customSamplingContext);

    /// <summary>
    /// Starts a transaction.
    /// </summary>
    [DebuggerStepThrough]
    internal static ITransactionTracer StartTransaction(
        ITransactionContext context,
        IReadOnlyDictionary<string, object?> customSamplingContext,
        DynamicSamplingContext? dynamicSamplingContext)
        => Sentry.SentrySdk.StartTransaction(context, customSamplingContext, dynamicSamplingContext);

    /// <summary>
    /// Starts a transaction.
    /// </summary>
    [DebuggerStepThrough]
    public static ITransactionTracer StartTransaction(ITransactionContext context)
        => Sentry.SentrySdk.StartTransaction(context);

    /// <summary>
    /// Starts a transaction.
    /// </summary>
    [DebuggerStepThrough]
    public static ITransactionTracer StartTransaction(string name, string operation)
        => Sentry.SentrySdk.StartTransaction(name, operation);

    /// <summary>
    /// Starts a transaction.
    /// </summary>
    [DebuggerStepThrough]
    public static ITransactionTracer StartTransaction(string name, string operation, string? description)
        => Sentry.SentrySdk.StartTransaction(name, operation, description);

    /// <summary>
    /// Starts a transaction.
    /// </summary>
    [DebuggerStepThrough]
    public static ITransactionTracer StartTransaction(string name, string operation, SentryTraceHeader traceHeader)
        => Sentry.SentrySdk.StartTransaction(name, operation, traceHeader);

    /// <summary>
    /// Binds specified exception the specified span.
    /// </summary>
    /// <remarks>
    /// This method is used internally and is not meant for public use.
    /// </remarks>
    [DebuggerStepThrough]
    public static void BindException(Exception exception, ISpan span)
        => Sentry.SentrySdk.BindException(exception, span);

    /// <summary>
    /// Gets the last active span.
    /// </summary>
    [DebuggerStepThrough]
    public static ISpan? GetSpan()
        => Sentry.SentrySdk.GetSpan();

    /// <summary>
    /// Gets the Sentry trace header of the parent that allows tracing across services
    /// </summary>
    [DebuggerStepThrough]
    public static SentryTraceHeader? GetTraceHeader()
        => Sentry.SentrySdk.GetTraceHeader();

    /// <summary>
    /// Gets the Sentry "baggage" header that allows tracing across services
    /// </summary>
    [DebuggerStepThrough]
    public static BaggageHeader? GetBaggage()
        => Sentry.SentrySdk.GetBaggage();

    /// <summary>
    /// Continues a trace based on HTTP header values provided as strings.
    /// </summary>
    /// <remarks>
    /// If no "sentry-trace" header is provided a random trace ID and span ID is created.
    /// </remarks>
    [DebuggerStepThrough]
    public static TransactionContext ContinueTrace(
        string? traceHeader,
        string? baggageHeader,
        string? name = null,
        string? operation = null)
        => Sentry.SentrySdk.ContinueTrace(traceHeader, baggageHeader, name, operation);

    /// <summary>
    /// Continues a trace based on HTTP header values.
    /// </summary>
    /// <remarks>
    /// If no "sentry-trace" header is provided a random trace ID and span ID is created.
    /// </remarks>
    [DebuggerStepThrough]
    public static TransactionContext ContinueTrace(
        SentryTraceHeader? traceHeader,
        BaggageHeader? baggageHeader,
        string? name = null,
        string? operation = null)
        => Sentry.SentrySdk.ContinueTrace(traceHeader, baggageHeader, name, operation);

    /// <inheritdoc cref="IHub.StartSession"/>
    [DebuggerStepThrough]
    public static void StartSession()
        => Sentry.SentrySdk.StartSession();

    /// <inheritdoc cref="IHub.EndSession"/>
    [DebuggerStepThrough]
    public static void EndSession(SessionEndStatus status = SessionEndStatus.Exited)
        => Sentry.SentrySdk.EndSession(status);

    /// <inheritdoc cref="IHub.PauseSession"/>
    [DebuggerStepThrough]
    public static void PauseSession()
        => Sentry.SentrySdk.PauseSession();

    /// <inheritdoc cref="IHub.ResumeSession"/>
    [DebuggerStepThrough]
    public static void ResumeSession()
        => Sentry.SentrySdk.ResumeSession();

    /// <inheritdoc cref="IHub.IsSessionActive"/>
    public static bool IsSessionActive { [DebuggerStepThrough] get => Sentry.SentrySdk.IsSessionActive; }

    /// <summary>
    /// Deliberately crashes an application, which is useful for testing and demonstration purposes.
    /// </summary>
    /// <remarks>
    /// The method is marked obsolete only to discourage accidental misuse.
    /// We do not intend to remove it.
    /// </remarks>
    [Obsolete("WARNING: This method deliberately causes a crash, and should not be used in a real application.")]
    public static void CauseCrash(CrashType crashType) => Sentry.SentrySdk.CauseCrash(crashType);
}
