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
    class Circle
    {
        
            public Vector3d acceleration;
            public Vector3d velocity;
            public Vector3d position;
            public double radius;

            public Circle(double x, double y, double z, double radius)
            {
                this.acceleration = new Vector3d(0, 0, 0);
                this.velocity = RandomVector();
                this.position = new Vector3d(x, y, z);
                this.radius = radius;
            }

            public Circle(Point3d point, double radius)
            {
                this.acceleration = new Vector3d(0, 0, 0);
                this.velocity = RandomVector();
                this.position = new Vector3d(point);
                this.radius = radius;
            }

            public void ApplyForce(Vector3d force)
            {
                this.acceleration = this.acceleration + force;
            }

            public void CheckBorder()
            {
                Point3d center = new Point3d(this.position);
                Point3d closestPoint = Globals.BOUNDARY.ClosestPoint(center);
                this.position = new Vector3d(closestPoint);
            }

            public void Update()
            {
                this.velocity = this.velocity + this.acceleration;
                this.position = this.position + this.velocity;
                this.acceleration = new Vector3d(0, 0, 0);
            }

            public void UpdateRadius()
            {
                this.radius = this.RadiusFactor() * (Globals.MAX_RADIUS - Globals.MIN_RADIUS) + Globals.MIN_RADIUS;
            }

            public static Vector3d RandomVector()
            {

                double x = Globals.rand.NextDouble() * 2 - 1;
                //    Print(x);
                double y = Globals.rand.NextDouble() * 2 - 1;
                //    Print(y+"");
                double z = Globals.rand.NextDouble() * 2 - 1;
                //    Print(z+"");
                Vector3d result = new Vector3d(x, y, z);
                result.Unitize();
                //    Console.WriteLine(result.ToString());
                return result;
            }

            public double RadiusFactor()
            {
                if (!Globals.HAS_GRADIENT)
                {
                    return 1.0;
                }
                else
                {
                    //      double x = circle.position.X;
                    //      double y = circle.position.Y;
                    //      double z = circle.position.Z;
                    Point3d center = new Point3d(position);
                    MeshPoint cp = Globals.DISTRIBUTION.ClosestMeshPoint(center, 0.0);
                    return 1 - ((double)Globals.DISTRIBUTION.ColorAt(cp).R / 255.0);
                }
            }
        
    }
}
