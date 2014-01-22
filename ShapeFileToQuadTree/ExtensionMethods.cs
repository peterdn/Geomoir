using Geomoir.Data;

namespace ShapeFileToQuadTree
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Converts a DotSpatial Coordinate to a Geomoir Coordinate.
        /// </summary>
        /// <param name="Coordinate">The Coordinate to convert.</param>
        /// <returns>The converted Coordinate</returns>
        public static Coordinate ToGeomoirCoordinate(this DotSpatial.Topology.Coordinate Coordinate)
        {
            return new Coordinate((float) Coordinate.X, (float) Coordinate.Y);
        } 
    }
}
