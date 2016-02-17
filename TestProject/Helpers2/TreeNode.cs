using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using DecoratorBase = TestProject.Helpers.SerializationFilterDecorator.DecoratorBase;

namespace TestProject.Helpers2
{
    public class TreeNode : IEquatable<TreeNode>, ISerializable
    {
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (isChildsEnumerable)
            {
                var list = new List<object>();
                if (_childNodes.Count != 0)
                {
                    foreach (var item in _childNodes)
                    {
                        list.Add(item.Value);
                    }
                    info.AddValue(_key, list);
                }
                else
                {
                    info.AddValue(_key, _decoratedValue);
                }
            }
            else
            {
                foreach (var item in _childNodes)
                {
                    if (item.Value._childNodes.Count == 0)
                    {
                        info.AddValue(item.Key, item.Value._decoratedValue);
                    }
                    else
                    {
                        if (item.Value.isChildsEnumerable)
                        {
                            var list = new List<object>();
                            foreach (var node in item.Value._childNodes)
                            {
                                if (node.Value._childNodes.Count != 0)
                                {
                                    list.Add(node.Value);
                                }
                                else
                                {
                                    list.Add(node.Value._decoratedValue);
                                }
                            }
                            info.AddValue(item.Key, list.ToArray());
                        }
                        else
                        {
                            info.AddValue(item.Key, item.Value);
                        }
                    }
                }
            }
        }

        public bool isChildsEnumerable;
        public readonly string _key;
        public Type _decoratedType;
        public object _decoratedValue;
        public readonly Dictionary<string, TreeNode> _childNodes;

        public TreeNode(string key, List<TreeNode> childNodes, Type nodeType, object nodeValue)
        {
            var uniqueChilds = childNodes.Distinct().ToArray();
            if (uniqueChilds.Length != childNodes.Count)
            {
                throw new ArgumentException("Child nodes keys was not unique.", "childNodes");
            }

            _key = key;
            _childNodes = uniqueChilds.ToDictionary(child => child._key, child => child);
            _decoratedType = nodeType;
            _decoratedValue = nodeValue;
        }

        public TreeNode(string key, List<TreeNode> childNodes, Type nodeType, object nodeValue, bool isEnumerable)
        {
            var uniqueChilds = childNodes.Distinct().ToArray();
            if (uniqueChilds.Length != childNodes.Count)
            {
                throw new ArgumentException("Child nodes keys was not unique.", "childNodes");
            }

            _key = key;
            _childNodes = uniqueChilds.ToDictionary(child => child._key, child => child);
            _decoratedType = nodeType;
            _decoratedValue = nodeValue;
            isChildsEnumerable = isEnumerable;
        }

        public override bool Equals(object obj)
        {
            var treeNode = obj as TreeNode;
            return treeNode != null && EqualsInner(treeNode);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_key != null ? _key.GetHashCode() : 0) * 397);
            }
        }

        public bool Equals(TreeNode other)
        {
            return EqualsInner(other);
        }

        protected bool EqualsInner(TreeNode other)
        {
            return false;
            //return (other != null) && (ReferenceEquals(this, other) || string.Equals(_key, other._key));
        }
    }
}