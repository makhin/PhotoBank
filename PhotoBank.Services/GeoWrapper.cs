using MetadataExtractor.Formats.Exif;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Utilities;

namespace PhotoBank.Services
{
    public interface IGeoWrapper
    {
        Geometry GetRectangle(BoundingRect rectangle);
        Geometry GetRectangle(FaceRectangle rectangle);
        Point GetLocation(GpsDirectory gpsDirectory);
    }

    public class GeoWrapper : IGeoWrapper
    {
        private readonly GeometryFactory _geometryFactory;

        public GeoWrapper()
        {
            _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        }

        public Geometry GetRectangle(BoundingRect rectangle)
        {
            GeometricShapeFactory gsf = new GeometricShapeFactory();
            gsf.CreateRectangle();

            return _geometryFactory.CreatePolygon(
                new[]
                {
                    new Coordinate(rectangle.X, rectangle.Y),
                    new Coordinate(rectangle.X + rectangle.H, rectangle.Y),
                    new Coordinate(rectangle.X + rectangle.H, rectangle.Y + rectangle.W),
                    new Coordinate(rectangle.X, rectangle.Y + rectangle.W),
                    new Coordinate(rectangle.X, rectangle.Y)
                });
        }

        public Geometry GetRectangle(FaceRectangle rectangle)
        {
            return _geometryFactory.CreatePolygon(
                new[]
                {
                    new Coordinate(rectangle.Left, rectangle.Top),
                    new Coordinate(rectangle.Left + rectangle.Width, rectangle.Top),
                    new Coordinate(rectangle.Left + rectangle.Width, rectangle.Top + rectangle.Height),
                    new Coordinate(rectangle.Left, rectangle.Top + rectangle.Height),
                    new Coordinate(rectangle.Left, rectangle.Top),
                });
        }

        public Point GetLocation(GpsDirectory gpsDirectory)
        {
            var location = gpsDirectory.GetGeoLocation();
            return location != null ? _geometryFactory.CreatePoint(new Coordinate(location.Latitude, location.Longitude)) : null;
        }
    }
}
