using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using MathNet.Numerics.LinearAlgebra;
using PolyBrick.Params;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System.Drawing;
using Rhino.Collections;

namespace PolyBrick.EllipsoidPacking
{
    public class Ellipsoid
    {
        public Vector3d acceleration;
        public Vector3d velocity;
        public Vector3d position;
        public double radiusA;
        public double radiusB;
        public double radiusC;
        public Vector3d orientation;
        public bool moved;
        public static Random rand = new Random();
        //public BoundingBox bBox;//AABB in object space

        public Ellipsoid()
        {
        //    //position = new Vector3d(0, 0, 0);
        //    //acceleration = new Vector3d(0, 0, 0);
        //    //velocity = GlobalRandom.RandomVector();
        //    //orientation = new Vector3d(0, 0, 1);
        //    //radiusA = EGlobals.MIN_RADIUS;
        //    //radiusB = EGlobals.MIN_RADIUS;
        //    //radiusC = EGlobals.MAX_RADIUS;
        }

        public Ellipsoid(double x,double y,double z)
        {
            position = new Vector3d(x, y, z);
            acceleration = new Vector3d(0, 0, 0);
            velocity = GlobalRandom.RandomVector();
            orientation = new Vector3d(0, 0, 1);
            radiusA = EGlobals.MIN_RADIUS;
            radiusB = EGlobals.MIN_RADIUS;
            radiusC = EGlobals.MAX_RADIUS;
            moved = true;
            //bBox = new BoundingBox(- radiusA, - radiusB, - radiusC,  radiusA,  radiusB,  radiusC);
        }

        public Ellipsoid(Point3d point)
        {
            position = new Vector3d(point);
            acceleration = new Vector3d(0, 0, 0);
            velocity = GlobalRandom.RandomVector();
            orientation = new Vector3d(0, 0, 1);
            radiusA = EGlobals.MIN_RADIUS;
            radiusB = EGlobals.MIN_RADIUS;
            radiusC = EGlobals.MAX_RADIUS;
            moved = true;
            //bBox = new BoundingBox(- radiusA, - radiusB,  - radiusC,   radiusA,   radiusB,  radiusC);
        }

        public Ellipsoid(Ellipsoid e)
        {
            position = e.position;
            acceleration = e.acceleration;
            velocity = e.velocity;
            orientation = e.orientation;
            radiusA = e.radiusA;
            radiusB = e.radiusB;
            radiusC = e.radiusC;
            moved = e.moved;
            //bBox = e.bBox;
        }

        public void CheckBorder()
        {
            Point3d center = new Point3d(this.position);
            Point3d closestPoint = EGlobals.BOUNDARY.ClosestPoint(center);
            this.position = new Vector3d(closestPoint);
        }

        public static Ellipsoid RandomEllipsoid()
        {
            if (rand.NextDouble() < 0.7)
            {
                if (EGlobals.HAS_TENSORFIELD)
                {
                    Point3d point = EGlobals.TENSORFIELDGOO.Value.Tensors[(int)Math.Floor(Math.Sqrt(rand.NextDouble()) * (EGlobals.TENSORFIELDGOO.Value.Tensors.Count - 1))].plane.Origin;
                    if (EGlobals.BOUNDARY.IsPointInside(point, RhinoMath.SqrtEpsilon, true)) return new Ellipsoid(point);
                }
            }
            double x;
            double y;
            double z;
            x = rand.NextDouble() * (EGlobals.BOUND_X_MAX - EGlobals.BOUND_X_MIN) + EGlobals.BOUND_X_MIN;
            y = rand.NextDouble() * (EGlobals.BOUND_Y_MAX - EGlobals.BOUND_Y_MIN) + EGlobals.BOUND_Y_MIN;
            z = rand.NextDouble() * (EGlobals.BOUND_Z_MAX - EGlobals.BOUND_Z_MIN) + EGlobals.BOUND_Z_MIN;
            while (!EGlobals.BOUNDARY.IsPointInside(new Point3d(x, y, z), RhinoMath.SqrtEpsilon, true))
            {
                x = rand.NextDouble() * (EGlobals.BOUND_X_MAX - EGlobals.BOUND_X_MIN) + EGlobals.BOUND_X_MIN;
                y = rand.NextDouble() * (EGlobals.BOUND_Y_MAX - EGlobals.BOUND_Y_MIN) + EGlobals.BOUND_Y_MIN;
                z = rand.NextDouble() * (EGlobals.BOUND_Z_MAX - EGlobals.BOUND_Z_MIN) + EGlobals.BOUND_Z_MIN;
            }

            return new Ellipsoid(x, y, z);
        }

