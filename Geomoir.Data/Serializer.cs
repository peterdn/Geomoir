using System.IO;

namespace Geomoir.Data
{
    // There has to be a better way of doing this, but for the moment
    // lets play along with the PCL restrictions.

    public static class Serializer
    {
        private const byte NODE = 0x10;
        private const byte LEAF = 0x20;

        private static void SaveTree(this QuadTree<byte> Tree, BinaryWriter Writer)
        {
            if (Tree is QuadTreeLeaf<byte>)
            {
                var leaf = (QuadTreeLeaf<byte>)Tree;
                Writer.Write(LEAF);
                Writer.Write(leaf.Data);
            }
            else if (Tree is QuadTreeNode<byte>)
            {
                var node = (QuadTreeNode<byte>)Tree;
                Writer.Write(NODE);
                foreach (var t in node.Children)
                    SaveTree(t, Writer);
            }
            else
            {
                throw new InvalidDataException("Invalid type");
            }
        }

        public static void Save(this QuadTree<byte> Tree, BinaryWriter Writer)
        {
            // First save the dimensions.
            Writer.Write(Tree.TopLeft.X);
            Writer.Write(Tree.TopLeft.Y);
            Writer.Write(Tree.BottomRight.X);
            Writer.Write(Tree.BottomRight.Y);

            // Save the rest of the tree.
            SaveTree(Tree, Writer);
        }

        private static QuadTree<byte> LoadQuadTreeInternal(BinaryReader Reader, Coordinate TopLeft, Coordinate BottomRight)
        {
            var type = Reader.ReadByte();
            if (type == LEAF)
            {
                return new QuadTreeLeaf<byte>
                {
                    BottomRight = BottomRight,
                    TopLeft = TopLeft,
                    Data = Reader.ReadByte()
                };
            }
            else if (type == NODE)
            {
                var middleTop = new Coordinate((BottomRight.X + TopLeft.X) / 2, TopLeft.Y);
                var middleBottom = new Coordinate((BottomRight.X + TopLeft.X) / 2, BottomRight.Y);
                var middleLeft = new Coordinate(TopLeft.X, (BottomRight.Y + TopLeft.Y) / 2);
                var middleRight = new Coordinate(BottomRight.X, (BottomRight.Y + TopLeft.Y) / 2);
                var middle = new Coordinate(middleTop.X, middleLeft.Y);
                return new QuadTreeNode<byte>
                {
                    TopLeft = TopLeft,
                    BottomRight = BottomRight,
                    Children = new[]
                    {
                        LoadQuadTreeInternal(Reader, TopLeft, middle),
                        LoadQuadTreeInternal(Reader, middleTop, middleRight),
                        LoadQuadTreeInternal(Reader, middleLeft, middleBottom),
                        LoadQuadTreeInternal(Reader, middle, BottomRight)
                    }
                };
            }
            else
            {
                throw new InvalidDataException("Invalid type identifier");
            }
        } 

        public static QuadTree<byte> LoadQuadTree(BinaryReader Reader)
        {
            // First load the dimensions.
            var topLeft = new Coordinate(Reader.ReadSingle(), Reader.ReadSingle());
            var bottomRight = new Coordinate(Reader.ReadSingle(), Reader.ReadSingle());

            return LoadQuadTreeInternal(Reader, topLeft, bottomRight);
        }
    }
}
