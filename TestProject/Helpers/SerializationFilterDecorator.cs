using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace TestProject.Helpers
{
    public class SerializationFilterDecorator : SerializationDecorator
    {
        private HashSet<string> _propertiesNames;

        public DecoratorBase GetDecorator(object instance, Type returnType, HashSet<string> propertiesNames)
        {
            _propertiesNames = propertiesNames;
            return base.GetDecorator(instance, returnType);
        }

        public void PrepareDecorator(Type instanceType)
        {
            if (instanceType == null)
                throw new ArgumentNullException("instanceType");

            var actionArgumentType = GetUnderlyingTypeIfEnumerable(instanceType) ?? instanceType;
            var propertiesNamesKey = _propertiesNames != null ? String.Join("|", _propertiesNames) : String.Join("|", GetObjectProperties(actionArgumentType));

            if (!IsInCache(actionArgumentType, propertiesNamesKey))
            {
                var lambda = (Action<object, SerializationInfo>)GetLambdaExpression(actionArgumentType).Compile();
                if (_cache.ContainsKey(actionArgumentType))
                {
                    _cache[actionArgumentType] = new Dictionary<string, Action<object, SerializationInfo>>();
                }
                _cache[actionArgumentType][propertiesNamesKey] = lambda;
            }
        }

        private bool IsInCache(Type instanceType, string propertiesNames)
        {
            if (_cache.ContainsKey(instanceType))
            {
                return _cache[instanceType].ContainsKey(propertiesNames);
            }
            return false;
        }

        protected override IEnumerable<PropertyInfo> GetObjectProperties(Type objectType)
        {
            var properties = base.GetObjectProperties(objectType);
            return _propertiesNames != null ? properties.Where(propertyInfo => _propertiesNames.Contains(propertyInfo.Name)) : properties;
        }

        Dictionary<Type, Dictionary<string, Action<object, SerializationInfo>>> _cache = new Dictionary<Type, Dictionary<string, Action<object, SerializationInfo>>>();
    }
}