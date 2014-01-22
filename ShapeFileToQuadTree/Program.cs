using System;
using System.Collections.Generic;
using System.Linq;
using DotSpatial.Data;
using DotSpatial.Topology;
using DotSpatial.Topology.Index.Strtree;
using Geomoir.Data;
using Coordinate = DotSpatial.Topology.Coordinate;

namespace ShapeFileToQuadTree
{
    class Program
    {
        private const int DEFAULT_MAX_DEPTH = 13;
        private const string DEFAULT_OUTFILE_PATH = "quadtree.dat";

        static void Main(string[] Args)
        {
            if (Args.Length == 0 || Args.Length >= 4)
            {
                Console.WriteLine("Usage ShapeFuleToQuadTree.exe shapefile.shp [MaxDepth] [quadtree.dat]");
                return;
            }

            var shapeFilePath = Args[0];
            var maxDepth = Args.Length > 1 ? int.Parse(Args[1]) : DEFAULT_MAX_DEPTH;
            var outputFilePath = Args.Length > 2 ? Args[2] : DEFAULT_OUTFILE_PATH;

            // Process the country shape file to construct a list
            // of country names and an R-tree of their bounding boxes.
            StRtree rtree;
            Dictionary<string, int> countries;
            ProcessShapeFile(shapeFilePath, out rtree, out countries);

            // Build a quad tree from the R-tree.
            var bounds = rtree.Root.Bounds as Envelope;
            var qtree = BuildQuadTree(rtree, countries, bounds.Minimum, bounds.Maximum, maxDepth);

            // Test on a few random places.
            var countryNames = countries.Keys.ToArray();
            TestOnRandomPlaces(qtree, countryNames);

            Console.WriteLine("Total leafs = {0}", nleafs);

            Console.ReadLine();
        }

        private static void TestOnRandomPlaces(QuadTree<int> Qtree, string[] CountryNames)
        {
            // Oxford (UK)
            var countryResult = Qtree.Query(new Geomoir.Data.Coordinate(-1.25f, 51.7f));
            Console.WriteLine("GOT {0}", CountryNames[countryResult]);

            // Pyongyang (North Korea)
            countryResult = Qtree.Query(new Geomoir.Data.Coordinate(125.3f, 39.0f));
            Console.WriteLine("GOT {0}", CountryNames[countryResult]);

            // Seoul (South Korea)
            countryResult = Qtree.Query(new Geomoir.Data.Coordinate(126.98f, 37.57f));
            Console.WriteLine("GOT {0}", CountryNames[countryResult]);

            // Madrid (Spain)
            countryResult = Qtree.Query(new Geomoir.Data.Coordinate(-3.68f, 40.04f));
            Console.WriteLine("GOT {0}", CountryNames[countryResult]);

            // Lima (Peru)
            countryResult = Qtree.Query(new Geomoir.Data.Coordinate(-77.03f, -12.04f));
            Console.WriteLine("GOT {0}", CountryNames[countryResult]);

            // Zanzibar (Tanzania)
            countryResult = Qtree.Query(new Geomoir.Data.Coordinate(39.32f, -6.13f));
            Console.WriteLine("GOT {0}", CountryNames[countryResult]);

            // Vancouver (Canada)
            countryResult = Qtree.Query(new Geomoir.Data.Coordinate(-123.1f, 49.25f));
            Console.WriteLine("GOT {0}", CountryNames[countryResult]);

            // Honolulu (USA)
            countryResult = Qtree.Query(new Geomoir.Data.Coordinate(-157.8f, 21.3f));
            Console.WriteLine("GOT {0}", CountryNames[countryResult]);

            // Londonderry (UK)
            countryResult = Qtree.Query(new Geomoir.Data.Coordinate(-7.31f, 54.996f));
            Console.WriteLine("GOT {0}", CountryNames[countryResult]);

            // Donegal (Ireland)
            countryResult = Qtree.Query(new Geomoir.Data.Coordinate(-8f, 54.917f));
            Console.WriteLine("GOT {0}", CountryNames[countryResult]);
        }

