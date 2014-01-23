using System;
using System.Collections.Generic;
using System.IO;
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
        private const int DEFAULT_MAX_DEPTH = 9;
        private const string DEFAULT_QUADTREE_OUTFILE_PATH = "quadtree.dat";
        private const string DEFAULT_COUNTRIES_OUTFILE_PATH = "countries.dat";

        static void Main(string[] Args)
        {
            if (Args.Length == 0 || Args.Length >= 5)
            {
                Console.WriteLine("Usage ShapeFileToQuadTree.exe shapefile.shp [MaxDepth] [quadtree.dat] [countries.dat]");
                return;
            }

            var shapeFilePath = Args[0];
            var maxDepth = Args.Length > 1 ? int.Parse(Args[1]) : DEFAULT_MAX_DEPTH;
            var quadtreeOutputFilePath = Args.Length > 2 ? Args[2] : DEFAULT_QUADTREE_OUTFILE_PATH;
            var countriesOutputFilePath = Args.Length > 3 ? Args[3] : DEFAULT_COUNTRIES_OUTFILE_PATH;

            // Process the country shape file to construct a list
            // of country names and an R-tree of their bounding boxes.
            StRtree rtree;
            Dictionary<string, byte> countries;

            try
            {
                ProcessShapeFile(shapeFilePath, out rtree, out countries);
            } 
            catch (OverflowException)
            {
                Console.WriteLine("Ooops! There should not be more than 255 countries!");
                return;
            }

            // Build a quad tree from the R-tree.
            var bounds = (Envelope) rtree.Root.Bounds;
            var qtree = BuildQuadTree(rtree, countries, bounds.Minimum, bounds.Maximum, maxDepth);

            // Test on a few random places.
            var countryNames = countries.Keys.ToArray();
            TestOnRandomPlaces(qtree, countryNames);

            Console.WriteLine("Total leafs = {0}", _nleafs);

            // Save the countries dictionary.
            Console.WriteLine("Saving countries to {0}", countriesOutputFilePath);
            using (var filestream = new FileStream(countriesOutputFilePath, FileMode.Create))
            {
                using (var writer = new StreamWriter(filestream))
                {
                    foreach (var countryName in countryNames)
                        writer.WriteLine(countryName);
                }
            }

            // Save the quad tree.
            Console.WriteLine("Saving quad tree to {0}", quadtreeOutputFilePath);
            using (var filestream = new FileStream(quadtreeOutputFilePath, FileMode.Create))
            {
                using (var writer = new BinaryWriter(filestream))
                {
                    qtree.Save(writer);
                }
            }

            Console.ReadLine();
        }

        private static void TestOnRandomPlaces(QuadTree<byte> Qtree, string[] CountryNames)
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

        private static int _nleafs;
        private static QuadTree<byte> BuildQuadTree(StRtree Tree, Dictionary<string, byte> Countries, Coordinate TopLeft, Coordinate BottomRight, int MaxDepth, int Depth = 0)
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
                ++_nleafs;
                return new QuadTreeLeaf<byte>
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
                byte label = 0;
                // Take country with largest area intersecting.
                var r = (from Tuple<string, IGeometry> t in results orderby t.Item2.Intersection(envelope.ToPolygon()).Area descending select t).First();
                if (r.Item2.Intersection(envelope.ToPolygon()).Area > 0)
                    label = Countries[r.Item1];

                ++_nleafs;
                return new QuadTreeLeaf<byte>
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
            return new QuadTreeNode<byte>
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
        private static void ProcessShapeFile(string ShapeFilePath, out StRtree Tree, out Dictionary<string, byte> Countries)
        {
            Countries = new Dictionary<string, byte>();
            Countries.Add(string.Empty, 0);

            using (var p = Shapefile.OpenFile(ShapeFilePath))
            {
                Tree = new StRtree(p.NumRows());

                for (int i = 0; i < p.NumRows(); ++i)
                {
                    var row = p.GetFeature(i);
                    var country = row.DataRow["name"].ToString();
                    checked
                    {
                        
                    }
                    if (!Countries.ContainsKey(country))
                        Countries.Add(country, checked((byte)Countries.Count));
                    
                    var shape = row.ToShape();
                    var geometry = shape.ToGeometry();

                    Tree.Insert(geometry.Envelope, new Tuple<string, IGeometry>(country, geometry));
                }
            }

            Tree.Build();
        }
    }
}
