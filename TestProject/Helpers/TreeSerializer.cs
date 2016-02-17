using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TestProject.Helpers2;

namespace TestProject.Helpers
{
    public class TreeSerializer
    {
        public static object BuildFilteredObjectTree(object instance, Type returnInstanceType, IEnumerable<IEnumerable<string>> routes)
        {
            var rootType = GetUnderlyingTypeIfEnumerable(returnInstanceType) ?? returnInstanceType;

            var enumerable = instance as IEnumerable;
            if (enumerable == null)
            {
                return BuildNode("RootNode", routes, rootType, instance);
            }

            var list = new List<TreeNode>();
            var routesArray = routes as IEnumerable<string>[] ?? routes.ToArray();

            foreach (var item in enumerable)
            {
                list.Add(BuildNode("RootNode", routesArray, rootType, item));
            }

            return list;
        }

        private static TreeNode BuildNode(string nodeKey, IEnumerable<IEnumerable<string>> routes, Type nodeType,
            object value)
        {
            var underlyingType = GetUnderlyingTypeIfEnumerable(nodeType) ?? nodeType;

            if (underlyingType != nodeType)
            {
                var i = 0;
                var enumerableNodes = new List<TreeNode>();
                var enumerable = value as IEnumerable;
                if (enumerable != null)
                {
                    var routesArray = routes as IEnumerable<string>[] ?? routes.ToArray();
                    foreach (var item in enumerable)
                    {
                        var childNodesForEachEnumerableNode = new List<TreeNode>();
                        var properties = underlyingType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).ToArray();

                        foreach (var routeProperty in routesArray.GroupBy(e => e.First()))
                        {
                            var propertyInfo = properties.FirstOrDefault(p => p.Name == routeProperty.Key);
                            if (propertyInfo == null)
                            {
                                continue;
                            }
                            var propertyValue = propertyInfo.GetValue(item);
                            if (propertyValue == null)
                            {
                                continue;
                            }

                            var propertyReturnType = propertyInfo.GetGetMethod().ReturnType;

                            var childNode = BuildNode(routeProperty.Key, routeProperty.Select(e => e.Skip(1)).Where(e => e.Any()), propertyReturnType, propertyValue);

                            childNodesForEachEnumerableNode.Add(childNode);
                        }
                        i++;

                        enumerableNodes.Add(new TreeNode(nodeKey + "_" + i, childNodesForEachEnumerableNode, item));
                    }
                    return new TreeNode(nodeKey, enumerableNodes, value, true);
                }
            }
            else
            {
                var childNodes = new List<TreeNode>();
                var propertyInfos = nodeType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).ToArray();
                foreach (var item in routes.GroupBy(e => e.First()))
                {
                    var propertyInfo = propertyInfos.FirstOrDefault(p => p.Name == item.Key);
                    if (propertyInfo == null)
                    {
                        continue;
                    }
                    var propertyValue = propertyInfo.GetValue(value);
                    if (propertyValue == null)
                    {
                        continue;
                    }
                    var propertyReturnType = propertyInfo.GetGetMethod().ReturnType;

                    var node = BuildNode(item.Key, item.Select(e => e.Skip(1)).Where(e => e.Any()), propertyReturnType, propertyValue);

                    childNodes.Add(node);
                }
                return new TreeNode(nodeKey, childNodes, value);
            }

            throw new Exception();
        }

        protected static Type GetUnderlyingTypeIfEnumerable(Type type)
        {
            if (TypeOfIEnumerable.IsAssignableFrom(type))
            {
                var underlyingType = type.IsGenericType
                    ? type.GetGenericArguments()[0]
                    : TypeOfObject;

                return underlyingType;
            }
            return null;
        }

        #region Static constructor and fields

        static TreeSerializer()
        {
            TypeOfIEnumerable = typeof(IEnumerable);
            TypeOfObject = typeof(object);
        }

        private static readonly Type TypeOfObject;
        private static readonly Type TypeOfIEnumerable;

        #endregion


    }
}