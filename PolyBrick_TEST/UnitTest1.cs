using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PolyBrick.EllipsoidPacking;
using Rhino;
using Rhino.Geometry;

namespace PolyBrick_TEST
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestEllipsoid()
        {
            Ellipsoid e1 = new Ellipsoid(0,0,0);
            Assert.AreEqual(0, e1.position.X);
        }
    }
}
