using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using Rhino;

namespace PolyBrick.EllipsoidPacking
{
    class PackEllipsoid_NoSpatialPartioning
    {
        public int? collisions;
        public List<Ellipsoid> ellipsoids = new List<Ellipsoid>();
        public List<int> collision_index;

        public PackEllipsoid_NoSpatialPartioning(int number)
        {
            this.Initiate(number);
            this.collisions = null;
        }

        public void Initiate(int number)
        {
            if (EGlobals.EXISTING_POINTS.Count != 0)
            {
                foreach (Point3d point in EGlobals.EXISTING_POINTS)
                {
                    Ellipsoid existingEllipsoid = new Ellipsoid(point);
                    existingEllipsoid.CheckBorder();
                    this.ellipsoids.Add(existingEllipsoid);
                }
            }
            for (int i = 0; i < number; i++)
            {
                this.ellipsoids.Add(Ellipsoid.RandomEllipsoid());
            }
        }

        public void pack()
        {
            this.collisions = 0;
            List<Vector3d> separate_forces = new List<Vector3d>();
            List<int> near_ellipsoids = new List<int>();
            collision_index = new List<int>();
            for (int i = 0; i < this.ellipsoids.Count; i++)
            {
                separate_forces.Add(new Vector3d(0, 0, 0));
                near_ellipsoids.Add(0);
                CheckBorders(i);

                //TODO:STRESS

                //Stress stress = EGlobals.FEBackGround.interpolate(new Point3d(ellipsoids[i].position));
                //if (EGlobals.HAS_GRADIENT) {
                //    ellipsoids[i].UpdateRadius(stress);
                //    ellipsoids[i].UpdateOrientation(stress);
                //}



                //TODO: Update Ellipsoid Orentation
            }
            for (int i  = 0; i< this.ellipsoids.Count; i++)
            {
                for (int j =i+1; j<ellipsoids.Count; j++)
                {
                    var ellipsoid_i = ellipsoids[i];
                    var ellipsoid_j = ellipsoids[j];
                    double d = new Point3d(ellipsoid_i.position).DistanceTo(new Point3d(ellipsoid_j.position));
                    if (d < (ellipsoid_i.GetRimDistance(ellipsoid_j) * 0.95))
                    {
                        ApplySeparationForcesToEllipsoid(ellipsoid_i, ellipsoid_j, separate_forces, near_ellipsoids, i, j);
                        collision_index.Add(i); //Debug
                        collision_index.Add(j); //Debug 
                    }
                }
            }
            

            for (int i = EGlobals.EXISTING_POINTS.Count; i < this.ellipsoids.Count; i++)
            {
                double length = separate_forces[i].Length;
                if (length > 0) //Need this to control step size??
                {
                    //separate_forces[i] = separate_forces[i] * Globals.MAX_SPEED / length; //Force option 1
                    separate_forces[i] = separate_forces[i] / near_ellipsoids[i]; //Force option 2
                    //separate_forces[i] = separate_forces[i]; //Force option 3
                    ellipsoids[i].ApplyForce(separate_forces[i]);
                    //TODO: Delete from previous list
                    this.ellipsoids[i].Move(); //Update only when there is force
                }

                //CheckBorders(i);
            }
            foreach (int element in near_ellipsoids)
            {
                this.collisions = this.collisions + element;
            }
        }
        public void CheckBorders(int i)
        {
            Ellipsoid ellipsoid_i = this.ellipsoids[i];
            Point3d center = new Point3d(ellipsoid_i.position);
            bool inside = EGlobals.BOUNDARY.IsPointInside(center, RhinoMath.SqrtEpsilon, true);
            if (!inside)
            {
                Point3d closestPoint = EGlobals.BOUNDARY.ClosestPoint(center);
                ellipsoid_i.position = new Vector3d(closestPoint);
            }
        }

        //public void UpdateEllipsoidRadius(int i)
        //{
        //    this.ellipsoids[i].UpdateRadius();
        //}

        public void ApplySeparationForcesToEllipsoid(Ellipsoid ellipsoid_i, Ellipsoid ellipsoid_j, List<Vector3d> separate_forces, List<int> near_ellipsoids, int index_i, int index_j)
        {
            double d = new Point3d(ellipsoid_i.position).DistanceTo(new Point3d(ellipsoid_j.position));
            Vector3d force_ij = GetSeparationForce(ellipsoid_i, ellipsoid_j, d);
            separate_forces[index_i] = separate_forces[index_i] + force_ij;
            separate_forces[index_j] = separate_forces[index_j] - force_ij;
            near_ellipsoids[index_i]++;
            near_ellipsoids[index_j]++;

        }

        public static Vector3d GetSeparationForce(Ellipsoid ellipsoid_i, Ellipsoid ellipsoid_j, double d)
        {
            Vector3d result = new Vector3d(0, 0, 0);
            if (d > 0)
            {
                Vector3d diff = ellipsoid_i.position - ellipsoid_j.position;
                diff.Unitize();
                diff = diff * (ellipsoid_i.GetRimDistance(ellipsoid_j) - d); // move half of overlap distance?
                result = result + diff;
            }
            else if (d == 0)
            {
                result = GlobalRandom.RandomVector();
            }
            return result;
        }
    }

    
}
