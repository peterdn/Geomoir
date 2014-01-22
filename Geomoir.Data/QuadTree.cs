using System;

namespace Geomoir.Data
{
    // TODO: Very basic implementation, can probably be made much more efficient.

    public abstract class QuadTree<T>
    {
        public Coordinate TopLeft { get; set; }
        public Coordinate BottomRight { get; set; }

        public abstract T Query(Coordinate QueryPoint);
    }
    
    public class QuadTreeLeaf<T> : QuadTree<T>
    {
        public T Data { get; set; }

        public override T Query(Coordinate QueryPoint)
        {
            if (QueryPoint.X < TopLeft.X || QueryPoint.X > BottomRight.X || QueryPoint.Y < TopLeft.Y || QueryPoint.Y > BottomRight.Y)
                throw new ArgumentOutOfRangeException("QueryPoint", "The specified coordinate is outside the bounds of the tree");

            return Data;
        }
    }

    public class QuadTreeNode<T> : QuadTree<T>
    {
        public QuadTree<T>[] Children { get; set; }

        public QuadTreeNode()
        {
            Children = new QuadTree<T>[4];
        }

        public override T Query(Coordinate QueryPoint)
        {
            if (QueryPoint.X < TopLeft.X || QueryPoint.X > BottomRight.X || QueryPoint.Y < TopLeft.Y || QueryPoint.Y > BottomRight.Y)
                throw new ArgumentOutOfRangeException("QueryPoint", "The specified coordinate is outside the bounds of the tree");

            if (QueryPoint.X <= Children[0].BottomRight.X && QueryPoint.Y <= Children[0].BottomRight.Y)
                return Children[0].Query(QueryPoint);
            if (QueryPoint.X <= Children[0].BottomRight.X)
                return Children[2].Query(QueryPoint);
            if (QueryPoint.Y <= Children[0].BottomRight.Y)
                return Children[1].Query(QueryPoint);
            return Children[3].Query(QueryPoint);
        }
    }
}
