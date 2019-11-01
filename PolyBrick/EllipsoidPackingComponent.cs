using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using PolyBrick.Params;

namespace PolyBrick.EllipsoidPacking
{
    public class EllipsoidPacking : GH_Component
    {


        public EllipsoidPacking()
          : base("Ellipsoid Packing", "Ellipsoid Packing",
              "Pack ellipsoids in a Brep.",
              "PolyBrick", "Packing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Start", "Start", "Start to solve.", GH_ParamAccess.item);
            pManager.AddPointParameter("Existing Points", "ExistingPoints", "Sphere Centers that already exist.", GH_ParamAccess.list);
            pManager[1].Optional = true;
            pManager.AddIntegerParameter("Initial number", "Init_Number", "Initial number of spheres.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Maximum radius", "Max_R", "Maximum axis along MaxPrinciple stress direction. If there is no gradient control, set this maximum radius for all the spheres", GH_ParamAccess.item);
            pManager.AddNumberParameter("Minimum radius", "Min_R", "Minimum axis along MaxPrinciple stress direction. If there is no gradient control, this value will be ignored.", GH_ParamAccess.item);
            //pManager.AddNumberParameter("Step Size", "Step_size", "Distance factor that each sphere moves in each iteration.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Maximum iterations", "Max_iterations", "Maximum iteration for computing.", GH_ParamAccess.item);
            pManager.AddBrepParameter("Boundary volume", "Boundary", "Boundary volume for sphere packing.", GH_ParamAccess.item);
            pManager.AddParameter(new TensorFieldParameter(),"TensorField", "TF", "Optional Tensor Field for packing control.", GH_ParamAccess.item);
            pManager[7].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.Register_PointParam("Centroid", "Centroid", "Center points of the spheres.", GH_ParamAccess.list);
            //pManager.Register_LineParam("Line", "Line", "Beams connecting centroids of kissing ellipsoids.", GH_ParamAccess.list);
            pManager.RegisterParam(new EllipsoidParameter(),"Ellipsoid", "Ellipsoid", "Packed ellipsoids.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool start = false;
            if (!DA.GetData(0, ref start)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Switch missing."); return; }
            List<Point3d> existingpoints = new List<Point3d>();
            DA.GetDataList(1, existingpoints);
            if (!DA.GetData(2, ref EGlobals.Initial_Number)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Initial number missing."); return; }
            if (!DA.GetData(3, ref EGlobals.MAX_RADIUS)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Maximum radius missing."); return; }
            if (!DA.GetData(4, ref EGlobals.MIN_RADIUS)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Minimum radius missing."); return; }
            //double Step_size = 0;
            //if (!DA.GetData(5, ref Step_size)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Step size missing."); return; }
            int Max_iterations = 0;
            if (!DA.GetData(5, ref Max_iterations)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Maximum iteration missing."); return; }
            if (!DA.GetData(6, ref EGlobals.BOUNDARY)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Boundary volume missing."); return; }
            EGlobals.HAS_TENSORFIELD = DA.GetData(7, ref EGlobals.TENSORFIELDGOO);

            Point3d min_corner = EGlobals.BOUNDARY.GetBoundingBox(false).Min;
            Point3d max_corner = EGlobals.BOUNDARY.GetBoundingBox(false).Max;
            EGlobals.BOUND_X_MIN = min_corner.X;
            EGlobals.BOUND_Y_MIN = min_corner.Y;
            EGlobals.BOUND_Z_MIN = min_corner.Z;
            EGlobals.BOUND_X_MAX = max_corner.X;
            EGlobals.BOUND_Y_MAX = max_corner.Y;
            EGlobals.BOUND_Z_MAX = max_corner.Z;

            double cell_size = EGlobals.MAX_RADIUS * 2 * 1.01;
            int x = (int)Math.Ceiling((EGlobals.BOUND_X_MAX - EGlobals.BOUND_X_MIN) / cell_size);
            int y = (int)Math.Ceiling((EGlobals.BOUND_Y_MAX - EGlobals.BOUND_Y_MIN) / cell_size);
            int z = (int)Math.Ceiling((EGlobals.BOUND_Z_MAX - EGlobals.BOUND_Z_MIN) / cell_size);


            if (start == true)
            {
                Grid grid = new Grid(x, y, z, cell_size); // Generate voxels
                EGlobals.EXISTING_POINTS = new List<Point3d>();
                if (existingpoints.Count != 0)
                {
                    foreach (Point3d point in existingpoints)
                    {
                        if (EGlobals.BOUNDARY.IsPointInside(point, EGlobals.MIN_RADIUS / 20.0, false))
                        {
                            EGlobals.EXISTING_POINTS.Add(point);
                        }
                    }
                }
                //EGlobals.DISTRIBUTION = Gradient;

                PackEllipsoid new_pack = new PackEllipsoid(EGlobals.Initial_Number); // Generate pack of ellipsoids

                //Put each ellipsoid into corresponding voxel
                for (int num = 0; num < new_pack.ellipsoids.Count; num++)
                {
                    Ellipsoid ellipsoid = new_pack.ellipsoids[num];
                    grid.Allocate(ellipsoid);
                    grid.ellipsoid_index.Add(ellipsoid, num);
                }

                int i = 0;
                int total_i = 0;
                List<EllipsoidGoo> last_ellipsoids = new List<EllipsoidGoo>();
                while (true)
                {
                    if (new_pack.collisions == 0)
                    {
                        last_ellipsoids = new List<EllipsoidGoo>();
                        foreach (Ellipsoid ellipsoid in new_pack.ellipsoids)
                        {
                            last_ellipsoids.Add(new EllipsoidGoo(ellipsoid));
                        }
                        Ellipsoid new_ellipsoid = Ellipsoid.RandomEllipsoid();
                        new_pack.ellipsoids.Add(new_ellipsoid);
                        grid.Allocate(new_ellipsoid);
                        grid.ellipsoid_index.Add(new_ellipsoid, new_pack.ellipsoids.Count - 1);
                        //Console.WriteLine("Generating new sphere!");
                        i = 0;
                    }

                    new_pack.pack(grid); // Collision detection and move 
                    i++;
                    total_i++;
                    if (i == Max_iterations && i != total_i)
                    {
                        DA.SetDataList(0, last_ellipsoids.ToArray()); //TODO:Output point list.
                        break;
                    }
                    else if (i == Max_iterations && i == total_i)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Too many initial spheres. Getting no result.");
                        break;
                    }
                }
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
                return Resource1.PolyBrickIcons_49;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("50691bc9-2d3c-4973-90a0-0844c2a65745"); }
        }
               
    }

    //public class Stress
    //{
    //    public static double element_size = 25;
    //    public Point3d location;
    //    public Vector3d compression;
    //    public Vector3d tension;

    //    public Stress(Point3d p, Vector3d v1, Vector3d v2)
    //    {
    //        location = p;
    //        compression = v1;
    //        tension = v2;
    //    }
    //}

    //public class Background
    //{
    //    public List<Stress>[,] fe_grid;
    //    protected double max_x;
    //    protected double min_x;
    //    protected double max_y;
    //    protected double min_y;
    //    protected double max_z;
    //    protected double min_z;
    //    protected static double unit_size = Stress.element_size;
    //    public double max_stress=Double.MinValue;

    //    public Background(List<Stress> stresses)
    //    {
    //        max_x = Double.MinValue;
    //        min_x = Double.MaxValue;
    //        max_y = Double.MinValue;
    //        min_y = Double.MaxValue;
    //        max_z = Double.MinValue;
    //        min_z = Double.MaxValue;
            
    //        foreach (Stress stress in stresses)
    //        {
    //            Point3d p = stress.location;
    //            if (p.X > max_x) { max_x = p.X; }
    //            if (p.X < min_x) { min_x = p.X; }
    //            if (p.Y > max_y) { max_y = p.Y; }
    //            if (p.Y < min_y) { min_y = p.Y; }
    //            if (p.Z > max_z) { max_z = p.Z; }
    //            if (p.Z < min_z) { min_z = p.Z; }
    //            if (stress.compression.Length > max_stress) { max_stress = stress.compression.Length; }
    //            if (stress.tension.Length > max_stress) { max_stress = stress.tension.Length; }
    //        }
    //        int x_count = (int)Math.Ceiling((max_x - min_x) / unit_size);
    //        int y_count = (int)Math.Ceiling((max_y - min_y) / unit_size);
    //        int z_count = (int)Math.Ceiling((max_z - min_z) / unit_size);
    //        fe_grid = new List<Stress>[x_count, y_count];
    //        foreach (Stress stress in stresses)
    //        {
    //            allocate(stress);
    //        }
        
    //    }

    //    public void allocate(Stress stress)
    //    {
    //        int pos_x = (int)Math.Floor((stress.location.X - min_x) / unit_size);
    //        int pos_y = (int)Math.Floor((stress.location.Y - min_y) / unit_size);
    //        int pos_z = (int)Math.Floor((stress.location.Z - min_z) / unit_size);
    //        fe_grid[pos_x, pos_y].Add(stress);
    //    }

    //    public List<Stress> getNearStress(Point3d p)
    //    {
    //        int pos_x = (int)Math.Floor((p.X - min_x) / unit_size);
    //        int pos_y = (int)Math.Floor((p.Y - min_y) / unit_size);
    //        List<Stress> l1 = fe_grid[pos_x, pos_y];
    //        List<Stress> l2 = fe_grid[pos_x+1, pos_y];
    //        List<Stress> l3 = fe_grid[pos_x, pos_y+1];
    //        List<Stress> l4 = fe_grid[pos_x+1, pos_y+1];
    //        List<Stress> near = new List<Stress>();
    //        near.AddRange(l1);
    //        near.AddRange(l2);
    //        near.AddRange(l3);
    //        near.AddRange(l4);
    //        return near;
    //    }

    //    public Stress interpolate(Point3d point)
    //    {
    //        // NOTICE: 2 dimensional currently
    //        Vector3d v1 = new Vector3d();
    //        Vector3d v2 = new Vector3d();
    //        List<Stress> near =getNearStress(point);
    //        foreach (Stress stress in near)
    //        {
    //            double dist = Math.Sqrt(Math.Pow(point.X - stress.location.X,2) + Math.Pow(point.Y - stress.location.Y,2));
    //            if (dist == 0){
    //                return stress;
    //            }
    //            else
    //            {
    //                v1 = v1 + stress.compression * (Math.Pow(dist, 2));
    //                v2 = v2 + stress.tension * (Math.Pow(dist, 2));
    //            }
    //        }
    //        return new Stress(point,v1,v2);
    //    }

    //}
}