        private static int nleafs = 0;
        private static QuadTree<int> BuildQuadTree(StRtree Tree, Dictionary<string, int> Countries, Coordinate TopLeft, Coordinate BottomRight, int MaxDepth, int Depth = 0)
        {
            // Bounding box for this node.
            var envelope = new Envelope(TopLeft, BottomRight);

            // Find all countries whose bounding boxes intersect with this node.
            var coarseResults = Tree.Query(envelope);

            // Of those countries, find those whose geometry actually intersects with this node.
            var fineResults = (from Tuple<string, IGeometry> r in coarseResults select r).Where(r => r.Item2.Intersects(envelope.ToPolygon()));

            // In case of either:
            // 1) No countries intersect, in which case we mark this node as empty. Or;
            // 2) Exactly one country intersects, in which case we mark this node as that country.
            var results = fineResults as IList<Tuple<string, IGeometry>> ?? fineResults.ToList();
            if (results.Count() <= 1)
            {
                var country = results.FirstOrDefault();
                var countryName = country != null ? country.Item1 : "";

                Console.WriteLine("Adding {0}, Depth {1}, {2}x{3}", countryName, Depth, BottomRight.X - TopLeft.X, BottomRight.Y - TopLeft.Y);
                ++nleafs;
                return new QuadTreeLeaf<int>
                {
                    Data = Countries[countryName],
                    TopLeft = TopLeft.ToGeomoirCoordinate(),
                    BottomRight = BottomRight.ToGeomoirCoordinate()
                };
            }
            
            // If we have reached the maximum depth and multiple countries interect
            // with this node, mark it as the country with the largest overlap.
            if (Depth >= MaxDepth)
            {
                var label = 0;
                // Take country with largest area intersecting.
                var r = (from Tuple<string, IGeometry> t in results orderby t.Item2.Intersection(envelope.ToPolygon()).Area descending select t).First();
                if (r.Item2.Intersection(envelope.ToPolygon()).Area > 0)
                    label = Countries[r.Item1];

                ++nleafs;
                return new QuadTreeLeaf<int>
                {
                    Data = label,
                    TopLeft = TopLeft.ToGeomoirCoordinate(),
                    BottomRight = BottomRight.ToGeomoirCoordinate()
                };
            }

            // Split the node into 4 quadrants and recurse on each.
            var middleTop = new Coordinate((BottomRight.X + TopLeft.X) / 2, TopLeft.Y);
            var middleBottom = new Coordinate((BottomRight.X + TopLeft.X) / 2, BottomRight.Y);
            var middleLeft = new Coordinate(TopLeft.X, (BottomRight.Y + TopLeft.Y) / 2);
            var middleRight = new Coordinate(BottomRight.X, (BottomRight.Y + TopLeft.Y) / 2);
            var middle = new Coordinate(middleTop.X, middleLeft.Y);
            return new QuadTreeNode<int>
            {
                TopLeft = TopLeft.ToGeomoirCoordinate(),
                BottomRight = BottomRight.ToGeomoirCoordinate(),
                Children = new []
                {
                    BuildQuadTree(Tree, Countries, TopLeft, middle, MaxDepth, Depth + 1),
                    BuildQuadTree(Tree, Countries, middleTop, middleRight, MaxDepth, Depth + 1),
                    BuildQuadTree(Tree, Countries, middleLeft, middleBottom, MaxDepth, Depth + 1),
                    BuildQuadTree(Tree, Countries, middle, BottomRight, MaxDepth, Depth + 1)
                }
            };
        }

        // Loads a shapefile (*.shp), i.e. downloaded from http://www.naturalearthdata.com/,
        // adds each polygon to an R-tree, and adds each country name to a dictionary.
        private static void ProcessShapeFile(string ShapeFilePath, out StRtree Tree, out Dictionary<string, int> Countries)
        {
            Countries = new Dictionary<string, int>();
            Countries.Add(string.Empty, 0);

            using (var p = Shapefile.OpenFile(ShapeFilePath))
            {
                Tree = new StRtree(p.NumRows());

                for (int i = 0; i < p.NumRows(); ++i)
                {
                    var row = p.GetFeature(i);
                    var country = row.DataRow["name"].ToString();

                    if (!Countries.ContainsKey(country))
                        Countries.Add(country, Countries.Count);
                    
                    var shape = row.ToShape();
                    var geometry = shape.ToGeometry();

                    Tree.Insert(geometry.Envelope, new Tuple<string, IGeometry>(country, geometry));
                }
            }

            Tree.Build();
        }
    }
}
