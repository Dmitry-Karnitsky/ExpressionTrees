using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TestProject.Models
{
    public class ResponseTree : IEnumerable<TreeNode>
    {
        private readonly TreeNode _root;

        public ResponseTree(IEnumerable<IEnumerable<string>> routes)
        {
            _root = BuildTree("RootNode", routes);
        }

        private TreeNode BuildTree(string nodeKey, IEnumerable<IEnumerable<string>> routes)
        {
            return BuildTreeInternal(nodeKey, routes);
        }

        protected virtual TreeNode BuildTreeInternal(string nodeKey, IEnumerable<IEnumerable<string>> routes)
        {
            var list = routes
                .GroupBy(e => e.First())
                .Select(item =>
                    BuildTreeInternal(item.Key, item
                        .Select(e => e.Skip(1))
                        .Where(e => e.Any())))
                .ToList();
            return new TreeNode(nodeKey, list, null);
        }

        public IEnumerator<TreeNode> GetEnumerator()
        {
            return TraverseTree().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected virtual IEnumerable<TreeNode> TraverseTree()
        {
            return GoReverse(_root);
        }

        private static IEnumerable<TreeNode> GoReverse(TreeNode root)
        {
            if (root != null)
            {
                foreach (var treeNode in root.ChildNodes)
                {
                    foreach (var node in GoReverse(treeNode))
                    {
                        yield return node;
                    }
                }

                yield return root;
            }
        }
    }
}