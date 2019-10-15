using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace PolyBrick.EllipsoidPacking
{
    public class EllipsoidPacking_NoSpatialPartitioning : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the EllipsoidPacking_NoSpatialPartitioning class.
        /// </summary>
        public EllipsoidPacking_NoSpatialPartitioning()
          : base("EllipsoidPacking_NoSpatialPartitioning", "EllipsoidPacking_NSP",
              "Pack ellipsoids without spatial partioning",
              "PolyBrick", "SpherePacking")
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
            pManager.AddGenericParameter("FEA", "FEA", "FEA stresses.", GH_ParamAccess.list);
            pManager[8].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_GenericParam("Ellipsoid", "Ellipsoid", "Packed ellipsoids.", GH_ParamAccess.list);
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
            double Step_size = 0;
            if (!DA.GetData(5, ref Step_size)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Step size missing."); return; }
            int Max_iterations = 0;
            if (!DA.GetData(6, ref Max_iterations)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Maximum iteration missing."); return; }
            if (!DA.GetData(7, ref EGlobals.BOUNDARY)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Boundary volume missing."); return; }
            //List<Stress> Gradient = new List<Stress>();
            EGlobals.HAS_TENSORFIELD = DA.GetData(8, ref EGlobals.TENSORFIELDGOO);


            //TODO:STRESS

            //EGlobals.FEBackGround = new Background(Gradient);



            EGlobals.MAX_SPEED = Step_size;
            EGlobals.MAX_FORCE = Step_size;

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

                PackEllipsoid_NoSpatialPartioning new_pack = new PackEllipsoid_NoSpatialPartioning(EGlobals.Initial_Number); // Generate pack of ellipsoids

                //Put each ellipsoid into corresponding voxel
                //Can combine this part with other loop?

                int i = 0;
                int total_i = 0;
                List<Ellipsoid> last_ellipsoids = new List<Ellipsoid>();
                while (true)
                {
                    if (new_pack.collisions == 0)
                    {
                        last_ellipsoids = new List<Ellipsoid>();
                        foreach (Ellipsoid ellipsoid in new_pack.ellipsoids)
                        {
                            last_ellipsoids.Add(new Ellipsoid(ellipsoid));
                        }
                        //Ellipsoid new_ellipsoid = Ellipsoid.RandomEllipsoid();
                        Ellipsoid new_ellipsoid = new Ellipsoid(0,0,0);
                        new_pack.ellipsoids.Add(new_ellipsoid);
                        i = 0;
                    }
                    if (total_i < 100)
                    {
                        EGlobals.MAX_SPEED = EGlobals.MAX_RADIUS;
                        EGlobals.MAX_FORCE = EGlobals.MAX_RADIUS;
                    }
                    else
                    {
                        EGlobals.MAX_SPEED = Step_size;
                        EGlobals.MAX_FORCE = Step_size;
                    }
                    new_pack.pack(); // Collision detection and move 
                    i++;
                    total_i++;
                    if (i == Max_iterations && i != total_i)
                    {
                        Console.WriteLine("Break without converge!");
                        DA.SetDataList(0, last_ellipsoids.ToArray()); //TODO:Output point list.
                        break;
                    }
                    else if (i == Max_iterations && i == total_i)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Too many initial spheres. Getting no result.");
                        break;
                    }
                }
                Console.WriteLine("Finish packing in " + total_i + " iterations");
                Console.WriteLine(new_pack.ellipsoids.Count + " spheres in total");
                Console.WriteLine("Initial number: " + EGlobals.Initial_Number);
                Console.WriteLine("Max radius: " + EGlobals.MAX_RADIUS);
                Console.WriteLine("Min radius: " + EGlobals.MIN_RADIUS);
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
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("15635890-2265-4957-b30f-b6029570d892"); }
        }
    }
}