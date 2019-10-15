using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino;

namespace PolyBrick.LargeStep
{
    public class SpherePacking : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SpherePacking class.
        /// </summary>
        public SpherePacking()
          : base("SpherePacking S", "SpherePacking S",
              "Pack spheres in a Brep.",
              "PolyBrick", "SpherePacking")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Start", "Start", "Start to solve.", GH_ParamAccess.item);
            pManager.AddPointParameter("Initiate Point", "InitPoint", "Location of the newly generated spheres.", GH_ParamAccess.item);
            pManager.AddPointParameter("Existing Points", "ExistingPoints", "Sphere Centers that already exist.", GH_ParamAccess.list);
            pManager[2].Optional = true;
            pManager.AddIntegerParameter("Initial number", "Init_Number", "Initial number of spheres.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Maximum radius", "Max_radius", "Maximum radius of spheres. If there is no gradient control, set this maximum radius for all the spheres", GH_ParamAccess.item);
            pManager.AddNumberParameter("Minimum radius", "Min_radius", "Minimum radius of spheres. If there is no gradient control, this value will be ignored.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Step Size", "Step_size", "Distance factor that each sphere moves in each iteration.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Maximum iterations", "Max_iterations", "Maximum iteration for computing.", GH_ParamAccess.item);
            pManager.AddBrepParameter("Boundary volume", "Boundary", "Boundary volume for sphere packing.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Gradient", "Gradient", "Colored mesh that controls the radius of spheres. The darker the larger.", GH_ParamAccess.item);
            pManager[9].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_PointParam("Centroid", "Centroid", "Center points of the spheres.", GH_ParamAccess.list);
            pManager.Register_DoubleParam("Radius", "Radius", "Radius of the spheres.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool start = false;
            if (!DA.GetData(0, ref start)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Switch missing."); return; }
            if (!DA.GetData(1, ref Globals.INIT_POINT)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Initiate point missing."); return; }
            List<Point3d> existingpoints = new List<Point3d>();
            DA.GetDataList(2, existingpoints);
            if (!DA.GetData(3, ref Globals.Initial_Number)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Initial number missing."); return; }
            if (!DA.GetData(4, ref Globals.MAX_RADIUS)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Maximum radius missing."); return; }
            if (!DA.GetData(5, ref Globals.MIN_RADIUS)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Minimum radius missing."); return; }
            double Step_size = 0;
            if (!DA.GetData(6, ref Step_size)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Step size missing."); return; }
            int Max_iterations = 0;
            if (!DA.GetData(7, ref Max_iterations)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Maximum iteration missing."); return; }
            if (!DA.GetData(8, ref Globals.BOUNDARY)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Boundary volume missing."); return; }
            Mesh Gradient = new Mesh();
            Globals.HAS_GRADIENT = DA.GetData(9, ref Gradient);

            Globals.MAX_SPEED = Step_size;
            Globals.MAX_FORCE = Step_size;

            Point3d min_corner = Globals.BOUNDARY.GetBoundingBox(false).Min;
            Point3d max_corner = Globals.BOUNDARY.GetBoundingBox(false).Max;
            Globals.BOUND_X_MIN = min_corner.X;
            Globals.BOUND_Y_MIN = min_corner.Y;
            Globals.BOUND_Z_MIN = min_corner.Z;
            Globals.BOUND_X_MAX = max_corner.X;
            Globals.BOUND_Y_MAX = max_corner.Y;
            Globals.BOUND_Z_MAX = max_corner.Z;

            double cell_size = Globals.MAX_RADIUS * 2* 1.01;
            int x = (int)Math.Ceiling((Globals.BOUND_X_MAX - Globals.BOUND_X_MIN) / cell_size);
            int y = (int)Math.Ceiling((Globals.BOUND_Y_MAX - Globals.BOUND_Y_MIN) / cell_size);
            int z = (int)Math.Ceiling((Globals.BOUND_Z_MAX - Globals.BOUND_Z_MIN) / cell_size);
                       

            if (start == true)
            {
                Grid grid = new Grid(x, y, z, cell_size);
                Globals.EXISTING_POINTS = new List<Point3d>();
                if (existingpoints.Count != 0)
                {
                    foreach (Point3d point in existingpoints)
                    {
                        if (Globals.BOUNDARY.IsPointInside(point, Globals.MIN_RADIUS / 20.0, false))
                        {
                            Globals.EXISTING_POINTS.Add(point);
                        }
                    }
                }
                Globals.initX = Globals.INIT_POINT.X;
                Globals.initY = Globals.INIT_POINT.Y;
                Globals.initZ = Globals.INIT_POINT.Z;
                Globals.DISTRIBUTION = Gradient;

                Pack new_pack = new Pack(Globals.Initial_Number);

                for (int num=0; num<new_pack.circles.Count; num++)
                {
                    Circle circle = new_pack.circles[num];
                    grid.Allocate(circle);
                    grid.circle_index.Add(circle, num);
                }

                int i = 0;
                int total_i = 0;
                List<Point3d> last_points = new List<Point3d>();
                List<double> last_radius = new List<double>();
                while (true)
                {
                    if (new_pack.collisions == 0)
                    {
                        last_points = new List<Point3d>();
                        last_radius = new List<double>();
                        foreach (Circle circle in new_pack.circles)
                        {
                            last_points.Add(new Point3d(circle.position));
                            last_radius.Add(circle.radius);
                        }
                        Circle new_circle = new Circle(Globals.initX, Globals.initY, Globals.initZ, Globals.MIN_RADIUS);
                        new_pack.circles.Add(new_circle);
                        grid.Allocate(new_circle);
                        grid.circle_index.Add(new_circle, new_pack.circles.Count - 1);
                        Console.WriteLine("Generating new sphere!");
                        i = 0;
                    }
                    if (total_i < 100)
                    {
                        Globals.MAX_SPEED = Globals.MAX_RADIUS;
                        Globals.MAX_FORCE = Globals.MAX_RADIUS;
                    }
                    else
                    {
                        Globals.MAX_SPEED = Step_size;
                        Globals.MAX_FORCE = Step_size;
                    }
                    //grid.circle_index.Clear();
                    //for (int xc = 0; xc < grid.x_count; xc++)
                    //{
                    //    for (int yc = 0; yc < grid.y_count; yc++)
                    //    {
                    //        for (int zc = 0; zc < grid.z_count; zc++)
                    //        {
                    //            grid.cells[xc, yc, zc] = new List<Circle>();
                    //        }
                    //    }
                    //}
                    //for (int j = 0; j < new_pack.circles.Count; j++)
                    //{
                    //    Circle circle = new_pack.circles[j];
                    //    int pos_x = (int)Math.Floor((circle.position.X - Globals.BOUND_X_MIN) / cell_size);
                    //    if (pos_x >= grid.x_count) pos_x= grid.x_count-1;
                    //    if (pos_x < 0) pos_x = 0;
                    //    int pos_y = (int)Math.Floor((circle.position.Y - Globals.BOUND_Y_MIN) / cell_size);
                    //    if (pos_y >= grid.x_count) pos_y = grid.y_count - 1;
                    //    if (pos_y < 0) pos_y = 0;
                    //    int pos_z = (int)Math.Floor((circle.position.Z - Globals.BOUND_Z_MIN) / cell_size);
                    //    if (pos_z >= grid.z_count) pos_z = grid.z_count - 1;
                    //    if (pos_z < 0) pos_z = 0;
                    //    grid.Allocate(pos_x, pos_y, pos_z, circle, j);
                    //}
                    new_pack.pack(grid);
                    i++;
                    total_i++;
                    if (i == Max_iterations && i != total_i)
                    {
                        Console.WriteLine("Break without converge!");
                        //foreach (Circle circle in new_pack.circles){
                        //  last_points.Add(new Point3d(circle.position));
                        //  last_radius.Add(circle.radius);
                        //  Print(circle.radiusFactor(circle).ToString());
                        //  Print(Globals.DISTRIBUTION.ClosestMeshPoint(new Point3d(circle.position), 0.0).Point.ToString());
                        //Color color = Globals.DISTRIBUTION.ColorAt(Globals.DISTRIBUTION.ClosestMeshPoint(new Point3d(circle.position), 0.0));
                        //Print(color.ToString());
                        //Print(((double)color.R).ToString());
                        //Print((((double)color.R)/255.0).ToString());
                        //}
                        DA.SetDataList(0, last_points.ToArray());
                        DA.SetDataList(1, last_radius.ToArray());
                        break;
                    }
                    else if (i == Max_iterations && i == total_i)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Too many initial spheres. Getting no result.");
                        break;
                    }
                }
                Console.WriteLine("Finish packing in " + total_i + " iterations");
                Console.WriteLine(new_pack.circles.Count + " spheres in total");
                Console.WriteLine("Initial number: " + Globals.Initial_Number);
                Console.WriteLine("Max radius: " + Globals.MAX_RADIUS);
                Console.WriteLine("Min radius: " + Globals.MIN_RADIUS);
                Console.WriteLine("Max iterations: " + Max_iterations);
            }
        }
    

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resource1.spicon1;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("c8481678-f32b-425f-985b-41618f06b528"); }
        }

        private class Globals
        {
            public static bool HAS_GRADIENT;
            public static int Initial_Number;
            public static double MAX_RADIUS;
            public static double MIN_RADIUS;
            public static double MAX_SPEED;
            public static double MAX_FORCE;
            public static Brep BOUNDARY;
            public static Point3d INIT_POINT;
            public static double initX;
            public static double initY;
            public static double initZ;
            public static Mesh DISTRIBUTION;
            public static Random rand = new System.Random();
            public static List<Point3d> EXISTING_POINTS = new List<Point3d>();
            public static double BOUND_X_MIN;
            public static double BOUND_Y_MIN;
            public static double BOUND_Z_MIN;
            public static double BOUND_X_MAX;
            public static double BOUND_Y_MAX;
            public static double BOUND_Z_MAX;
        }

        public class Circle
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
                this.velocity = new Vector3d(0, 0, 0);
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

        public class Pack
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

            public void pack(Grid grid)
            {
                this.collisions = 0;
                List<Vector3d> separate_forces = new List<Vector3d>();
                List<int> near_circles = new List<int>();
                //List<Vector3d> separate_forces = new List<Vector3d>();
                //List<int> near_circles = new List<int>();
                for (int i = 0; i < this.circles.Count; i++)
                {
                    separate_forces.Add(new Vector3d(0, 0, 0));
                    near_circles.Add(0);
                    //      this.checkBorders(i);
                }
                                               
                for (int x = 0; x < grid.x_count; x++)
                {
                    for (int y =0;y<grid.y_count; y++)
                    {
                        for (int z = 0; z < grid.z_count; z++)
                        {
                            List<Circle> cell = grid.cells[x, y, z];
                            if ( cell.Count != 0)
                            {
                                List<Circle> neighbors = grid.GetNeighborCellCircles(x, y, z);
                                for (int i = 0; i < cell.Count; i++)
                                {
                                    Circle circle_i = cell[i];
                                    int index_i = grid.circle_index[circle_i];
                                    CheckBorders(index_i);
                                    UpdateCircleRadius(index_i);
                                    double d;
                                    for (int j = i + 1; j < cell.Count; j++)
                                    {
                                        Circle circle_j = cell[j];
                                        d = new Point3d(circle_i.position).DistanceTo(new Point3d(circle_j.position));
                                        if (d < circle_i.radius + circle_j.radius)
                                        {
                                            int index_j = grid.circle_index[circle_j];
                                            ApplySeparationForcesToCircle(circle_i, circle_j, separate_forces, near_circles, index_i, index_j);
                                        }                                            
                                    }
                                    for (int k = 0; k < neighbors.Count; k++)
                                    {
                                        Circle circle_k = neighbors[k];
                                        d = new Point3d(circle_i.position).DistanceTo(new Point3d(circle_k.position));
                                        if (d < circle_i.radius + circle_k.radius)
                                        {
                                            int index_k = grid.circle_index[circle_k];
                                            ApplySeparationForcesToCircle(circle_i, circle_k, separate_forces, near_circles, index_i, index_k);
                                        }
                                            
                                    }
                                    
                                }
                            }
                            
                        }
                    }
                }
                
                for (int i = Globals.EXISTING_POINTS.Count; i < this.circles.Count; i++)
                {
                    double length = separate_forces[i].Length;
                    if (length > 0) //Need this to control step size??
                    {
                        separate_forces[i] = separate_forces[i] * Globals.MAX_SPEED / length;
                        circles[i].ApplyForce(separate_forces[i]);
                        //TODO: Delete from previous list
                        List<Circle> prev_list = grid.GetOneCell(circles[i]);
                        prev_list.Remove(circles[i]);
                        this.circles[i].Update(); //Update only when there is force
                        //TODO: Allocate 
                        grid.Allocate(circles[i]);                        
                    }
                    
                    
                    //CheckBorders(i);
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

            public void ApplySeparationForcesToCircle(Circle circle_i, Circle circle_j, List<Vector3d> separate_forces, List<int> near_circles,int index_i,int index_j)
            {                
                double d = new Point3d(circle_i.position).DistanceTo(new Point3d(circle_j.position));
                if (d < circle_i.radius + circle_j.radius)
                {
                    Vector3d force_ij = GetSeparationForce(circle_i, circle_j, d);                    
                    separate_forces[index_i] = separate_forces[index_i] + force_ij;
                    separate_forces[index_j] = separate_forces[index_j] - force_ij;
                    near_circles[index_i]++;
                    near_circles[index_j]++;
                }

                
                //this.circles[i].ApplyForce(separate_forces[i]);
                //this.circles[i].Update();
                //this.circles[i].velocity = new Vector3d(0, 0, 0);

            }

            public static Vector3d GetSeparationForce(Circle circle_i, Circle circle_j, double d)
            {
                Vector3d result = new Vector3d(0, 0, 0);
                if (d > 0 && d < circle_i.radius + circle_j.radius)
                {
                    Vector3d diff = circle_i.position - circle_j.position;
                    diff.Unitize();
                    diff = diff * (circle_i.radius + circle_j.radius - d); // move half of overlap distance?
                    result = result + diff;                    
                } else if (d == 0)
                {
                    result = Circle.RandomVector();
                }
                return result;
            }

        }

        public class Grid
        {
            public int x_count;
            public int y_count;
            public int z_count;
            public double cell_size;
            public List<Circle>[,,] cells;
            public Dictionary<Circle, int> circle_index;

            public Grid(int x, int y, int z, double size)
            {
                x_count = x;
                y_count = y;
                z_count = z;
                cell_size = size;
                cells = new List<Circle>[x, y, z];
                circle_index = new Dictionary<Circle, int>();
                for(int i = 0; i< x_count; i++)
                {
                    for (int j = 0; j< y_count; j++)
                    {
                        for (int k = 0; k< z_count; k++)
                        {
                            cells[i, j, k] = new List<Circle>();
                        }
                    }
                }
            }

            //public List<Circle> GetCircles(int x,int y,int z)
            //{
            //    return cells[x, y, z];
            //}

            public void Allocate(Circle circle)
            {
                //int x = (int)Math.Floor((circle.position.X -Globals.BOUND_X_MIN)/ cell_size);
                //int y = (int)Math.Floor((circle.position.Y - Globals.BOUND_Y_MIN) / cell_size);
                //int z = (int)Math.Floor((circle.position.Z - Globals.BOUND_Z_MIN) / cell_size);
                int pos_x = (int)Math.Floor((circle.position.X - Globals.BOUND_X_MIN) / cell_size);
                if (pos_x >= x_count) pos_x = x_count - 1;
                if (pos_x < 0) pos_x = 0;
                int pos_y = (int)Math.Floor((circle.position.Y - Globals.BOUND_Y_MIN) / cell_size);
                if (pos_y >= x_count) pos_y = y_count - 1;
                if (pos_y < 0) pos_y = 0;
                int pos_z = (int)Math.Floor((circle.position.Z - Globals.BOUND_Z_MIN) / cell_size);
                if (pos_z >= z_count) pos_z = z_count - 1;
                if (pos_z < 0) pos_z = 0;
                cells[pos_x, pos_y, pos_z].Add(circle);         
                //circle_index.Add(circle, i);
                
            }

            public List<Circle> GetNeighborCellCircles(int x, int y, int z)
            {
                List<Circle> c1 = GetOneCell(x - 1, y - 1, z - 1);
                List<Circle> c2 = GetOneCell(x, y - 1, z - 1);
                List<Circle> c3 = GetOneCell(x - 1, y - 1, z);
                List<Circle> c4 = GetOneCell(x, y - 1, z);
                List<Circle> c5 = GetOneCell(x - 1, y - 1, z + 1);
                List<Circle> c6 = GetOneCell(x, y - 1, z + 1);
                List<Circle> c7 = GetOneCell(x + 1, y - 1, z - 1);
                List<Circle> c8 = GetOneCell(x + 1, y - 1, z);
                List<Circle> c9 = GetOneCell(x + 1, y - 1, z + 1);
                List<Circle> c10 = GetOneCell(x - 1, y, z - 1);
                List<Circle> c11 = GetOneCell(x - 1, y, z);
                List<Circle> c12 = GetOneCell(x - 1, y, z + 1);
                List<Circle> c13 = GetOneCell(x, y, z + 1);

                List<Circle> neighbors = new List<Circle>();
                if (c1 != null) neighbors.AddRange(c1);
                if (c2 != null) neighbors.AddRange(c2);
                if (c3 != null) neighbors.AddRange(c3);
                if (c4 != null) neighbors.AddRange(c4);
                if (c5 != null) neighbors.AddRange(c5);
                if (c6 != null) neighbors.AddRange(c6);
                if (c7 != null) neighbors.AddRange(c7);
                if (c8 != null) neighbors.AddRange(c8);
                if (c9 != null) neighbors.AddRange(c9);
                if (c10 != null) neighbors.AddRange(c10);
                if (c11 != null) neighbors.AddRange(c11);
                if (c12 != null) neighbors.AddRange(c12);
                if (c13 != null) neighbors.AddRange(c13);

                return neighbors;
            }

            public List<Circle> GetOneCell(int x, int y, int z)
            {
                if (x < 0 || y < 0 || z < 0 || x > x_count - 1 || y > y_count - 1 || z > z_count - 1)
                {
                    return null;
                }
                return cells[x, y, z];
            }

            public List<Circle> GetOneCell(Circle circle)
            {
                int pos_x = (int)Math.Floor((circle.position.X - Globals.BOUND_X_MIN) / cell_size);
                if (pos_x >= x_count) pos_x = x_count - 1;
                if (pos_x < 0) pos_x = 0;
                int pos_y = (int)Math.Floor((circle.position.Y - Globals.BOUND_Y_MIN) / cell_size);
                if (pos_y >= x_count) pos_y = y_count - 1;
                if (pos_y < 0) pos_y = 0;
                int pos_z = (int)Math.Floor((circle.position.Z - Globals.BOUND_Z_MIN) / cell_size);
                if (pos_z >= z_count) pos_z = z_count - 1;
                if (pos_z < 0) pos_z = 0;
                return cells[pos_x, pos_y, pos_z];
            }
        }
    }
}