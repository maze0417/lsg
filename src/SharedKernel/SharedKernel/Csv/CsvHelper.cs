using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LSG.SharedKernel.Csv
{
    public static class CsvHelper
    {
        public static string ToCsvString<T>(IEnumerable<T> list, CsvMapper.CsvMapper headerMapper = null)
        {
            const string separator = ",";
            var sb = new StringBuilder();

            var type = typeof(T);
            var propertyInfos = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // header line
            var headers = GetHeaders(type, headerMapper);
            sb.AppendLine(string.Join(separator, headers));

            // value lines
            foreach (var datum in list)
            {
                var values = propertyInfos.Select(r => r.GetValue(datum)?.ToString());
                sb.AppendLine(string.Join(separator, values));
            }

            return sb.ToString();
        }

        private static IEnumerable<string> GetHeaders(Type type, CsvMapper.CsvMapper mapper)
        {
            if (mapper == null || mapper.IsEmpty())
                return type.GetProperties().Select(r => r.Name);

            return mapper.GetHeaders(type);
        }
    }
}