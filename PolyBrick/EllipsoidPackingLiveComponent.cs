using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using PolyBrick.Params;

namespace PolyBrick.EllipsoidPacking
{
    public class EllipsoidPackingLiveComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the EllipsoidPackingLiveComponent class.
        /// </summary>
        public EllipsoidPackingLiveComponent()
          : base("Ellipsoid Packing Live", "Ellipsoid Packing Live",
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
            pManager.AddNumberParameter("Step Size", "Step_size", "Distance factor that each sphere moves in each iteration.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Maximum iterations", "Max_iterations", "Maximum iteration for computing.", GH_ParamAccess.item);
            pManager.AddBrepParameter("Boundary volume", "Boundary", "Boundary volume for sphere packing.", GH_ParamAccess.item);
            pManager.AddParameter(new TensorFieldParameter(), "TensorField", "TF", "Optional Tensor Field for packing control.", GH_ParamAccess.item);
            pManager[8].Optional = true;
            pManager.AddBooleanParameter("Reset", "Reset", "Reset Inputs", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.RegisterParam(new EllipsoidParameter(), "Ellipsoid", "Ellipsoid", "Packed ellipsoids.", GH_ParamAccess.list);
            pManager.Register_IntegerParam("Iteration", "i", "Current iteration", GH_ParamAccess.item);
        }

        bool HAS_TENSORFIELD;
        int Initial_Number;
        double MAX_SPEED;
        double MAX_FORCE;
        Random rand = new System.Random();
        List<Point3d> EXISTING_POINTS = null;
        double BOUND_X_MIN;
        double BOUND_Y_MIN;
        double BOUND_Z_MIN;
        double BOUND_X_MAX;
        double BOUND_Y_MAX;
        double BOUND_Z_MAX;
        Grid grid = null;
        int i = 0;
        int total_i = 0;
        List<EllipsoidGoo> last_ellipsoids;

        PackEllipsoid new_pack = null;
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
            if (!DA.GetData(2, ref Initial_Number)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Initial number missing."); return; }
            if (!DA.GetData(3, ref EGlobals.MAX_RADIUS)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Maximum radius missing."); return; }
            if (!DA.GetData(4, ref EGlobals.MIN_RADIUS)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Minimum radius missing."); return; }
            double Step_size = 0;
            if (!DA.GetData(5, ref Step_size)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Step size missing."); return; }
            int Max_iterations = 0;
            if (!DA.GetData(6, ref Max_iterations)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Maximum iteration missing."); return; }
            if (!DA.GetData(7, ref EGlobals.BOUNDARY)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Boundary volume missing."); return; }
            EGlobals.HAS_TENSORFIELD = DA.GetData(8, ref EGlobals.TENSORFIELDGOO);
            bool reset = false;
            if (!DA.GetData(9, ref reset)) return;


            //TODO:STRESS

            //EGlobals.FEBackGround = new Background(Gradient);

            MAX_SPEED = Step_size;
            MAX_FORCE = Step_size;

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

            if (EXISTING_POINTS == null)
            {
                EXISTING_POINTS = new List<Point3d>();
                if (existingpoints.Count != 0)
                {
                    foreach (Point3d point in existingpoints)
                    {
                        if (EGlobals.BOUNDARY.IsPointInside(point, EGlobals.MIN_RADIUS / 20.0, false))
                        {
                            EXISTING_POINTS.Add(point);
                        }
                    }
                }
            }

            if (grid == null) grid = new Grid(x, y, z, cell_size); // Generate voxels

            if (new_pack == null)
            {
                //Existing point issue not solved
                new_pack = new PackEllipsoid(Initial_Number);
                //Put each ellipsoid into corresponding voxel
                for (int num = 0; num < new_pack.ellipsoids.Count; num++)
                {
                    Ellipsoid ellipsoid = new_pack.ellipsoids[num];
                    grid.Allocate(ellipsoid);
                    grid.ellipsoid_index.Add(ellipsoid, num);
                }
            }

            if (reset)
            {
                grid = new Grid(x, y, z, cell_size);
                i = 0;
                total_i = 0;

                EXISTING_POINTS = new List<Point3d>();
                if (existingpoints.Count != 0)
                {
                    foreach (Point3d point in existingpoints)
                    {
                        if (EGlobals.BOUNDARY.IsPointInside(point, EGlobals.MIN_RADIUS / 20.0, false))
                        {
                            EXISTING_POINTS.Add(point);
                        }
                    }
                }
                //Existing point issue not solved
                new_pack = new PackEllipsoid(Initial_Number);
                for (int num = 0; num < new_pack.ellipsoids.Count; num++)
                {
                    Ellipsoid ellipsoid = new_pack.ellipsoids[num];
                    grid.Allocate(ellipsoid);
                    grid.ellipsoid_index.Add(ellipsoid, num);
                }
            }

            if (start == true)
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
                    i = 0;
                }
                new_pack.pack(grid); // Collision detection and move 
                i++;
                total_i++;
                if (i >= Max_iterations && i != total_i)
                {
                    //DA.SetDataList(0, last_ellipsoids.ToArray());
                }
                else if (i == Max_iterations && i == total_i)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Too many initial spheres. Getting no result.");
                }
                else
                {
                    ExpireSolution(true);
                }
            }
            if (new_pack.ellipsoids != null) {
                if(i >= Max_iterations && i != total_i) DA.SetDataList(0, last_ellipsoids.ToArray());
                else DA.SetDataList(0, EllipsoidGoo.EllipsoidGooList(new_pack.ellipsoids).ToArray());
            }
            DA.SetData(1, total_i);
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
                return Resource1.PolyBrickIcons_50;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2c1fa44d-740b-4d34-8b3f-5d065bed0b99"); }
        }

        internal PackEllipsoid New_pack { get => new_pack; set => new_pack = value; }
    }
}