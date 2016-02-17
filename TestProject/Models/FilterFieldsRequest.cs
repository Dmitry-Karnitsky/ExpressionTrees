using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Results;

namespace TestProject.Models
{
    public class FilterFieldsRequest
    {
        public const char ParametersSeparator = ',';
        public const char FieldsSeparator = '.';

        private readonly ResponseTree _response;

        public FilterFieldsRequest(string queryString)
        {
            //_root = new TreeNode("RootNode");
            var routes = queryString
                .Split(new[] { ParametersSeparator }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Split(new[] { FieldsSeparator }, StringSplitOptions.RemoveEmptyEntries));

            _response = new ResponseTree(routes);

            //BuildTree(_root, routes);
        }

        //protected void BuildTree(TreeNode root, IEnumerable<IEnumerable<string>> routes)
        //{
        //    foreach (var item in routes.GroupBy(e => e.First()))
        //    {
        //        var node = new TreeNode(item.Key);
        //        root.AttachChild(node);
        //        BuildTree(node, item.Select(e => e.Skip(1)).Where(e => e.Any()));
        //    }
        //}

        protected TreeNode BuildTree(string nodeKey, IEnumerable<IEnumerable<string>> routes)
        {
            var list = routes
                .GroupBy(e => e.First())
                .Select(item => BuildTree(item.Key, item.Select(e => e.Skip(1))
                .Where(e => e.Any())))
                .ToList();
            return new TreeNode(nodeKey, list, null);
        }

        #region PropertiesComparer

        private class PropertiesComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                if (x == null)
                    throw new ArgumentNullException("x");
                if (y == null)
                    throw new ArgumentNullException("y");

                return String.Equals(x.Trim(), y.Trim(), StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(string obj)
            {
                if (obj == null)
                {
                    throw new ArgumentNullException("obj");
                }

                return obj.GetHashCode();
            }
        }

        #endregion
    }



    class Program
    {
        static void Main(string[] args)
        {
            var lines = ExtractHelper.IterateProps(typeof(Container)).ToArray();

            foreach (var line in lines)
                Console.WriteLine(line);

            Console.ReadLine();
        }
    }

    static class ExtractHelper
    {

        public static IEnumerable<string> IterateProps(Type baseType)
        {
            return IteratePropsInner(baseType, baseType.Name);
        }

        private static IEnumerable<string> IteratePropsInner(Type baseType, string baseName)
        {
            var props = baseType.GetProperties();

            foreach (var property in props)
            {
                var name = property.Name;
                var type = ListArgumentOrSelf(property.PropertyType);
                if (IsMarked(type))
                    foreach (var info in IteratePropsInner(type, name))
                        yield return string.Format("{0}.{1}", baseName, info);
                else
                    yield return string.Format("{0}.{1}", baseName, property.Name);
            }
        }

        static bool IsMarked(Type type)
        {
            return type.GetCustomAttributes(typeof(ExtractNameAttribute), true).Any();
        }


        public static Type ListArgumentOrSelf(Type type)
        {
            if (!type.IsGenericType)
                return type;
            if (type.GetGenericTypeDefinition() != typeof(List<>))
                throw new Exception("Only List<T> are allowed");
            return type.GetGenericArguments()[0];
        }
    }

    [ExtractName]
    public class Container
    {
        public string Name { get; set; }
        public List<Address> Addresses { get; set; }
    }

    [ExtractName]
    public class Address
    {
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public List<Telephone> Telephones { get; set; }
    }

    [ExtractName]
    public class Telephone
    {
        public string CellPhone { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = true)]
    public sealed class ExtractNameAttribute : Attribute
    { }
}