<Query Kind="Program" />

void Main()
{
    TreeNode _root = new TreeNode("root");
    var testRoutes =  
    new[]
            {
                "Prop1",
                "Prop2.InnerProp1.Field1.Abc",
                "Prop2.InnerProp1.Field2.Def",
                "Prop2.InnerProp2.Field1",
                "Prop2.InnerProp2.Field2",
                "Prop3.Field1.IntVal.Abc",
                "Prop3.Field2.DoubleVal.Def",
                "Prop3.Field2",
                "Prop4",
                "Prop5"
            };
    BuildTree(_root, testRoutes);
    
    _root.Dump();
}

protected void BuildTree(TreeNode root, string[] routes)
{
    if (routes.Length == 0)
    {
        return;
    }

    var grouped = routes.GroupBy(e => e.TrimStart('.').Split('.')[0]).ToDictionary(i => i.Key, i =>
    {
        var keyLength = i.Key.Length;
        return i.Select(e => e.Length == 0 ? e : e.TrimStart('.').Remove(0, keyLength)).Where(k => !string.IsNullOrWhiteSpace(k.Trim('.', ' '))).ToArray();
    });

    foreach (var item in grouped)
    {
        var node = new TreeNode(item.Key);
        root.ChildNodes.Add(node);
        BuildTree(node, item.Value);
    }
}

public class TreeNode
{
    public string Key;
    public List<TreeNode> ChildNodes;

    public TreeNode(string key)
        : this(key, new List<TreeNode>())
    {
    }

    public TreeNode(string key, List<TreeNode> childNodes)
    {
        Key = key;
        ChildNodes = childNodes;
    }
}

// Define other methods and classes here
