using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace PolyBrick.EllipsoidPacking
{
    public class EllipsoidToMeshComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the EllipsoidToMeshComponent class.
        /// </summary>
        public EllipsoidToMeshComponent()
          : base("EllipsoidToMesh", "EllipsoidToMesh",
              "Convert Ellipsoid to Mesh",
              "PolyBrick", "Convert")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new EllipsoidParameter(), "Ellipsoid", "E", "Ellipsoid to be converted.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_MeshParam("Mesh", "M", "Output Mesh.");
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            EllipsoidGoo eGoo = new EllipsoidGoo();
            if (!DA.GetData(0, ref eGoo)) return;
            Ellipsoid e = eGoo.Value;
            Mesh sphereMesh = Mesh.CreateQuadSphere(new Sphere(new Point3d(0,0,0),1),3);
            sphereMesh.Transform(Transform.Scale(Plane.WorldXY, e.radiusA, e.radiusB, e.radiusC));
            sphereMesh.Transform(Transform.Rotation(new Vector3d(0, 0, 1), e.orientation, new Point3d(0,0,0)));
            sphereMesh.Translate(e.position);
            DA.SetData(0, sphereMesh);
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
                return Resource1.PolyBrickIcons_48;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("f979b58e-7761-4892-9e3e-1dcb8c5d1126"); }
        }
    }
}