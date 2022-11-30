using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace LSG.SharedKernel.AppConfig
{
    public class BooleanConverterFromInt : BooleanConverter
    {
        private static readonly IReadOnlyDictionary<string, bool> AllowedValues =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                {"0", false},
                {"1", true}
            };

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return AllowedValues.TryGetValue(value?.ToString() ?? string.Empty, out var result)
                ? result
                : base.ConvertFrom(context, culture, value);
        }
    }
}