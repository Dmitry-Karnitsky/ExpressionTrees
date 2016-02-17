using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using TestProject.Helpers2;

namespace TestProject.Helpers
{
    public class TreeSerializer
    {
        public const char ParametersSeparator = ',';
        public const char FieldsSeparator = '.';

        private readonly string _queryString =   "Prop1" + ParametersSeparator + 
                        "Prop2.InnerProp1.Field1.Abc" + ParametersSeparator + 
                        "Prop2.InnerProp1.Field2.Def" + ParametersSeparator + 
                        "Prop2.InnerProp2.Field1" + ParametersSeparator + 
                        "Prop2.InnerProp2.Field2" + ParametersSeparator + 
                        "Prop2.InnerProp3" + ParametersSeparator + 
                        "Prop3.Field1.IntVal.Abc" + ParametersSeparator + 
                        "Prop3.Field1.DoubleVal.Def" + ParametersSeparator + 
                        "Prop3.Field1.DoubleVal.Hkl" + ParametersSeparator + 
                        "Prop3.Field2" + ParametersSeparator + 
                        "Prop4" + ParametersSeparator + 
                        "Prop5";

        public static TreeNode BuildTree(string nodeKey, IEnumerable<IEnumerable<string>> routes, Type nodeType,
            object value)
        {
            var underlyingType = nodeType.IsGenericType ? nodeType.GetGenericArguments()[0] : nodeType; // stub for enumerable

            if (nodeType.IsGenericType)
            {
                var enumerable = value as IEnumerable;
                var listM = new List<TreeNode>();
                var i = 0;
                foreach (var node in enumerable)
                {
                    var list = new List<TreeNode>();
                    var props = underlyingType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).ToArray();

                    foreach (var item in routes.GroupBy(e => e.First()))
                    {
                        var prop = props.FirstOrDefault(p => p.Name == item.Key);
                        if (prop == null)
                            continue;
                        var type = prop.GetGetMethod().ReturnType;
                        var nodeNew = BuildTree(item.Key, item.Select(e => e.Skip(1)).Where(e => e.Any()), type, prop.GetValue(node));
                        list.Add(nodeNew);
                    }
                    i++;
                    listM.Add(new TreeNode(nodeKey + "_" + i, list, underlyingType, node));
                }
                return new TreeNode(nodeKey, listM, nodeType, value, true);
            }
            else
            {
                var list = new List<TreeNode>();
                var props = nodeType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).ToArray();
                foreach (var item in routes.GroupBy(e => e.First()))
                {
                    var prop = props.FirstOrDefault(p => p.Name == item.Key);
                    if (prop == null)
                        continue;
                    var type = prop.GetGetMethod().ReturnType;
                    var node = BuildTree(item.Key, item.Select(e => e.Skip(1)).Where(e => e.Any()), type, prop.GetValue(value));
                    list.Add(node);
                }
                return new TreeNode(nodeKey, list, nodeType, value);
            }
        }


    }
}