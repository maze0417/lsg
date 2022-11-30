using System;
using System.Collections.Generic;
using System.Linq;

namespace LSG.SharedKernel.Csv.CsvMapper
{
    public abstract class CsvMapper
    {
        protected abstract Dictionary<string, string> Map { get; }

        public bool IsEmpty()
        {
            return !Map.Any();
        }

        public IEnumerable<string> GetHeaders(Type type)
        {
            return type.GetProperties().Select(propertyInfo => Map.ContainsKey(propertyInfo.Name)
                ? Map[propertyInfo.Name]
                : propertyInfo.Name);
        }
    }
}