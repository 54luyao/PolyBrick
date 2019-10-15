using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino;

namespace PolyBrick
{
    class Globals
    {
        public static bool HAS_GRADIENT;
        public static int Initial_Number;
        public static double MAX_RADIUS;
        public static double MIN_RADIUS;
        public static double MAX_SPEED;
        public static double MAX_FORCE;
        public static Brep BOUNDARY;
        public static Point3d INIT_POINT=new Point3d();
        public static double initX;
        public static double initY;
        public static double initZ;
        public static Mesh DISTRIBUTION;
        public static Random rand = new System.Random();
        public static List<Point3d> EXISTING_POINTS = new List<Point3d>();

        //public static Point3d min_corner = BOUNDARY.GetBoundingBox(false).Min;
        //public static Point3d max_corner = BOUNDARY.GetBoundingBox(false).Max;
        //public static double BOUND_X_MIN = min_corner.X;
        //public static double BOUND_Y_MIN = min_corner.Y;
        //public static double BOUND_Z_MIN = min_corner.Z;
        //public static double BOUND_X_MAX = max_corner.X;
        //public static double BOUND_Y_MAX = max_corner.Y;
        //public static double BOUND_Z_MAX = max_corner.Z;

    }
}
