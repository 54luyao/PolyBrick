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
    class Pack
    {
        public int? collisions;
        public List<Circle> circles = new List<Circle>();

        public Pack(int number)
        {
            this.Initiate(number);
            this.collisions = null;
        }

        public void Initiate(int number)
        {
            if (Globals.EXISTING_POINTS.Count != 0)
            {
                foreach (Point3d point in Globals.EXISTING_POINTS)
                {
                    Circle existingCircle = new Circle(point, Globals.MIN_RADIUS);
                    existingCircle.CheckBorder();
                    this.circles.Add(existingCircle);
                }
            }
            //    Console.WriteLine(this.circles.Count.ToString());
            for (int i = 0; i < number; i++)
            {
                this.circles.Add(new Circle(Globals.initX, Globals.initY, Globals.initZ, Globals.MIN_RADIUS));
            }
        }

        public void pack()
        {
            this.collisions = 0;
            List<Vector3d> separate_forces = new List<Vector3d>(this.circles.Count);
            List<int> near_circles = new List<int>(this.circles.Count);
            //List<Vector3d> separate_forces = new List<Vector3d>();
            //List<int> near_circles = new List<int>();
            for (int i = 0; i < this.circles.Count; i++)
            {
                separate_forces.Add(new Vector3d(0, 0, 0));
                near_circles.Add(0);
                //      this.checkBorders(i);
            }


            //    Parallel.For(0, 100, i => {
            //      this.checkBorders(i);
            //      this.updateCircleRadius(i);
            //      this.applySeparationForcesToCircle(i, separate_forces, near_circles);
            //      });
            for (int i = 0; i < this.circles.Count; i++)
            {
                this.CheckBorders(i);
                this.UpdateCircleRadius(i);
                this.ApplySeparationForcesToCircle(i, separate_forces, near_circles);
            }
            for (int i = Globals.EXISTING_POINTS.Count; i < this.circles.Count; i++)
            {
                this.circles[i].Update();
                this.circles[i].velocity = new Vector3d(0, 0, 0);
            }
            foreach (int element in near_circles)
            {
                this.collisions = this.collisions + element;
            }
        }

        public void CheckBorders(int i)
        {
            Circle circle_i = this.circles[i];
            Point3d center = new Point3d(circle_i.position);
            bool inside = Globals.BOUNDARY.IsPointInside(center, RhinoMath.SqrtEpsilon, true);
            if (!inside)
            {
                Point3d closestPoint = Globals.BOUNDARY.ClosestPoint(center);
                circle_i.position = new Vector3d(closestPoint);
            }
        }

        public void UpdateCircleRadius(int i)
        {
            this.circles[i].UpdateRadius();
        }

        public void ApplySeparationForcesToCircle(int i, List<Vector3d> separate_forces, List<int> near_circles)
        {
            Circle circle_i = this.circles[i];
            for (int j = i + 1; j < this.circles.Count; j++)
            {
                Circle circle_j = this.circles[j];
                double d = new Point3d(circle_i.position).DistanceTo(new Point3d(circle_j.position));
                if (d < circle_i.radius + circle_j.radius)
                {
                    Vector3d force_ij = GetSeparationForce(circle_i, circle_j, d);
                    separate_forces[i] = separate_forces[i] + force_ij;
                    separate_forces[j] = separate_forces[j] - force_ij;
                    near_circles[i]++;
                    near_circles[j]++;
                }
            }
            double length = separate_forces[i].Length;
            if (length > 0) //Need this to control step size??
            {
                separate_forces[i] = separate_forces[i] * Globals.MAX_SPEED / length;
            }
            this.circles[i].ApplyForce(separate_forces[i]);
            //this.circles[i].Update();
            //this.circles[i].velocity = new Vector3d(0, 0, 0);

        }

        public static Vector3d GetSeparationForce(Circle circle_i, Circle circle_j, double d)
        {
            Vector3d result = new Vector3d(0, 0, 0);
            if (d > 0 && d < circle_i.radius + circle_j.radius)
            {
                Vector3d diff = circle_i.position - circle_j.position;
                diff = diff / d * (circle_i.radius + circle_j.radius - d); // move half of overlap distance?
                result = result + diff;
            } else if (d == 0)
            {
                result = Circle.RandomVector();
            }
            return result;
        }

    }

}

