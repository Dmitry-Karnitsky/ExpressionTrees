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
        public static object BuildFilteredObjectTree(object instance, Type instanceType, IEnumerable<IEnumerable<string>> routes, out Type filteredObjectType)
        {
            if (instanceType == null)
                throw new ArgumentNullException("instanceType");
            if (routes == null)
                throw new ArgumentNullException("routes");

            if (instance == null)
            {
                filteredObjectType = instanceType;
                return null;
            }

            Type underlyingType;
            var isReturnTypeEnumerable = TryGetUnderlyingTypeIfEnumerable(instanceType, out underlyingType);

            if (!isReturnTypeEnumerable)
            {
                filteredObjectType = TypeOfTreeNode;
                return BuildNode("RootNode", routes, instanceType, instance);
            }

            var array = GetRootArray(instance, routes, underlyingType);

            if (array != null)
            {
                filteredObjectType = TypeOfTreeNodesArray;
                return array;
            }

            throw new InvalidCastException("Type of instance was IEnumerable but instance was not.");
        }

        private static TreeNode[] GetRootArray(object instance, IEnumerable<IEnumerable<string>> routes, Type underlyingType)
        {
            var routesArray = routes as IEnumerable<string>[] ?? routes.ToArray();

            var list = instance as IList;
            if (list != null)
            {
                var count = list.Count;
                var items = new TreeNode[count];
                for (var i = 0; i < count; i++)
                {
                    items[i] = BuildNode("RootNode", routesArray, underlyingType, list[i]);
                }
                return items;
            }

            var enumerable = instance as IEnumerable;
            if (enumerable != null)
            {
                var collection = instance as ICollection;
                var items = collection != null ? new List<TreeNode>(collection.Count) : new List<TreeNode>();

                foreach (var item in enumerable)
                {
                    items.Add(BuildNode("RootNode", routesArray, underlyingType, item));
                }

                return items.ToArray();
            }

            return null;
        }

        private static TreeNode BuildNode(string nodeKey, IEnumerable<IEnumerable<string>> routes, Type nodeType, object value)
        {
            Type underlyingType;
            var isReturnTypeEnumerable = TryGetUnderlyingTypeIfEnumerable(nodeType, out underlyingType);

            if (isReturnTypeEnumerable)
            {
                var routesArray = routes as IEnumerable<string>[] ?? routes.ToArray();
                var list = value as IList;
                if (list != null)
                {
                    var count = list.Count;
                    var enumerableNodes = new TreeNode[count];
                    
                    for (var i = 0; i < count; i++)
                    {
                        var childNodesForEachEnumerableNode = GetChildNodes(underlyingType, list[i], routesArray);
                        enumerableNodes[i] = new TreeNode(nodeKey + i, childNodesForEachEnumerableNode, list[i]);
                    }
                    return new TreeNode(nodeKey, enumerableNodes, value, true);
                }
                var enumerable = value as IEnumerable;
                if (enumerable != null)
                {
                    var collection = value as ICollection;
                    var i = 0;
                    var enumerableNodes = collection != null ? new List<TreeNode>(collection.Count) : new List<TreeNode>();
                    foreach (var item in enumerable)
                    {
                        var childNodesForEachEnumerableNode = GetChildNodes(underlyingType, item, routesArray);
                        enumerableNodes.Add(new TreeNode(nodeKey + i, childNodesForEachEnumerableNode, item));
                        i++;
                    }
                    return new TreeNode(nodeKey, enumerableNodes, value, true);
                }
            }
            else
            {
                var childNodes = GetChildNodes(nodeType, value, routes);
                return new TreeNode(nodeKey, childNodes, value);
            }

            throw new Exception();
        }

        private static List<TreeNode> GetChildNodes(Type nodeType, object value, IEnumerable<IEnumerable<string>> routes)
        {
            var childNodes = new List<TreeNode>();
            if (value == null)
            {
                return childNodes;
            }
            var propertyInfos = nodeType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(p => p.Name, p => p);
            foreach (var item in routes.GroupBy(e => e.First()))
            {
                PropertyInfo propertyInfo;
                if (!propertyInfos.TryGetValue(item.Key, out propertyInfo))
                {
                    continue;
                }

                var getter = propertyInfo.GetGetMethod(false);
                if (getter == null || getter.GetParameters().Length != 0)
                {
                    continue;
                }

                var propertyValue = getter.Invoke(value, null);
                var propertyReturnType = getter.ReturnType;

                var node = BuildNode(item.Key, item.Select(e => e.Skip(1)).Where(e => e.Any()), propertyReturnType, propertyValue);
                childNodes.Add(node);
            }
            return childNodes;
        }

        private static bool TryGetUnderlyingTypeIfEnumerable(Type type, out Type underlyingType)
        {
            if (type.IsGenericType && TypeOfIEnumerable.IsAssignableFrom(type))
            {
                underlyingType = type.IsGenericType
                    ? type.GetGenericArguments()[0]
                    : TypeOfObject;

                return true;
            }

            underlyingType = null;
            return false;
        }

        #region Static constructor and fields

        static TreeSerializer()
        {
            TypeOfIEnumerable = typeof(IEnumerable);
            TypeOfObject = typeof(object);
            TypeOfTreeNode = typeof(TreeNode);
            TypeOfTreeNodesArray = typeof(TreeNode[]);
        }

        private static readonly Type TypeOfObject;
        private static readonly Type TypeOfIEnumerable;
        private static readonly Type TypeOfTreeNodesArray;
        private static readonly Type TypeOfTreeNode;

        #endregion
    }
}