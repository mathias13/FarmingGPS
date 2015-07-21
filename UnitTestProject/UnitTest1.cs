using DotSpatial.Positioning;
using DotSpatial.Topology;
using DotSpatial.Topology.Algorithm;
using FarmingGPSLib.HelperClasses;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace UnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void CheckBearingCalculation()
        {
            Azimuth bearing = TestBearing(20, 20);
            Assert.AreEqual(bearing, Azimuth.Northeast);

            bearing = TestBearing(20, -20);
            Assert.AreEqual(bearing, Azimuth.Northwest);

            bearing = TestBearing(-20, -20);
            Assert.AreEqual(bearing, Azimuth.Southwest);

            bearing = TestBearing(-20, 20);
            Assert.AreEqual(bearing, Azimuth.Southeast);

            bearing = TestBearing(20, 0);
            Assert.AreEqual(bearing, Azimuth.North);

            bearing = TestBearing(0, -20);
            Assert.AreEqual(bearing, Azimuth.West);

            bearing = TestBearing(-20, 0);
            Assert.AreEqual(bearing, Azimuth.South);

            bearing = TestBearing(0, 20);
            Assert.AreEqual(bearing, Azimuth.East);
        }

        [TestMethod]
        public void CheckSpeedCalculation()
        {
            double speed = TestVelocity(20, 20);
            Assert.AreEqual(speed, 28.284271247461900976033774484194);

            speed = TestVelocity(20, -20);
            Assert.AreEqual(speed, 28.284271247461900976033774484194);

            speed = TestVelocity(-20, -20);
            Assert.AreEqual(speed, 28.284271247461900976033774484194);

            speed = TestVelocity(-20, 20);
            Assert.AreEqual(speed, 28.284271247461900976033774484194);
        }

        private double TestVelocity(int velocityNorth, int velocityEast)
        {
            double speed = Math.Sqrt(Math.Pow((double)velocityEast, 2.0) + Math.Pow((double)velocityNorth, 2.0));

            //Velocity is in mm/s so we multiply to get m/s
            return Math.Abs(speed);
        }

        private Azimuth TestBearing(int velocityNorth, int velocityEast)
        {
            double speed = Math.Sqrt(Math.Pow((double)velocityEast, 2.0) + Math.Pow((double)velocityNorth, 2.0)); ;
            double radians = Math.Acos((double)velocityEast / speed);
            double degrees = (180.0 / Math.PI) * radians;
            if (velocityNorth < 0)
                degrees = 360.0 - degrees;
            return new Azimuth(90.0 + ((degrees) * -1.0)).Normalize().Round(3);
        }

        [TestMethod]
        public void TestPolygon()
        {
            CoordinateList coords = new CoordinateList();
            AddCoords(coords, new Coordinate(10.0, 0.0), new Coordinate(5.0, 0.0));
            AddCoords(coords, new Coordinate(10.0, 1.0), new Coordinate(5.0, 1.0));
            AddCoords(coords, new Coordinate(10.0, 2.0), new Coordinate(5.0, 2.0));
            AddCoords(coords, new Coordinate(10.0, 3.0), new Coordinate(5.0, 3.0));
            AddCoords(coords, new Coordinate(10.0, 4.0), new Coordinate(5.0, 4.0));
            AddCoords(coords, new Coordinate(7.5, 7.0), new Coordinate(4.75, 4.5));
            AddCoords(coords, new Coordinate(5, 9.0), new Coordinate(4.5, 5.0));
            AddCoords(coords, new Coordinate(2.5, 7.0), new Coordinate(4.25, 4.5));
            AddCoords(coords, new Coordinate(0.0, 4.0), new Coordinate(4.0, 4.0));
            AddCoords(coords, new Coordinate(0.0, 3.5), new Coordinate(4.0, 3.5));
            
            //AddCoords(coords, new Coordinate(0.0, 3.0), new Coordinate(4.0, 3.0));
            //AddCoords(coords, new Coordinate(0.0, 2.0), new Coordinate(4.0, 2.0));

            //AddCoords(coords, new Coordinate(2.0, 3.0), new Coordinate(6.0, 3.0));
            //AddCoords(coords, new Coordinate(2.0, 2.0), new Coordinate(6.0, 2.0));

            coords.Add(new Coordinate(coords[0].X, coords[0].Y));
            LinearRing linearRing = new LinearRing(coords);
            Assert.IsTrue(linearRing.IsSimple);
            Assert.IsTrue(linearRing.IsRing);
            Polygon polygon = new Polygon(linearRing);
            Assert.IsTrue(polygon.IsSimple);
        }

        [TestMethod]
        public void TestSelfIntersect()
        {
            List<Coordinate> coords = new List<Coordinate>();
            coords.Add(new Coordinate(1.0, 0.0));
            coords.Add(new Coordinate(1.0, 1.0));
            coords.Add(new Coordinate(0.0, 0.0));
            coords.Add(new Coordinate(0.0, 1.0));
            coords.Add(new Coordinate(1.0, 0.0));
            LinearRing ring = new LinearRing(coords);
            Assert.IsTrue(ring.IsSimple);
        }

        private void AddCoords(CoordinateList list, Coordinate coordLeft, Coordinate coordRight)
        {
            int index = list.Count / 2;
            list.Insert(index, coordLeft);
            list.Insert(index, coordRight);
        }

        [TestMethod]
        public void TestIntersection()
        {
            RobustLineIntersector intersector = new RobustLineIntersector();
            IntersectionType intersectionType = intersector.ComputeIntersect(new Coordinate(2.0, 2.0),
                                                                                new Coordinate(2.0, 4.0),
                                                                                new Coordinate(1.0, 3.0),
                                                                                new Coordinate(3.0, 3, 0));
            Assert.IsTrue(intersectionType == IntersectionType.PointIntersection);
        }

        [TestMethod]
        public void TestIntersection2()
        {
            List<Coordinate> coords = new List<Coordinate>();
            coords.Add(new Coordinate(0.0, 0.0));
            coords.Add(new Coordinate(5.0, 0.0));
            coords.Add(new Coordinate(5.0, 5.0));
            coords.Add(new Coordinate(0.0, 5.0));
            coords.Add(new Coordinate(0.0, 0.0));
            ILinearRing ring = new LinearRing(coords);
            ILineString line = new LineString(new Coordinate[] {new Coordinate(3.0, 6.0), new Coordinate(6.0, 3.0)});
            IGeometry result = ring.Intersection(line);

            Assert.IsTrue(result.Coordinates.Count > 0);
        }

        [TestMethod]
        public void TestComputePoint()
        {
            Azimuth testAngle = Azimuth.North;
            testAngle.Subtract(90).Normalize();
            Coordinate one = HelperClassCoordinate.ComputePoint(new Coordinate(0.0, 0.0), testAngle.ToRadians().Value, 3.0);
            Assert.AreEqual(new Coordinate(0.0, 3.0), one);
        }

        [TestMethod]
        public void TestIntersection1()
        {
            LinearRing ring = new LinearRing(new Coordinate[5]{
                                                new Coordinate(0.0, 0.0),
                                                new Coordinate(0.0, 4.0),
                                                new Coordinate(4.0, 4.0),
                                                new Coordinate(4.0, 0.0),
                                                new Coordinate(0.0, 0.0)});

            LinearRing ring1 = new LinearRing(new Coordinate[5]{
                                                new Coordinate(5.0, -1.0),
                                                new Coordinate(-1.0, -1.0),
                                                new Coordinate(-1.0, 5.0),
                                                new Coordinate(5.0, 5.0),
                                                new Coordinate(5.0, -1.0)});

            //IGeometry ring2 = ring.Intersection(ring1);
            //Assert.IsTrue(ring2.Coordinates.Count > 0);
            

            Polygon poly1 = new Polygon(ring);
            Polygon poly2 = new Polygon(ring1);
            IGeometry polyDiff = poly1.Difference(poly2);
            Assert.IsTrue(polyDiff.FeatureType == FeatureType.Polygon);

            IGeometry polyDiff1 = poly2.Difference(poly1);
                                              Assert.IsTrue(polyDiff1.FeatureType == FeatureType.Polygon);
        }

    }
}
