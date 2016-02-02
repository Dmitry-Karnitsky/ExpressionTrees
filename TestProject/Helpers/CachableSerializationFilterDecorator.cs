using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;

namespace TestProject.Helpers
{
    public class CachableSerializationFilterDecorator : SerializationFilterDecorator
    {
        public void PrepareDecorator(Type instanceType, HashSet<string> propertiesNames)
        {
            if (instanceType == null)
                throw new ArgumentNullException("instanceType");

            var actionArgumentType = GetUnderlyingTypeIfEnumerable(instanceType) ?? instanceType;

            // to avoid caching with wrong parameter when properties names hashset contains
            // contains properties that return object does not contain
            // is it necessary?
            //var allObjectProperties = GetObjectProperties(actionArgumentType, null);
            //var copiedHashSet = new HashSet<string>(propertiesNames);
            //copiedHashSet.IntersectWith(allObjectProperties.Select(p => p.Name));
            //var propertiesNamesKey = String.Join("|", copiedHashSet);

            var keyParams = propertiesNames ?? GetObjectProperties(instanceType, null).Select(p => p.Name);

            var propertiesNamesKey = String.Join("|", keyParams);

            if (!IsInCache(actionArgumentType, propertiesNamesKey))
            {
                var lambda = GetDelegate(actionArgumentType, propertiesNames);
                EnsureKeyInitialized(actionArgumentType);
                Cache[actionArgumentType][propertiesNamesKey] = lambda;
            }
        }

        protected override Action<object, SerializationInfo> GetDelegate(Type instanceType, HashSet<string> propertiesNames)
        {
            //var allObjectProperties = GetObjectProperties(instanceType, null);
            //var copiedHashSet = new HashSet<string>(propertiesNames);
            //copiedHashSet.IntersectWith(allObjectProperties.Select(p => p.Name));
            //var propertiesNamesKey = String.Join("|", copiedHashSet);

            var keyParams = propertiesNames ?? GetObjectProperties(instanceType, null).Select(p => p.Name);
            var propertiesNamesKey = String.Join("|", keyParams);

            if (IsInCache(instanceType, propertiesNamesKey))
            {
                return Cache[instanceType][propertiesNamesKey];
            }

            return base.GetDelegate(instanceType, propertiesNames);
        }

        protected static bool IsInCache(Type instanceType, string propertiesNames)
        {
            if (Cache.ContainsKey(instanceType))
            {
                return Cache[instanceType].ContainsKey(propertiesNames);
            }
            return false;
        }

        protected static void EnsureKeyInitialized(Type keyType)
        {
            lock (keyType)
            {
                if (!Cache.ContainsKey(keyType))
                {
                    Cache[keyType] = new Dictionary<string, Action<object, SerializationInfo>>();
                }
            }
        }

        protected static Dictionary<Type, Dictionary<string, Action<object, SerializationInfo>>> Cache = new Dictionary<Type, Dictionary<string, Action<object, SerializationInfo>>>();
    }
}