        public static Ellipsoid RandomEllipsoid(PackEllipsoid pack)
        {
            if (pack.ellipsoids.Count > 0)
            {
                Point3d min_corner = EGlobals.BOUNDARY.GetBoundingBox(false).Min;
                Point3d max_corner = EGlobals.BOUNDARY.GetBoundingBox(false).Max;
                double cell_size = EGlobals.MIN_RADIUS;
                int xCount = (int)Math.Ceiling((EGlobals.BOUND_X_MAX - EGlobals.BOUND_X_MIN) / cell_size);
                int yCount = (int)Math.Ceiling((EGlobals.BOUND_Y_MAX - EGlobals.BOUND_Y_MIN) / cell_size);
                int zCount = (int)Math.Ceiling((EGlobals.BOUND_Z_MAX - EGlobals.BOUND_Z_MIN) / cell_size);
                List<Point3d> pList = new List<Point3d>();
                for (int i = 0; i < pack.ellipsoids.Count; i++)
                {
                    pList.Add(new Point3d(pack.ellipsoids[i].position));
                }
                Point3dList centroidList = new Point3dList(pList);
                for (int x = 0; x < xCount; x++)
                {
                    for (int y = 0; y < yCount; y++)
                    {
                        for (int z = 0; z < zCount; z++)
                        {
                            Point3d samplePoint = new Point3d(x * cell_size + min_corner.X, y * cell_size + min_corner.Y, z * cell_size + min_corner.Z);
                            if (EGlobals.BOUNDARY.IsPointInside(samplePoint, RhinoMath.SqrtEpsilon, true))
                            {
                                int nearIndex = centroidList.ClosestIndex(samplePoint);
                                Point3d nearest = pList[nearIndex];
                                Vector3d orientation = new Vector3d();
                                double maxFactor = 0;
                                double minFactor = 0;
                                EGlobals.TENSORFIELDGOO.Value.GetOrientation(samplePoint, ref orientation, ref maxFactor, ref minFactor);
                                double rMin = EGlobals.MAX_RADIUS - maxFactor * (EGlobals.MAX_RADIUS - EGlobals.MIN_RADIUS);
                                if (rMin + pack.ellipsoids[nearIndex].radiusA < samplePoint.DistanceTo(nearest))
                                {
                                    return new Ellipsoid(samplePoint);
                                }
                            }
                        }
                    }
                }
            }
            double randx;
            double randy;
            double randz;
            randx = rand.NextDouble() * (EGlobals.BOUND_X_MAX - EGlobals.BOUND_X_MIN) + EGlobals.BOUND_X_MIN;
            randy = rand.NextDouble() * (EGlobals.BOUND_Y_MAX - EGlobals.BOUND_Y_MIN) + EGlobals.BOUND_Y_MIN;
            randz = rand.NextDouble() * (EGlobals.BOUND_Z_MAX - EGlobals.BOUND_Z_MIN) + EGlobals.BOUND_Z_MIN;
            while (!EGlobals.BOUNDARY.IsPointInside(new Point3d(randx, randy, randz), RhinoMath.SqrtEpsilon, true))
            {
                randx = rand.NextDouble() * (EGlobals.BOUND_X_MAX - EGlobals.BOUND_X_MIN) + EGlobals.BOUND_X_MIN;
                randy = rand.NextDouble() * (EGlobals.BOUND_Y_MAX - EGlobals.BOUND_Y_MIN) + EGlobals.BOUND_Y_MIN;
                randz = rand.NextDouble() * (EGlobals.BOUND_Z_MAX - EGlobals.BOUND_Z_MIN) + EGlobals.BOUND_Z_MIN;
            }

            return new Ellipsoid(randx, randy, randz);
        }

        public void UpdateSizeOrientation(TensorField tf)
        {
            double maxFactor = 0;
            double minFactor = 0;
            Vector3d newOrientation = new Vector3d();
            tf.GetOrientation((Point3d)this.position, ref newOrientation, ref maxFactor, ref minFactor);
            orientation = newOrientation;
            radiusC = EGlobals.MAX_RADIUS - minFactor * (EGlobals.MAX_RADIUS - EGlobals.MIN_RADIUS);
            radiusB = EGlobals.MAX_RADIUS - maxFactor * (EGlobals.MAX_RADIUS - EGlobals.MIN_RADIUS);
            radiusA = EGlobals.MAX_RADIUS - maxFactor * (EGlobals.MAX_RADIUS - EGlobals.MIN_RADIUS);
            //bBox = new BoundingBox(-radiusA, -radiusB, -radiusC, radiusA, radiusB, radiusC);
        }

        //public void UpdateRadius(Stress stress)
        //{
        //    //TODO:Update radius base on background;
        //    //Stress stress = EGlobals.FEBackGround.interpolate(new Point3d(this.position));
        //    double max_stress = EGlobals.TENSORFIELDGOO.Value.MaxStress;
        //    radiusA = EGlobals.MIN_RADIUS + (max_stress-stress.compression.Length) / max_stress * (EGlobals.MAX_RADIUS - EGlobals.MIN_RADIUS);
        //    radiusB = radiusA;
        //    radiusC = EGlobals.MIN_RADIUS + (max_stress - stress.tension.Length) / max_stress * (EGlobals.MAX_RADIUS - EGlobals.MIN_RADIUS);
        //}

        //public void UpdateOrientation(Stress stress)
        //{
        //    //TODO:Update orientation base on background;
        //    //Stress stress = EGlobals.FEBackGround.interpolate(new Point3d(this.position));
        //    this.orientation = stress.compression;
        //}

