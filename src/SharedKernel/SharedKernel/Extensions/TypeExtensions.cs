using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LSG.SharedKernel.Extensions
{
    public static class TypeExtensions
    {
        private static readonly IDictionary<Type, PropertyInformation> EntityProperties =
            new ConcurrentDictionary<Type, PropertyInformation>();

        public static PropertyInformation GetPropertyInfo(this Type type)
        {
            if (EntityProperties.TryGetValue(type, out _))
                return EntityProperties[type];

            var properties = type.GetProperties();

            var propertyGetter = properties.ToDictionary(
                property => property.Name,
                property => property.CreateGetter());

            var propertySetter = properties.ToDictionary(
                property => property.Name,
                property => property.CreateSetter());

            EntityProperties[type] = new PropertyInformation(properties, propertyGetter, propertySetter);

            return EntityProperties[type];
        }

        public static Func<object, object> CreateGetter(this PropertyInfo pi)
        {
            if (!pi.CanRead)
            {
                return null;
            }

            if (pi.DeclaringType == null)
            {
                return null;
            }

            var instance = Expression.Parameter(typeof(object), "i");
            var convertP = Expression.TypeAs(instance, pi.DeclaringType);
            var property = Expression.Property(convertP, pi);
            var convert = Expression.TypeAs(property, typeof(object));
            return (Func<object, object>) Expression.Lambda(convert, instance).Compile();
        }

        public static Action<object, object> CreateSetter(this PropertyInfo pi)
        {
            if (!pi.CanWrite)
            {
                return null;
            }

            var instance = Expression.Parameter(typeof(object), "i");
            var value = Expression.Parameter(typeof(object));
            if (pi.DeclaringType == null)
            {
                return null;
            }

            var convertedParam = Expression.Convert(instance, pi.DeclaringType);
            var propExp = Expression.Property(convertedParam, pi.Name);
            var assignExp = Expression.Assign(propExp, Expression.Convert(value, pi.PropertyType));
            return Expression.Lambda<Action<object, object>>(assignExp, instance, value).Compile();
        }
    }

    public class PropertyInformation
    {
        public PropertyInformation(PropertyInfo[] properties, Dictionary<string, Func<object, object>> valueGetter,
            Dictionary<string, Action<object, object>> valueSetter)
        {
            Properties = properties;
            ValueGetterByName = valueGetter;
            ValueSetterByName = valueSetter;
        }

        public PropertyInfo[] Properties { get; }
        public Dictionary<string, Func<object, object>> ValueGetterByName { get; }
        public Dictionary<string, Action<object, object>> ValueSetterByName { get; }
    }
}