using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace TestProject.Helpers
{
    public class CachableSerializationFilterDecorator : SerializationFilterDecorator
    {
        private const string PropertiesKeySeparator = "|";
        
        public void PrepareDecorator(Type instanceType, HashSet<string> propertiesNames)
        {
            if (instanceType == null)
                throw new ArgumentNullException("instanceType");

            var actionArgumentType = GetUnderlyingTypeIfEnumerable(instanceType) ?? instanceType;

            // to avoid caching with wrong key when properties names hashset
            // contains properties that return object does not contain
            // is it necessary?
            //var allObjectProperties = GetObjectProperties(actionArgumentType);
            //var copiedHashSet = new HashSet<string>(propertiesNames);
            //copiedHashSet.IntersectWith(allObjectProperties.Select(p => p.Name));
            //var propertiesNamesKey = String.Join("|", copiedHashSet);

            var keyParams = propertiesNames ?? GetObjectProperties(instanceType).Select(p => p.Name);

            var propertiesNamesKey = String.Join(PropertiesKeySeparator, keyParams);

            if (!IsInCache(actionArgumentType, propertiesNamesKey))
            {
                var lambda = GetDelegate(actionArgumentType, propertiesNames);
                EnsureKeyInitialized(actionArgumentType);
                cache[actionArgumentType][propertiesNamesKey] = lambda;
            }
        }

        protected override Action<object, SerializationInfo> GetDelegate(Type instanceType, HashSet<string> propertiesNames)
        {
            //var allObjectProperties = GetObjectProperties(instanceType);
            //var copiedHashSet = new HashSet<string>(propertiesNames);
            //copiedHashSet.IntersectWith(allObjectProperties.Select(p => p.Name));
            //var propertiesNamesKey = String.Join("|", copiedHashSet);

            var keyParams = propertiesNames ?? GetObjectProperties(instanceType).Select(p => p.Name);
            var propertiesNamesKey = String.Join(PropertiesKeySeparator, keyParams);

            if (IsInCache(instanceType, propertiesNamesKey))
            {
                return cache[instanceType][propertiesNamesKey];
            }

            return base.GetDelegate(instanceType, propertiesNames);
        }

        protected static bool IsInCache(Type instanceType, string propertiesNames)
        {
            if (cache.ContainsKey(instanceType))
            {
                return cache[instanceType].ContainsKey(propertiesNames);
            }
            return false;
        }

        protected static void EnsureKeyInitialized(Type keyType)
        {
            lock (keyType)
            {
                if (!cache.ContainsKey(keyType))
                {
                    cache[keyType] = new Dictionary<string, Action<object, SerializationInfo>>();
                }
            }
        }

        protected static Dictionary<Type, Dictionary<string, Action<object, SerializationInfo>>> cache = new Dictionary<Type, Dictionary<string, Action<object, SerializationInfo>>>();
    }
}