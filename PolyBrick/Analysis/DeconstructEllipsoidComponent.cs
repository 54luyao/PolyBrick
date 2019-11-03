using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace PolyBrick.EllipsoidPacking
{
    public class DeconstructEllipsoidComponent : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Initializes a new instance of the DeconstructEllipsoidComponent class.
        /// </summary>
        public DeconstructEllipsoidComponent()
          : base("Deconstruct Ellipsoid", "DeEllipsoid",
              "Deconstruct ellipsoid.",
              "PolyBrick", "Ellipsoid")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new EllipsoidParameter(), "Ellipsoid", "E", "Ellipsoid to be deconstructed.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_PointParam("Centorid", "C", "Centroid of Ellipsoid.");
            pManager.Register_DoubleParam("RadiusX", "RX", "Radius along X direction in object space.");
            pManager.Register_DoubleParam("RadiusY", "RY", "Radius along Y direction in object space.");
            pManager.Register_DoubleParam("RadiusZ", "RZ", "Radius along Z direction in object space.");
            pManager.Register_VectorParam("Orientation", "O", "Orientation along Z direction in object space.");
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            EllipsoidGoo ellipsoidGoo = new EllipsoidGoo();
            DA.GetData(0, ref ellipsoidGoo);
            DA.SetData(0, ellipsoidGoo.Value.position);
            DA.SetData(1, ellipsoidGoo.Value.radiusA);
            DA.SetData(2, ellipsoidGoo.Value.radiusB);
            DA.SetData(3, ellipsoidGoo.Value.radiusC);
            DA.SetData(4, ellipsoidGoo.Value.orientation);
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
                return Resource1.PolyBrickIcons_51 ;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b37c6794-12d7-4253-bac0-6eb3b280c3a3"); }
        }
    }
}