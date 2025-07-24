using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeLibrary
{
    public class BTreeService<T>
    {
        private int _nextId = 1;
        private int GetNextId() => _nextId++;
        private readonly int _degree;
        private BTreeNode<T> _root;

        public BTreeNode<T> Root => _root;

        public BTreeService(int degree)
        {
            if (degree < 3)
            {
                throw new ArgumentOutOfRangeException(nameof(degree), "Degree must be >= 3.");
            }

            _degree = degree;
            _root = new LeafNode<T> { Degree = degree }; // start as single leaf
        }


        public void Add(T data)
        {
            int newId = GetNextId();
            if (_root is LeafNode<T> leaf)
            {
                var prevMax = leaf.Leaves.LastOrDefault();
                leaf.Leaves.Add(new LeafData<T>(newId, data));
                leaf.Leaves.Sort((a, b) => a.Id.CompareTo(b.Id));

                if (leaf.Leaves.Count > _degree)
                {
                    var newParent = new InternalNode<T>();
                    newParent.Children.Add(leaf); // leaf is the left child
                    var accessPath = new Stack<InternalNode<T>>();
                    accessPath.Push(newParent);
                    SplitLeaf(leaf, accessPath);   // SplitLeaf will insert the separator key and new right leaf
                    _root = newParent;            // Promote the new parent as root
                }
            }
            else if (_root is InternalNode<T> internalNode)
            {
                var accessPath = new Stack<InternalNode<T>>();
                LeafNode<T> targetLeaf = FindLeafToInsert(internalNode, newId, accessPath);
                InsertIntoLeaf(targetLeaf, new LeafData<T>(newId, data));

                if (targetLeaf.Leaves.Count > _degree)
                {
                    SplitLeaf(targetLeaf, accessPath);

                    // Handle root replacement if needed
                    if (accessPath.Count == 0 && internalNode.Children.Count > _degree)
                    {
                        var newRoot = new InternalNode<T>();
                        newRoot.Children.Add(_root);
                        SplitInternal((InternalNode<T>)_root, newRoot);
                        _root = newRoot;
                    }
                }
            }
            else 
            {
                throw new Exception("Unsupported node type");
            }
        }

        public int GetDepth()
        {
            return GetDepth(_root);
        }

        private int GetDepth(BTreeNode<T> node)
        {
            if (node is LeafNode<T>)
                return 1;

            if (node is InternalNode<T> internalNode && internalNode.Children.Count > 0)
                return 1 + GetDepth(internalNode.Children[0]); // All children have the same depth

            return 1; // Default fallback (shouldn’t happen)
        }

        private void InsertIntoLeaf(LeafNode<T> leaf, LeafData<T> data)
        {
            leaf.Leaves.Add(data);
            leaf.Leaves.Sort((a, b) => a.Id.CompareTo(b.Id));
        }

        private LeafNode<T> FindLeafToInsert(InternalNode<T> node, int id, Stack<InternalNode<T>> accessPath)
        {
            var current = node;

            while (true)
            {
                accessPath.Push(current);

                // Binary search to find correct child
                int i = current.Keys.FindIndex(k => id < k);
                if (i == -1 || i == current.Keys.Count)
                {
                    i = current.Children.Count - 1;
                }

                var child = current.Children[i];
                if (child is LeafNode<T> leaf)
                {
                    return leaf;
                }
                current = (InternalNode<T>)child;
            }
        }
        private void SplitLeaf(LeafNode<T> leaf, Stack<InternalNode<T>> accessPath)
        {
            // Step 1
            // Get the node to split on
            int mid = leaf.Leaves.Count / 2;

            // Step 2
            // Split the leaves
            // Get the right side of the nodes
            var rightLeaves = leaf.Leaves.GetRange(mid, leaf.Leaves.Count - mid);
            //now that we have a copy remove them from the original node
            leaf.Leaves.RemoveRange(mid, leaf.Leaves.Count - mid);

            // Step 3
            // Create a New Leaf Node
            var newLeaf = new LeafNode<T>();
            // Allocate a new leaf node to hold the second half of the entries.
            newLeaf.Leaves.AddRange(rightLeaves);


            //Step 4 
            //Update links
            newLeaf.Next = leaf.Next;
            leaf.Next = newLeaf;
            newLeaf.Previous = leaf;

            //Step 5
            //Get the seperator key
            int separatorKey = newLeaf.Leaves[0].Id;


            // Insert new leaf into parent's children list
            var parent = accessPath.Pop();
            int insertIndex = parent.Children.IndexOf(leaf) + 1;
            parent.Children.Insert(insertIndex, newLeaf);

            // Insert separator key
            parent.Keys.Insert(insertIndex - 1, separatorKey);

            // After inserting the separator key
            if (parent.Keys.Count > _degree)
            {
                // Check if parent is root
                if (parent == _root)
                {
                    var newRoot = new InternalNode<T>();
                    newRoot.Children.Add(parent);
                    SplitInternal(parent, newRoot);
                    _root = newRoot;
                }
                else
                {
                    var grandparent = accessPath.Pop();
                    SplitInternal(parent, grandparent);
                }
            }
        }

        private void SplitInternal(InternalNode<T> node, InternalNode<T> parent)
        {
            // Step 1: Determine split index and separator
            int mid = node.Keys.Count / 2;
            int separatorKey = node.Keys[mid];

            // Step 2: Create new internal node (right half)
            var newInternal = new InternalNode<T>();

            // Copy keys and children from the right half
            newInternal.Keys.AddRange(node.Keys.GetRange(mid + 1, node.Keys.Count - (mid + 1)));
            newInternal.Children.AddRange(node.Children.GetRange(mid + 1, node.Children.Count - (mid + 1)));

            // Step 3: Trim original (left) node
            node.Keys.RemoveRange(mid, node.Keys.Count - mid);
            node.Children.RemoveRange(mid + 1, node.Children.Count - (mid + 1));

            // Step 4: Insert new internal node and separator into parent
            int insertIndex = parent.Children.IndexOf(node) + 1;
            parent.Children.Insert(insertIndex, newInternal);
            parent.Keys.Insert(insertIndex - 1, separatorKey);
        }
    }
    }
