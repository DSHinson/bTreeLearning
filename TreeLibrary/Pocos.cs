using System.Xml.Linq;

namespace TreeLibrary
{
    public readonly record struct LeafData<T>(int Id, T Value);

    public abstract class BTreeNode<T>
    {
        public int Degree { get; set; } = 3;
    }

    public class LeafNode<T> : BTreeNode<T>
    {
        public List<LeafData<T>> Leaves { get; } = new();

        public LeafNode<T>? Next { get; set; }
        public LeafNode<T>? Previous { get; set; }
    }

    public class InternalNode<T> : BTreeNode<T>
    {
        // Keys used for routing
        public List<int> Keys { get; } = new();

        // Children are either LeafNode<T> or InternalNode<T>
        public List<BTreeNode<T>> Children { get; } = new();
    }
}
