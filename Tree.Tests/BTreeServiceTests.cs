using System.Reflection;

namespace TreeLibrary.Tests
{
    [TestFixture]
    public class BTreeServiceTests
    {
        /// <summary> 
        /// Helper to reach the private "_root" field via reflection.
        /// </summary>
        private static BTreeNode<T> GetRoot<T>(BTreeService<T> tree)
        {
            var fld = typeof(BTreeService<T>).GetField("_root", BindingFlags.NonPublic | BindingFlags.Instance);
            return (BTreeNode<T>)fld!.GetValue(tree)!;
        }

        /// <summary>
        /// Inserts fewer than <paramref name="degree"/> items; root should remain a LeafNode.
        /// </summary>
        [TestCase(3)]
        [TestCase(4)]
        public void Insert_UnderDegree_RootStaysLeaf(int degree)
        {
            var tree = new BTreeService<string>(degree);

            // insert degree?1 values
            for (int i = 0; i < degree - 1; i++)
            {
                tree.Add($"v{i}");
            }

            var root = GetRoot(tree);
            Assert.That(root, Is.TypeOf<LeafNode<string>>(),"Root should still be a leaf when less than degree items inserted.");
        }

        /// <summary>
        /// Inserting <paramref name="degree"/> + 1 items should split the root leaf once,
        /// promoting a new InternalNode root with two leaf children.
        /// </summary>
        [TestCase(3)]
        [TestCase(4)]
        public void Insert_DegreePlusOneItems_SplitsLeafAndCreatesInternalRoot(int degree)
        {
            var tree = new BTreeService<string>(degree);

            for (int i = 0; i < degree + 1; i++)
            {
                tree.Add($"v{i}");
            }

            var root = GetRoot(tree);
            Assert.That(root, Is.TypeOf<InternalNode<string>>(), "Root should now be internal.");

            var internalRoot = (InternalNode<string>)root;
            Assert.That(internalRoot.Keys.Count, Is.EqualTo(1), "Root should hold 1 separator key.");
            Assert.That(internalRoot.Children.Count, Is.EqualTo(2), "Root should have two children.");
            Assert.That(internalRoot.Children.All(c => c is LeafNode<string>), Is.True, "Both children of new root should be leaves.");
        }


        /// <summary>
        /// Insert enough values to force 2 leaf splits; root internal node will now contain
        /// 2 keys and 3 leaf children, but should NOT have split yet.
        /// For degree�=�3 that means 2*degree�=�6 inserts (the 3rd and 6th cause splits).
        /// </summary>
        [TestCase(3, 6)]
        [TestCase(4, 8)]
        public void Insert_MultipleSplits_RootInternalKeysGrow(int degree, int valueCount)
        {
            var tree = new BTreeService<string>(degree);

            for (int i = 0; i < valueCount; i++)
            {
                tree.Add($"v{i}");
            }

            var root = GetRoot(tree);
            Assert.That(root, Is.TypeOf<InternalNode<string>>(), "Root should be internal.");

            var internalRoot = (InternalNode<string>)root;
            Assert.That(internalRoot.Keys.Count, Is.EqualTo(2),"Root should now contain 2 separator keys after two leaf splits.");
            Assert.That(internalRoot.Children.Count, Is.EqualTo(3),"Root should have three leaf children after two splits.");
        }

        /// <summary>
        /// Insert enough values to overflow the internal root itself, forcing an Internal split
        /// (and creation of a new higher?level root). For degree�=�3 the 3rd leaf?split gives
        /// root.Keys.Count == 3 ?? at that point internal split should have occurred.
        /// </summary>
        [TestCase(3, 9)]   // 3rd split at 9th insert
        public void Insert_OverflowRoot_SplitsInternalAndCreatesNewRoot(int degree, int valueCount)
        {
            var tree = new BTreeService<string>(degree);

            for (int i = 0; i < valueCount; i++)
            {
                tree.Add($"v{i}");
            }

            var root = GetRoot(tree);

            // After internal split the root should again have 1 key,
            // and its two children should themselves be InternalNodes.
            Assert.That(root, Is.TypeOf<InternalNode<string>>(), "Root should still be internal.");

            var internalRoot = (InternalNode<string>)root;
            Assert.That(internalRoot.Keys.Count, Is.EqualTo(1), "New root should have exactly 1 key.");
            Assert.That(internalRoot.Children.Count, Is.EqualTo(2), "New root should have exactly 2 children.");
            Assert.That(internalRoot.Children.All(c => c is InternalNode<string>), Is.True, "After root split, its children should be internal nodes.");
        }

        [TestCase(3)]
        [TestCase(2)]
        public void Insert_ManyItems_GrowsTreeToDepthTen(int degree)
        {
            var tree = new BTreeService<string>(degree);

            // Insert enough items to grow to depth 10.
            // In a B+ tree, the max number of items per level grows exponentially.
            // This is a rough estimate based on log_base(degree)(n) = depth
            // So, n ≈ degree^depth
            int targetDepth = 10;
            int itemCount = (int)Math.Pow(degree, targetDepth);

            for (int i = 0; i < itemCount; i++)
            {
                tree.Add($"v{i}");
            }

            int actualDepth = tree.GetDepth();
            Assert.That(actualDepth, Is.EqualTo(targetDepth), $"Tree should reach depth {targetDepth} with {itemCount} inserts for degree {degree}.");
        }

    }
}
