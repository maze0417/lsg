using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Elasticsearch.Net;
using LSG.SharedKernel.Logger;
using Microsoft.Extensions.Hosting;
using Serilog.Debugging;
using Serilog.Sinks.Elasticsearch;

namespace LSG.SharedKernel.Elk;

public interface IElkManager
{
    DynamicResponse EmitBatchLogs(LogEventWrapper[] events);

    Task<SearchResult> SearchAsync(SearchRequest request);

    event Action<string> OnError;
}

public sealed class ElkManager : IElkManager
{
    private readonly ElkState _state;

    public ElkManager(IElkConfig elkConfig, IHostEnvironment hostEnvironment)
    {
        var options = new ElasticsearchSinkOptions(elkConfig.Urls)
        {
            AutoRegisterTemplate = false,
            IndexFormat =
                $"{hostEnvironment.ApplicationName}-{hostEnvironment.EnvironmentName}-{DateTime.UtcNow:yyyy-MM-dd}"
        };

        _state = ElkState.Create(options);

        _state.DiscoverClusterVersion();
        _state.RegisterTemplateIfNeeded();
        _state.FailureCallback = e =>
            Console.WriteLine($@"Unable to submit event {e.MessageTemplate}");
        SelfLog.Enable(Console.Error);
    }


    DynamicResponse IElkManager.EmitBatchLogs(LogEventWrapper[] events)
    {
        var result = default(DynamicResponse);
        try
        {
            result = EmitBatchChecked<DynamicResponse>(events);
        }
        catch (Exception ex)
        {
            HandleException(ex, events);
            return result;
        }

        // Handle the results from ES, check if there are any errors.
        if (result.Success && result.Body?["errors"] == true)
        {
            var indexer = 0;
            var items = result.Body?["items"];

            if (items == null) return result;

            foreach (var item in items)
            {
                if (item["index"] != null
                    && HasProperty(item["index"], "error")
                    && item["index"]["error"] != null)
                {
                    var e = events.ElementAt(indexer);
                    if (_state.Options.EmitEventFailure.HasFlag(EmitEventFailureHandling.WriteToSelfLog))
                    {
                        // ES reports an error, output the error to the selflog

                        var error = string.Format(
                            DateTime.UtcNow.ToString("o") + " " +
                            "Failed to store event with template '{0}' into Elasticsearch. Elasticsearch reports for index {1} the following: {2}",
                            e.MessageTemplate, item["index"]["_index"], _state.Serialize(item["index"]["error"]));

                        OnError?.Invoke(error);
                        SelfLog.WriteLine(
                            "Failed to store event with template '{0}' into Elasticsearch. Elasticsearch reports for index {1} the following: {2}",
                            e.MessageTemplate,
                            item["index"]["_index"],
                            _state.Serialize(item["index"]["error"]));
                    }


                    if (_state.Options.EmitEventFailure.HasFlag(EmitEventFailureHandling.RaiseCallback) &&
                        _state.Options.FailureCallback != null)
                        // Send to a failure callback
                        try
                        {
                            _state.FailureCallback?.Invoke(e);
                        }
                        catch (Exception ex)
                        {
                            // We do not let this fail too
                            SelfLog.WriteLine("Caught exception while emitting to callback {1}: {0}", ex,
                                _state.Options.FailureCallback);
                        }
                }

                indexer++;
            }
        }
        else if (result.Success == false && result.OriginalException != null)
        {
            HandleException(result.OriginalException, events);
        }

        return result;
    }

    async Task<SearchResult> IElkManager.SearchAsync(SearchRequest request)
    {
        var json = _state.Serialize(request.Query);

        var result = await
            _state.Client.SearchAsync<DynamicResponse>(request.Index, PostData.String(json));

        // Handle the results from ES, check if there are any errors.
        if (result.Success && result.Body?["errors"] == true)
        {
            var items = result.Body?["items"];
            if (items == null) return null;

            foreach (var item in items)
                if (item["index"] != null
                    && HasProperty(item["index"], "error")
                    && item["index"]["error"] != null)
                {
                    var error = string.Format(
                        DateTime.UtcNow.ToString("o") + " " +
                        "Failed to search index {0} the following: {1}",
                        item["index"]["_index"], _state.Serialize(item["index"]["error"]));

                    OnError?.Invoke(error);
                    SelfLog.WriteLine(error);
                }
        }
        else if
        (
            result
                .Success
            ==
            false
            &&
            result
                .OriginalException
            !=
            null
        )
        {
            throw
                result
                    .OriginalException;
        }


        var response = Encoding.UTF8.GetString(result.ResponseBodyInBytes);
        try
        {
            return
                JsonSerializer.Deserialize<SearchResult>(response);
        }
        catch (Exception e)
        {
            throw new InvalidCastException($"Failed to cast {response},{e.Message} from elk search response");
        }
    }

    public event Action<string> OnError;


    private T EmitBatchChecked<T>(IEnumerable<LogEventWrapper> events)
        where T : class, IElasticsearchResponse, new()
    {
        // ReSharper disable PossibleMultipleEnumeration
        if (events == null || !events.Any())
            return null;
        if (!_state.TemplateRegistrationSuccess &&
            _state.Options.RegisterTemplateFailure == RegisterTemplateRecovery.FailSink)
            return null;

        var payload = new List<string>();
        foreach (var e in events)
        {
            var indexName = _state.GetIndexForEvent(e.Timestamp.ToUniversalTime());
            var action = new { index = new { _index = indexName, _id = e.Timestamp.ToUnixTimeMilliseconds() } };

            var actionJson = _state.Serialize(action);
            payload.Add(actionJson);

            payload.Add(e.LogEventString);
        }

        return _state.Client.Bulk<T>(PostData.MultiJson(payload));
    }

    /// <summary>
    ///     Handles the exceptions.
    /// </summary>
    /// <param name="ex"></param>
    /// <param name="events"></param>
    private void HandleException(Exception ex, IEnumerable<LogEventWrapper> events)
    {
        if (_state.Options.EmitEventFailure.HasFlag(EmitEventFailureHandling.WriteToSelfLog))
            // ES reports an error, output the error to the selflog
            SelfLog.WriteLine("Caught exception while preforming bulk operation to Elasticsearch: {0}", ex);

        if (_state.Options.EmitEventFailure.HasFlag(EmitEventFailureHandling.RaiseCallback) &&
            _state.Options.FailureCallback != null)
            // Send to a failure callback
            try
            {
                foreach (var e in events) _state.FailureCallback?.Invoke(e);
            }
            catch (Exception exCallback)
            {
                // We do not let this fail too
                SelfLog.WriteLine("Caught exception while emitting to callback {1}: {0}", exCallback,
                    _state.Options.FailureCallback);
            }

        if (_state.Options.EmitEventFailure.HasFlag(EmitEventFailureHandling.ThrowException))
            throw ex;
    }

// Helper function: checks if a given dynamic member / dictionary key exists at runtime
    private static bool HasProperty(dynamic settings, string name)
    {
        if (settings is IDictionary<string, object> dictionary) return dictionary.ContainsKey(name);

        if (settings is DynamicObject dy)
            return dy.GetDynamicMemberNames().Contains(name);

        return settings.GetType().GetProperty(name) != null;
    }
}