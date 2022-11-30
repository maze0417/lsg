using System;
using System.Collections;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using static System.Text.Encodings.Web.JavaScriptEncoder;

namespace LSG.SharedKernel.Extensions;

public static class ObjectExtensions
{
    public static string ToJson(this object obj, bool indented = false, bool useJsonStringEnumConverter = false,
        bool unsafeRelaxedJsonEscaping = false)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        if (useJsonStringEnumConverter)
            options.Converters.Add(new JsonStringEnumConverter());

        if (unsafeRelaxedJsonEscaping)
            options.Encoder = UnsafeRelaxedJsonEscaping;
        return JsonSerializer.Serialize(obj, options);
    }

    public static string ToQueryString(this object request, string separator = ",")
    {
        if (request == null)
            return string.Empty;

        // Get all properties on the object

        var propInfo = request.GetType().GetPropertyInfo();

        var properties = propInfo.Properties
            .Where(x => x.CanRead)
            .Where(x => propInfo.ValueGetterByName[x.Name](request) != null)
            .ToDictionary(x =>
            {
                var bindProp = x.GetCustomAttribute<BindPropertyAttribute>();
                return bindProp?.Name ?? x.Name;
            }, x => propInfo.ValueGetterByName[x.Name](request));

        // Get names for all IEnumerable properties (excl. string)
        var propertyNames = properties
            .Where(x => !(x.Value is string) && x.Value is IEnumerable)
            .Select(x => x.Key)
            .ToList();

        // Concat all IEnumerable properties into a comma separated string
        foreach (var key in propertyNames)
        {
            var valueType = properties[key]?.GetType();
            if (valueType == null)
                continue;

            var valueElemType = valueType.IsGenericType
                ? valueType.GetGenericArguments()[0]
                : valueType.GetElementType();
            if (valueElemType == null)
                continue;

            if (valueElemType.IsPrimitive || valueElemType == typeof(string))
            {
                var enumerable = properties[key] as IEnumerable;
                properties[key] = string.Join(",",
                    (enumerable ?? throw new InvalidOperationException()).Cast<object>());
            }
        }

        // Concat all key/value pairs into a string separated by ampersand
        return string.Join("&", properties
            .Select(x => string.Concat(
                Encode(x.Key), "=",
                Encode(x.Value?.ToString()))));

        static string Encode(string data)
        {
            return string.IsNullOrEmpty(data) ? string.Empty : Uri.EscapeDataString(data).Replace("%20", "+");
        }
    }

    public static FormUrlEncodedContent ToFormRequest(this object request)
    {
        if (request == null)
            return null;

        // Get all properties on the object

        var propInfo = request.GetType().GetPropertyInfo();

        var properties = propInfo.Properties
            .Where(x => x.CanRead)
            .Where(x => propInfo.ValueGetterByName[x.Name](request) != null)
            .ToDictionary(x =>
            {
                var bindProp = x.GetCustomAttribute<BindPropertyAttribute>();
                return bindProp?.Name ?? x.Name;
            }, x => propInfo.ValueGetterByName[x.Name](request));

        // Get names for all IEnumerable properties (excl. string)
        var propertyNames = properties
            .Where(x => !(x.Value is string) && x.Value is IEnumerable)
            .Select(x => x.Key)
            .ToList();

        // Concat all IEnumerable properties into a comma separated string
        foreach (var key in propertyNames)
        {
            var valueType = properties[key]?.GetType();
            if (valueType == null)
                continue;

            var valueElemType = valueType.IsGenericType
                ? valueType.GetGenericArguments()[0]
                : valueType.GetElementType();
            if (valueElemType == null)
                continue;

            if (valueElemType.IsPrimitive || valueElemType == typeof(string))
            {
                var enumerable = properties[key] as IEnumerable;
                properties[key] = string.Join(",",
                    (enumerable ?? throw new InvalidOperationException()).Cast<object>());
            }
        }

        // Concat all key/value pairs into a string separated by ampersand

        var dic = properties
            .ToDictionary(k => Encode(k.Key),
                v => Encode(v.Value?.ToString()));


        return new FormUrlEncodedContent(dic);

        static string Encode(string data)
        {
            return string.IsNullOrEmpty(data) ? string.Empty : Uri.EscapeDataString(data).Replace("%20", "+");
        }
    }
}