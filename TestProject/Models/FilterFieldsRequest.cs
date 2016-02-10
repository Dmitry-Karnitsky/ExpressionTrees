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

        private readonly TreeNode _tree;
        private readonly Dictionary<string, HashSet<string>> _fields;

        public static FilterFieldsRequest CreateRequest(string queryStringFields)
        {
            var queryStringWithoutSpaces = queryStringFields.Replace(" ", string.Empty);
            return new FilterFieldsRequest(queryStringWithoutSpaces);
        }

        private FilterFieldsRequest(string queryStringFields)
        {
            _fields = new Dictionary<string, HashSet<string>>();
            InitializeRequest(queryStringFields);
        }

        private void InitializeRequest(string queryStringFields)
        {
            ExtractFieldsNames(queryStringFields);
        }

        private void ExtractFieldsNames(string queryStringFields)
        {
            var rootNodes = queryStringFields.Split(new[] { ParametersSeparator }, StringSplitOptions.RemoveEmptyEntries);
            Array.Sort(rootNodes, (str1, str2) =>
            {
                var depth1 = str1.Count(ch => ch == FieldsSeparator);
                var depth2 = str2.Count(ch => ch == FieldsSeparator);

                if (depth1 == depth2)
                    return 0;

                if (depth1 < depth2)
                    return 1;

                return -1;
            });

            var paramsFields = new List<string[]>();
            foreach (var item in rootNodes)
            {
                paramsFields.Add(item.Split(new[] { FieldsSeparator }, StringSplitOptions.RemoveEmptyEntries));
            }

            CreateTree(paramsFields);
        }

        private void CreateTree(List<string[]> paramsFields)
        {
            TreeNode<string> currentTree = null;
            foreach (var row in paramsFields)
            {
                for (var i = row.Length; i > 0; i--)
                {
                    var index = i - 1;
                    currentTree = new TreeNode<string>(row[index], currentTree, null, i);
                }
            }
        }




        //public Node(E name, Set array, int depth) {
        //    nodeName = name;
        //    this.depth = depth;
        //    Map map = new HashMap();

        //    for (E[] line : array) { //iterates over arrays
        //        if (line.length > depth) { //checks if an element exists at this depth
        //            E common = line[depth]; //gets an element
        //            Set branch = map.get(common); //gets a branch for the element
        //            if (branch == null) { //if first such an element
        //                branch = new HashSet(); //creates branch
        //                map.put(common, branch); //adds for the element
        //            }
        //            branch.add(line); //adds the line for proper branch
        //        }
        //    }
        //    children = new Node[map.size()];
        //    int i = 0;
        //    depth++;//gets deeper
        //    for (Map.Entry entry : map.entrySet()) {//iterates over map
        //        children[i] = new Node(entry.getKey(), entry.getValue(), depth);//makes child
        //        i++;
        //    }
        //}



        public override bool Equals(object obj)
        {
            var filterFieldsRequest = obj as FilterFieldsRequest;
            return filterFieldsRequest != null && Equals(this, filterFieldsRequest);
        }

        protected bool Equals(FilterFieldsRequest other)
        {
            return Equals(_fields, other._fields);
        }

        public override int GetHashCode()
        {
            return (_fields != null ? _fields.GetHashCode() : 0);
        }

        private static bool Equals(FilterFieldsRequest request1, FilterFieldsRequest request2)
        {
            return false;
        }


        protected class TreeNode<T>
        {
            public T Data { get; private set; }
            public TreeNode<T> FirstChild { get; private set; }
            public TreeNode<T> NextSibling { get; private set; }
            public int Level { get; private set; }


            protected int depth;

            public TreeNode(T data, TreeNode<T> firstChild, TreeNode<T> nextSibling, int level)
            {
                Data = data;
                FirstChild = firstChild;
                NextSibling = nextSibling;
                Level = level;

                if (firstChild == null)
                {
                    depth = 1;
                }
                else
                {
                    depth = firstChild.depth + 1;
                }
            }

            public TreeNode<T> GetNode(int level, T data)
            {
                if (depth < level)
                {
                    return null;
                }

                if()
            }

            private TreeNode<T> GetChildNodes(TreeNode<T> node, int level)
            {
                
            }
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