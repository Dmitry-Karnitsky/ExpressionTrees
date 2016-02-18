using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace TestProject.Models
{
    public class TreeNode : IEquatable<TreeNode>, ISerializable
    {
        private readonly string _key;
        private readonly Dictionary<string, TreeNode> _childNodes;

        private Type _decoratedType;

        public TreeNode(string key)
            : this(key, new List<TreeNode>(), null)
        {
        }

        public TreeNode(string key, List<TreeNode> childNodes, Type decoratedType)
        {
            var uniqueChilds = childNodes.Distinct().ToArray();
            if (uniqueChilds.Length != childNodes.Count)
            {
                throw new ArgumentException("Child nodes keys was not unique.", "childNodes");
            }

            _key = key;
            _decoratedType = decoratedType;
            _childNodes = uniqueChilds.ToDictionary(node => node.Key, node => node);

            if (_decoratedType != null)
            {
                InitializeDecorator();
            }
        }

        protected TreeNode(TreeNode treeNode)
        {
            _key = treeNode._key;
            _childNodes = treeNode._childNodes;
        }

        public IEnumerable<TreeNode> ChildNodes
        {
            get { return _childNodes.Values; }
        }

        public Type DecoratedType
        {
            get { return _decoratedType;  }
            set
            {
                _decoratedType = value;

                if (_decoratedType != null)
                {
                    InitializeDecorator();
                }
            }
        }

        public string Key
        {
            get { return _key; }
        }

        public TreeNode GetChildNode(string key)
        {
            TreeNode retVal;
            _childNodes.TryGetValue(key, out retVal);

            return retVal;
        }

        private void InitializeDecorator()
        {
            //_decorator = null;
        }

        #region Serialization implementation

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            
        }

        protected void FillSerializationInfo(SerializationInfo info)
        {
            
        }

        #endregion

        #region Equals members

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