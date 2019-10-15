using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using PolyBrick.Params;


namespace PolyBrick.EllipsoidPacking
{
    class EGlobals
    {
        public static bool HAS_TENSORFIELD;
        public static int Initial_Number;
        public static double MAX_RADIUS;
        public static double MIN_RADIUS;
        public static double MAX_SPEED;
        public static double MAX_FORCE;
        public static Brep BOUNDARY;
        //public static Point3d INIT_POINT;
        //public static double initX;
        //public static double initY;
        //public static double initZ;
        //public static List<Stress> DISTRIBUTION = new List<Stress>();
        public static Random rand = new System.Random();
        public static List<Point3d> EXISTING_POINTS = new List<Point3d>();
        public static double BOUND_X_MIN;
        public static double BOUND_Y_MIN;
        public static double BOUND_Z_MIN;
        public static double BOUND_X_MAX;
        public static double BOUND_Y_MAX;
        public static double BOUND_Z_MAX;
        public static TensorFieldGoo TENSORFIELDGOO;
    }
}