        public void ApplyForce(Vector3d force)
        {
            this.acceleration = this.acceleration + force;
        }

        public void Move()
        {
            this.velocity = this.velocity + this.acceleration;
            this.position = this.position + this.velocity;
            this.acceleration = new Vector3d(0, 0, 0);
            velocity = new Vector3d(0, 0, 0);
        }

        /**Returns the summation of radi base on location of the two ellipsoids.*/
        public double GetRimDistance(Ellipsoid other)
        {
            //TODO:
            var average_orientation = (orientation + other.orientation) / 2;
            var transform = Transform.Rotation(average_orientation, new Vector3d(0, 0, 1), new Point3d(0,0,0));
            var c2c = new Vector3d(other.position - position);
            Vector3d R_c2c = transform*c2c;
            R_c2c.Unitize();
            var theta = Math.Abs(Math.Atan(R_c2c.Z / Math.Sqrt(Math.Pow(R_c2c.X, 2) + Math.Pow(R_c2c.Y, 2))));
            double rim1 = Math.Sqrt(1 / (Math.Pow(R_c2c.X / radiusA, 2) + Math.Pow(R_c2c.Y / radiusB, 2) + Math.Pow(R_c2c.Z / radiusC, 2))); 
            double rim2 = Math.Sqrt(1 / (Math.Pow(R_c2c.X / other.radiusA, 2) + Math.Pow(R_c2c.Y / other.radiusB, 2) + Math.Pow(R_c2c.Z / other.radiusC, 2)));
            return rim1+rim2;
        }

        //public static bool BroadPhaseCollision(Ellipsoid i, Ellipsoid j)
        //{
        //    BoundingBox bBoxi = new BoundingBox(i.bBox.GetCorners());
        //    bBoxi.Transform(Transform.Translation(i.position));
        //    bBoxi.Transform(Transform.Rotation(Vector3d.ZAxis,i.orientation,(Point3d)i.position));
        //    BoundingBox bBoxj = new BoundingBox(j.bBox.GetCorners());
        //    bBoxj.Transform(Transform.Translation(j.position));
        //    bBoxj.Transform(Transform.Rotation(Vector3d.ZAxis, j.orientation, (Point3d)j.position));
        //    double minXi = bBoxi.Min.X;
        //    double maxXi = bBoxi.Max.X;
        //    double minXj = bBoxj.Min.X;
        //    double maxXj = bBoxj.Max.X;
        //    if (maxXi < minXj || maxXj < minXi) return false;

        //    double minYi = bBoxi.Min.Y;
        //    double maxYi = bBoxi.Max.Y;
        //    double minYj = bBoxj.Min.Y;
        //    double maxYj = bBoxj.Max.Y;
        //    if (maxYi < minYj || maxYj < minYi) return false;

        //    double minZi = bBoxi.Min.Z;
        //    double maxZi = bBoxi.Max.Z;
        //    double minZj = bBoxj.Min.Z;
        //    double maxZj = bBoxj.Max.Z;
        //    if (maxZi < minZj || maxZj < minZi) return false;
        //    return true;
        //}
    }

    public class EllipsoidGoo : GH_Goo<Ellipsoid>
    {
        public EllipsoidGoo()
        {
            Value = new Ellipsoid();
        }

        public EllipsoidGoo(double x, double y, double z)
        {
            Value = new Ellipsoid(x, y, z);
        }

        public EllipsoidGoo(Point3d point)
        {
            Value = new Ellipsoid( point);
        }

        public EllipsoidGoo(Ellipsoid e)
        {
            Value = new Ellipsoid(e);
        }

        public static List<EllipsoidGoo> EllipsoidGooList(List<Ellipsoid> ellipsoids)
        {
            List<EllipsoidGoo> ellipsoidGoos = new List<EllipsoidGoo>();
            for ( int i =0; i<ellipsoids.Count;i++)
            {
                ellipsoidGoos.Add(new EllipsoidGoo(ellipsoids[i]));
            }
            return ellipsoidGoos;
        }

        public override bool IsValid
        {
            get { return true; }
        }

        public override IGH_Goo Duplicate()
        {
            return DuplicateEllipsoid();
        }

        public EllipsoidGoo DuplicateEllipsoid()
        {
            return new EllipsoidGoo(Value);
        }

        public override string TypeName => "Ellipsoid";

        public override string ToString()
        {
            return "Ellipsoid";
        }

        public override string TypeDescription => "Contains a collection of ellipsoids.";
    }

    public class EllipsoidParameter : GH_Param<EllipsoidGoo>
    {
        public EllipsoidParameter()
            : base(new GH_InstanceDescription("Ellipsoid", "E", "Contains a collection of ellipsoids.", "PolyBrick", "Parameters"))
        {
        }

        protected override Bitmap Icon
        {
            get { return Resource1.PolyBrickIcons_47; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("030f7391-d2ec-46f1-9728-5ba6b19ee414"); }
        }

    }

    
}
