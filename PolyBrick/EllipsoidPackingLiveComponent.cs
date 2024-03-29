﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using PolyBrick.Params;
using Rhino;


namespace PolyBrick.EllipsoidPacking
{
    public class EllipsoidPackingLiveComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the EllipsoidPackingLiveComponent class.
        /// </summary>
        public EllipsoidPackingLiveComponent()
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
            pManager.AddIntegerParameter("Maximum iterations", "Max_iterations", "Maximum iteration for computing.", GH_ParamAccess.item);
            pManager.AddGeometryParameter("Boundary volume", "Boundary", "Boundary volume for sphere packing.Input closed Brep or Mesh.", GH_ParamAccess.item);
            pManager.AddParameter(new TensorFieldParameter(), "TensorField", "TF", "Optional Tensor Field for packing control.", GH_ParamAccess.item);
            pManager[7].Optional = true;
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

        int Initial_Number;
        List<Point3d> EXISTING_POINTS = null;
        Grid grid = null;
        int i = 0;
        int total_i = 0;
        List<EllipsoidGoo> last_ellipsoids;
        //GeometryBase lastBoundary;

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
            int Max_iterations = 0;
            if (!DA.GetData(5, ref Max_iterations)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Maximum iteration missing."); return; }
            //lastBoundary = (Brep)EGlobals.BOUNDARY;
            GeometryBase boundary = new Mesh();
            if (!DA.GetData(6, ref boundary)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Boundary volume missing."); return; }
            EGlobals.HAS_TENSORFIELD = DA.GetData(7, ref EGlobals.TENSORFIELDGOO);
            bool reset = false;
            if (!DA.GetData(8, ref reset)) return;

            double boundaryCheckTolerance = 0.001;

            try
            {
                EGlobals.BOUNDARY = (Mesh)boundary;
            } catch
            {
                try
                {
                    Brep boundaryBrep = (Brep)boundary;
                    Mesh[] meshes = Mesh.CreateFromBrep(boundaryBrep, MeshingParameters.QualityRenderMesh);
                    Mesh convertedMesh = new Mesh();
                    foreach(Mesh m in meshes)
                    {
                        convertedMesh.Append(m);
                    }
                    EGlobals.BOUNDARY = convertedMesh;
                } catch
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid boundary input");
                    return;
                }
            }
            if (!EGlobals.BOUNDARY.IsClosed) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Boundary is not closed."); return; }

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

            Message = "Paused";
            if (EXISTING_POINTS == null)
            {
                EXISTING_POINTS = new List<Point3d>();
                if (existingpoints.Count != 0)
                {
                    for (int index = 0; index < existingpoints.Count; index++)
                    {
                        if (EGlobals.BOUNDARY.IsPointInside(existingpoints[index], boundaryCheckTolerance, false))
                        {
                            EXISTING_POINTS.Add(existingpoints[index]);
                        }
                    }
                }
            }

            if (grid == null) grid = new Grid(x, y, z, cell_size); // Generate voxels

            if (new_pack == null)
            {
                //Existing point issue not solved
                new_pack = new PackEllipsoid(Initial_Number,EXISTING_POINTS);
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
                Message = "Reset";
                grid = new Grid(x, y, z, cell_size);
                i = 0;
                total_i = 0;

                EXISTING_POINTS = new List<Point3d>();
                if (existingpoints.Count != 0)
                {
                    for (int index = 0; index < existingpoints.Count; index++)
                    {
                        if (EGlobals.BOUNDARY.IsPointInside(existingpoints[index], boundaryCheckTolerance, false))
                        {
                            EXISTING_POINTS.Add(existingpoints[index]);
                        }
                    }
                }
                //Existing point issue not solved
                new_pack = new PackEllipsoid(Initial_Number, EXISTING_POINTS);
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
                    for (int index = 0; index < new_pack.ellipsoids.Count; index++)
                    {
                        last_ellipsoids.Add(new EllipsoidGoo(new_pack.ellipsoids[index]));
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
                    Message = "Converged";
                    //DA.SetDataList(0, last_ellipsoids.ToArray());
                }
                else if (i >= Max_iterations && i == total_i)
                {
                    Message = "Break";
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Too many initial spheres. Getting no result.");
                }
                else
                {
                    Message = "Running";
                    ExpireSolution(true);
                }
            }
            if (new_pack.ellipsoids != null)
            {
                if (i >= Max_iterations && i != total_i) DA.SetDataList(0, last_ellipsoids.ToArray());
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