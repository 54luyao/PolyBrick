using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace PolyBrick
{
    public class SpherePackingComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public SpherePackingComponent()
          : base("Sphere Packing", "SpherePacking",
              "Pack spheres in a Brep.",
              "PolyBrick", "Packing")
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
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        /// 
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool start=false;
            if (!DA.GetData(0, ref start)){ AddRuntimeMessage( GH_RuntimeMessageLevel.Error,"Switch missing.") ; return; }
            if (!DA.GetData(1, ref Globals.INIT_POINT)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Initiate point missing."); return; }
            List<Point3d> existingpoints = new List<Point3d>();
            DA.GetDataList(2, existingpoints);
            if (!DA.GetData(3, ref Globals.Initial_Number)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Initial number missing."); return; }
            if (!DA.GetData(4, ref Globals.MAX_RADIUS)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Maximum radius missing."); return; }
            if (!DA.GetData(5, ref Globals.MIN_RADIUS)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Minimum radius missing."); return; }
            double Step_size=0;
            if (!DA.GetData(6, ref Step_size)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Step size missing."); return; }
            int Max_iterations = 0;
            if (!DA.GetData(7, ref Max_iterations)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Maximum iteration missing."); return; }
            if (!DA.GetData(8, ref Globals.BOUNDARY)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Boundary volume missing."); return; }
            Mesh Gradient=new Mesh();
            Globals.HAS_GRADIENT =DA.GetData(9, ref Gradient);

            Globals.MAX_SPEED = Step_size;
            Globals.MAX_FORCE = Step_size;


            
            if (start == true)
            {
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
                        new_pack.circles.Add(new Circle(Globals.initX, Globals.initY, Globals.initZ, Globals.MIN_RADIUS));
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
                    new_pack.pack();
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
                    } else if (i == Max_iterations && i == total_i)
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
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                return Resource1.spicon1;
                //return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("023569e8-523e-4ce5-a603-294b1f2e3355"); }
        }

        //private class Globals
        //{
        //    public static bool HAS_GRADIENT;
        //    public static int Initial_Number;
        //    public static double MAX_RADIUS;
        //    public static double MIN_RADIUS;
        //    public static double MAX_SPEED;
        //    public static double MAX_FORCE;
        //    public static Brep BOUNDARY;
        //    public static Point3d INIT_POINT;
        //    public static double initX;
        //    public static double initY;
        //    public static double initZ;
        //    public static Mesh DISTRIBUTION;
        //    public static Random rand = new System.Random();
        //    public static List<Point3d> EXISTING_POINTS = new List<Point3d>();
        //}

        //public class Circle
        //{
        //    public Vector3d acceleration;
        //    public Vector3d velocity;
        //    public Vector3d position;
        //    public double radius;

        //    public Circle(double x, double y, double z, double radius)
        //    {
        //        this.acceleration = new Vector3d(0, 0, 0);
        //        this.velocity = this.RandomVector();
        //        this.position = new Vector3d(x, y, z);
        //        this.radius = radius;
        //    }

        //    public Circle(Point3d point, double radius)
        //    {
        //        this.acceleration = new Vector3d(0, 0, 0);
        //        this.velocity = this.RandomVector();
        //        this.position = new Vector3d(point);
        //        this.radius = radius;
        //    }

        //    public void ApplyForce(Vector3d force)
        //    {
        //        this.acceleration = this.acceleration + force;
        //    }

        //    public void CheckBorder()
        //    {
        //        Point3d center = new Point3d(this.position);
        //        Point3d closestPoint = Globals.BOUNDARY.ClosestPoint(center);
        //        this.position = new Vector3d(closestPoint);
        //    }

        //    public void Update()
        //    {
        //        this.velocity = this.velocity + this.acceleration;
        //        this.position = this.position + this.velocity;
        //        this.acceleration = new Vector3d(0, 0, 0);
        //    }

        //    public void UpdateRadius()
        //    {
        //        this.radius = this.RadiusFactor() * (Globals.MAX_RADIUS - Globals.MIN_RADIUS) + Globals.MIN_RADIUS;
        //    }

        //    public Vector3d RandomVector()
        //    {

        //        double x = Globals.rand.NextDouble() * 2 - 1;
        //        //    Print(x);
        //        double y = Globals.rand.NextDouble() * 2 - 1;
        //        //    Print(y+"");
        //        double z = Globals.rand.NextDouble() * 2 - 1;
        //        //    Print(z+"");
        //        Vector3d result = new Vector3d(x, y, z);
        //        result.Unitize();
        //        //    Console.WriteLine(result.ToString());
        //        return result;
        //    }

        //    public double RadiusFactor()
        //    {
        //        if (!Globals.HAS_GRADIENT)
        //        {
        //            return 1.0;
        //        }
        //        else
        //        {
        //            //      double x = circle.position.X;
        //            //      double y = circle.position.Y;
        //            //      double z = circle.position.Z;
        //            Point3d center = new Point3d(position);
        //            MeshPoint cp = Globals.DISTRIBUTION.ClosestMeshPoint(center, 0.0);
        //            return 1 - ((double)Globals.DISTRIBUTION.ColorAt(cp).R / 255.0);
        //        }
        //    }
        //}

        //public class Pack
        //{

        //    public int? collisions;
        //    public List<Circle> circles = new List<Circle>();

        //    public Pack(int number)
        //    {
        //        this.Initiate(number);
        //        this.collisions = null;
        //    }

        //    public void Initiate(int number)
        //    {
        //        if (Globals.EXISTING_POINTS.Count != 0)
        //        {
        //            foreach (Point3d point in Globals.EXISTING_POINTS)
        //            {
        //                Circle existingCircle = new Circle(point, Globals.MIN_RADIUS);
        //                existingCircle.CheckBorder();
        //                this.circles.Add(existingCircle);
        //            }
        //        }
        //        //    Console.WriteLine(this.circles.Count.ToString());
        //        for (int i = 0; i < number; i++)
        //        {
        //            this.circles.Add(new Circle(Globals.initX, Globals.initY, Globals.initZ, Globals.MIN_RADIUS));
        //        }
        //    }

        //    public void pack()
        //    {
        //        this.collisions = 0;
        //        List<Vector3d> separate_forces = new List<Vector3d>(this.circles.Count);
        //        List<int> near_circles = new List<int>(this.circles.Count);
        //        //List<Vector3d> separate_forces = new List<Vector3d>();
        //        //List<int> near_circles = new List<int>();
        //        for (int i = 0; i < this.circles.Count; i++)
        //        {
        //            separate_forces.Add(new Vector3d(0, 0, 0));
        //            near_circles.Add(0);
        //            //      this.checkBorders(i);
        //        }


        //        //    Parallel.For(0, 100, i => {
        //        //      this.checkBorders(i);
        //        //      this.updateCircleRadius(i);
        //        //      this.applySeparationForcesToCircle(i, separate_forces, near_circles);
        //        //      });
        //        for (int i = 0; i < this.circles.Count; i++)
        //        {
        //            this.CheckBorders(i);
        //            this.UpdateCircleRadius(i);
        //            this.ApplySeparationForcesToCircle(i, separate_forces, near_circles);
        //        }
        //        for (int i = Globals.EXISTING_POINTS.Count; i < this.circles.Count; i++)
        //        {
        //            this.circles[i].Update();
        //            this.circles[i].velocity = new Vector3d(0, 0, 0);
        //        }
        //        foreach (int element in near_circles)
        //        {
        //            this.collisions = this.collisions + element;
        //        }
        //    }

        //    public void CheckBorders(int i)
        //    {
        //        Circle circle_i = this.circles[i];
        //        Point3d center = new Point3d(circle_i.position);
        //        bool inside = Globals.BOUNDARY.IsPointInside(center, RhinoMath.SqrtEpsilon, true);
        //        if (!inside)
        //        {
        //            Point3d closestPoint = Globals.BOUNDARY.ClosestPoint(center);
        //            circle_i.position = new Vector3d(closestPoint);
        //        }
        //    }

        //    public void UpdateCircleRadius(int i)
        //    {
        //        this.circles[i].UpdateRadius();
        //    }

        //    public void ApplySeparationForcesToCircle(int i, List<Vector3d> separate_forces, List<int> near_circles)
        //    {
        //        Circle circle_i = this.circles[i];
        //        for (int j = i + 1; j < this.circles.Count; j++)
        //        {
        //            Circle circle_j = this.circles[j];
        //            double d = new Point3d(circle_i.position).DistanceTo(new Point3d(circle_j.position));
        //            if (d < circle_i.radius + circle_j.radius)
        //            {
        //                Vector3d force_ij = GetSeparationForce(circle_i, circle_j, d);
        //                separate_forces[i] = separate_forces[i] + force_ij;
        //                separate_forces[j] = separate_forces[j] - force_ij;
        //                near_circles[i]++;
        //                near_circles[j]++;
        //            }
        //        }
        //        double length = separate_forces[i].Length;
        //        if (length > 0) //Need this to control step size??
        //        {
        //            separate_forces[i] = separate_forces[i] * Globals.MAX_SPEED / length;
        //        }
        //        this.circles[i].ApplyForce(separate_forces[i]);
        //        //this.circles[i].Update();
        //        //this.circles[i].velocity = new Vector3d(0, 0, 0);

        //    }

        //    public static Vector3d GetSeparationForce(Circle circle_i, Circle circle_j, double d)
        //    {
        //        Vector3d result = new Vector3d(0, 0, 0);
        //        if (d > 0 && d < circle_i.radius + circle_j.radius)
        //        {
        //            Vector3d diff = circle_i.position - circle_j.position;
        //            diff = diff / d * (circle_i.radius + circle_j.radius - d); // move half of overlap distance?
        //            result = result + diff;
        //        }
        //        return result;
        //    }

        //}
    }
}
