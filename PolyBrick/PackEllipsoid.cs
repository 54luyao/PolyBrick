using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using Rhino;

namespace PolyBrick.EllipsoidPacking
{
    class PackEllipsoid
    {
        public int? collisions;
        public List<Ellipsoid> ellipsoids = new List<Ellipsoid>();
        public List<int> collision_index;

        public PackEllipsoid(int number)
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
                    Ellipsoid existingEllipsoid = new Ellipsoid(point, EGlobals.MIN_RADIUS);
                    existingEllipsoid.CheckBorder();
                    this.ellipsoids.Add(existingEllipsoid);
                }
            }
            for (int i = 0; i < number; i++)
            {
                this.ellipsoids.Add(Ellipsoid.RandomEllipsoid());
            }
        }

        public void pack(Grid grid)
        {
            this.collisions = 0;
            List<Vector3d> separate_forces = new List<Vector3d>();
            List<int> near_ellipsoids = new List<int>();
            collision_index = new List<int>();
            for (int i = 0; i < ellipsoids.Count; i++)
            {
                separate_forces.Add(new Vector3d(0, 0, 0));
                near_ellipsoids.Add(0);
            }
            for (int i = EGlobals.EXISTING_POINTS.Count; i < this.ellipsoids.Count; i++)
            {
                LinkedList<Ellipsoid> prev_list = grid.GetOneCell(ellipsoids[i]);
                prev_list.Remove(ellipsoids[i]);
                CheckBorders(i);
                grid.Allocate(ellipsoids[i]);
                if (EGlobals.HAS_TENSORFIELD)
                {
                    ellipsoids[i].UpdateSizeOrientation(EGlobals.TENSORFIELDGOO.Value);
                }

                //TODO:STRESS

                //Stress stress = EGlobals.FEBackGround.interpolate(new Point3d(ellipsoids[i].position));
                //if (EGlobals.HAS_GRADIENT) {
                //    ellipsoids[i].UpdateRadius(stress);
                //    ellipsoids[i].UpdateOrientation(stress);
                //}



                //TODO: Update Ellipsoid Orentation
            }

            for (int x = 0; x < grid.x_count; x++)
            {
                for (int y = 0; y < grid.y_count; y++)
                {
                    for (int z = 0; z < grid.z_count; z++)
                    {
                        LinkedList<Ellipsoid> cell = grid.cells[x, y, z];
                        if (cell.Count != 0)
                        {
                            Ellipsoid[] cell_array = new Ellipsoid[cell.Count];
                            cell.CopyTo(cell_array, 0);
                            List<Ellipsoid> neighbors = grid.GetNeighborCellEllipsoids(x, y, z);
                            for (int i = 0; i < cell.Count; i++)
                            {
                                Ellipsoid ellipsoid_i = cell_array[i];
                                int index_i = grid.ellipsoid_index[ellipsoid_i];
                                
                                double d;
                                for (int j = i + 1; j < cell.Count; j++)
                                {
                                    Ellipsoid ellipsoid_j = cell_array[j];
                                    d = new Point3d(ellipsoid_i.position).DistanceTo(new Point3d(ellipsoid_j.position));
                                    if (d < (ellipsoid_i.GetRimDistance(ellipsoid_j) * 0.95))
                                    {
                                        int index_j = grid.ellipsoid_index[ellipsoid_j];
                                        ApplySeparationForcesToEllipsoid (ellipsoid_i, ellipsoid_j, separate_forces, near_ellipsoids, index_i, index_j);
                                        collision_index.Add(index_i); //Debug
                                        collision_index.Add(index_j); //Debug 
                                    }
                                }
                                for (int k = 0; k < neighbors.Count; k++)
                                {
                                    Ellipsoid ellipsoid_k = neighbors[k];
                                    d = new Point3d(ellipsoid_i.position).DistanceTo(new Point3d(ellipsoid_k.position));
                                    if (d < (ellipsoid_i.GetRimDistance(ellipsoid_k) * 0.95))
                                    {
                                        int index_k = grid.ellipsoid_index[ellipsoid_k];
                                        ApplySeparationForcesToEllipsoid(ellipsoid_i, ellipsoid_k, separate_forces, near_ellipsoids, index_i, index_k);
                                        collision_index.Add(index_i); //Debug
                                        collision_index.Add(index_k); //Debug 
                                    }
                                }
                            }
                        }
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
                    LinkedList<Ellipsoid> prev_list = grid.GetOneCell(ellipsoids[i]);
                    prev_list.Remove(ellipsoids[i]);
                    this.ellipsoids[i].Move(); //Update only when there is force
                    if (EGlobals.HAS_TENSORFIELD)
                    {
                        ellipsoids[i].UpdateSizeOrientation(EGlobals.TENSORFIELDGOO.Value);
                    }
                    grid.Allocate(ellipsoids[i]);
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
                diff = diff * (ellipsoid_i.GetRimDistance(ellipsoid_j)  - d)*0.5; // move half of overlap distance?
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
