using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Elasticsearch.Net;
using Elasticsearch.Net.Specification.CatApi;
using Elasticsearch.Net.Specification.IndicesApi;
using LSG.SharedKernel.Logger;
using Serilog.Debugging;
using Serilog.Sinks.Elasticsearch;

namespace LSG.SharedKernel.Elk
{
    internal class ElkState
    {
        public static ElkState Create(ElasticsearchSinkOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return new ElkState(options);
        }

        private readonly ElasticsearchSinkOptions _options;


        private readonly ElasticLowLevelClient _client;

        private readonly bool _registerTemplateOnStartup;
        private readonly string _templateName;
        private readonly string _templateMatchString;
        private static readonly Regex IndexFormatRegex = new Regex(@"^(.*)(?:\{0\:.+\})(.*)$");
        private string _discoveredVersion;

        public string DiscoveredVersion => _discoveredVersion;
        public Action<LogEventWrapper> FailureCallback { get; set; }

        private bool IncludeTypeName =>
            (DiscoveredVersion?.StartsWith("7.") ?? false)
            && _options.AutoRegisterTemplateVersion == AutoRegisterTemplateVersion.ESv6;

        public ElasticsearchSinkOptions Options => _options;
        public IElasticLowLevelClient Client => _client;

        public bool TemplateRegistrationSuccess { get; private set; }

        private ElkState(ElasticsearchSinkOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.IndexFormat)) throw new ArgumentException("options.IndexFormat");
            if (string.IsNullOrWhiteSpace(options.TemplateName)) throw new ArgumentException("options.TemplateName");

            // Strip type argument if ESv7 since multiple types are not supported anymore
            if (options.AutoRegisterTemplateVersion == AutoRegisterTemplateVersion.ESv7)
            {
                options.TypeName = "_doc";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(options.TypeName)) throw new ArgumentException("options.TypeName");
            }

            _templateName = options.TemplateName;
            _templateMatchString = IndexFormatRegex.Replace(options.IndexFormat, @"$1*$2");


            _options = options;


            var configuration =
                new ConnectionConfiguration(options.ConnectionPool, options.Connection, options.Serializer)
                    .RequestTimeout(options.ConnectionTimeout);

            if (options.ModifyConnectionSettings != null)
                configuration = options.ModifyConnectionSettings(configuration);

            configuration.ThrowExceptions();

            _client = new ElasticLowLevelClient(configuration);


            _registerTemplateOnStartup = options.AutoRegisterTemplate;
            TemplateRegistrationSuccess = !_registerTemplateOnStartup;
        }


        public string Serialize(object o)
        {
            return _client.Serializer.SerializeToString(o, formatting: SerializationFormatting.None);
        }

        public string GetIndexForEvent(DateTimeOffset offset)
        {
            if (!TemplateRegistrationSuccess &&
                _options.RegisterTemplateFailure == RegisterTemplateRecovery.IndexToDeadletterIndex)
                return string.Format(_options.DeadLetterIndexName, offset);

            return string.Format(_options.IndexFormat, offset).ToLowerInvariant();
        }


        /// <summary>
        /// Register the elasticsearch index template if the provided options mandate it.
        /// </summary>
        public void RegisterTemplateIfNeeded()
        {
            if (!_registerTemplateOnStartup) return;

            try
            {
                if (!_options.OverwriteTemplate)
                {
                    var templateExistsResponse = _client.Indices.TemplateExistsForAll<VoidResponse>(_templateName,
                        new IndexTemplateExistsRequestParameters()
                        {
                            RequestConfiguration = new RequestConfiguration() {AllowedStatusCodes = new[] {200, 404}}
                        });
                    if (templateExistsResponse.HttpStatusCode == 200)
                    {
                        TemplateRegistrationSuccess = true;

                        return;
                    }
                }

                var result = _client.Indices.PutTemplateForAll<StringResponse>(_templateName, GetTemplatePostData(),
                    new PutIndexTemplateRequestParameters
                    {
                        IncludeTypeName = IncludeTypeName ? true : (bool?) null
                    });

                if (!result.Success)
                {
                    ((IElasticsearchResponse) result).TryGetServerErrorReason(out var serverError);
                    SelfLog.WriteLine("Unable to create the template. {0}", serverError);

                    if (_options.RegisterTemplateFailure == RegisterTemplateRecovery.FailSink)
                        throw new Exception($"Unable to create the template named {_templateName}.",
                            result.OriginalException);

                    TemplateRegistrationSuccess = false;
                }
                else
                    TemplateRegistrationSuccess = true;
            }
            catch (Exception ex)
            {
                TemplateRegistrationSuccess = false;

                SelfLog.WriteLine("Failed to create the template. {0}", ex);

                if (_options.RegisterTemplateFailure == RegisterTemplateRecovery.FailSink)
                    throw;
            }
        }

        private PostData GetTemplatePostData()
        {
            //PostData no longer exposes an implicit cast from object.  Previously it supported that and would inspect the object Type to
            //determine if it it was a literal string to write directly or if it was an object that it needed to serialize.  Now the onus is 
            //on us to tell it what type we are passing otherwise if the user specified the template as a json string it would be serialized again.
            var template = GetTemplateData();
            if (template is string s)
            {
                return PostData.String(s);
            }
            else
            {
                return PostData.Serializable(template);
            }
        }

        private object GetTemplateData()
        {
            if (_options.GetTemplateContent != null)
                return _options.GetTemplateContent();

            var settings = _options.TemplateCustomSettings ?? new Dictionary<string, string>();

            if (!settings.ContainsKey("index.refresh_interval"))
                settings.Add("index.refresh_interval", "5s");

            if (_options.NumberOfShards.HasValue && !settings.ContainsKey("number_of_shards"))
                settings.Add("number_of_shards", _options.NumberOfShards.Value.ToString());

            if (_options.NumberOfReplicas.HasValue && !settings.ContainsKey("number_of_replicas"))
                settings.Add("number_of_replicas", _options.NumberOfReplicas.Value.ToString());

            return ElasticsearchTemplateProvider.GetTemplate(
                _options,
                DiscoveredVersion,
                settings,
                _templateMatchString,
                _options.AutoRegisterTemplateVersion);
        }

        public void DiscoverClusterVersion()
        {
            if (!_options.DetectElasticsearchVersion) return;

            var response = _client.Cat.Nodes<StringResponse>(new CatNodesRequestParameters()
            {
                Headers = new[] {"v"}
            });
            if (!response.Success) return;

            _discoveredVersion = response.Body.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (_discoveredVersion?.StartsWith("7.") ?? false)
                _options.TypeName = "_doc";
        }
    }
}