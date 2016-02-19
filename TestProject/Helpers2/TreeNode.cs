using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace TestProject.Helpers2
{
    public class TreeNode : IEquatable<TreeNode>, ISerializable
    {
        private readonly bool _isChildsEnumerable;
        private readonly string _key;
        private readonly object _decoratedValue;
        private readonly Dictionary<string, TreeNode> _childNodes;

        public TreeNode(string key, IList<TreeNode> childNodes, object nodeValue)
            :this (key, childNodes, nodeValue, false)
        {
        }

        public TreeNode(string key, IList<TreeNode> childNodes, object nodeValue, bool isEnumerable)
        {
            var uniqueChilds = childNodes.Distinct().ToArray();
            if (uniqueChilds.Length != childNodes.Count)
            {
                throw new ArgumentException("Child nodes keys was not unique.", "childNodes");
            }

            _key = key;
            _childNodes = uniqueChilds.ToDictionary(child => child._key, child => child);
            _decoratedValue = nodeValue;
            _isChildsEnumerable = isEnumerable;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (_isChildsEnumerable)
            {
                info.AddValue(_key, _childNodes.Count != 0 ? _childNodes.Values : _decoratedValue);
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
                        if (item.Value._isChildsEnumerable)
                        {
                            var count = item.Value._childNodes.Values.Count;
                            var nodes = item.Value._childNodes.Values;

                            var list = new List<object>(count);
                            foreach (var node in nodes)
                            {
                                list.Add(node._childNodes.Count != 0 ? node : node._decoratedValue);
                            }
                            info.AddValue(item.Key, list);

                            //var count = item.Value._childNodes.Values.Count;
                            //var nodes = new TreeNode[count];
                            //item.Value._childNodes.Values.CopyTo(nodes, 0);

                            //var valuesToSerialize = new object[count];
                            //for (var i = 0; i < count; i++)
                            //{
                            //    valuesToSerialize[i] = nodes[i]._childNodes.Count != 0 ? nodes[i] : nodes[i]._decoratedValue;
                            //}
                            //info.AddValue(item.Key, valuesToSerialize);
                        }
                        else
                        {
                            info.AddValue(item.Key, item.Value);
                        }
                    }
                }
            }
        }

        #region Equals and GetHashCode implementation

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
            return (other != null) && (ReferenceEquals(this, other) || string.Equals(_key, other._key));
        }

        #endregion

    }
}