using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace PolyBrick.Params
{
    public class DeconstructTensorField : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Initializes a new instance of the Deconstruct_Tensor_Field class.
        /// </summary>
        public DeconstructTensorField()
          : base("Deconstruct Tensor Field", "DeTensorField",
              "Deconstruct a tensor field",
              "PolyBrick", "Tensor Field")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new TensorFieldParameter(), "TensorField", "TF", "Tensor field to deconstruct.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_PlaneParam("Plane", "P", "Nodes in the tensor field.", GH_ParamAccess.list);
            pManager.Register_DoubleParam("MaxPrincipleStress", "MaxPS", "Maximum principle stresses.", GH_ParamAccess.list);
            pManager.Register_DoubleParam("MidPrincipleStress", "MidPS", "Middle principle stresses.", GH_ParamAccess.list);
            pManager.Register_DoubleParam("MinPrincipleStress", "MinPS", "Minimum principle stresses.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            TensorFieldGoo tensorFieldGoo = new TensorFieldGoo();
            if (!DA.GetData(0, ref tensorFieldGoo)) { return; }
            TensorField tensorField = tensorFieldGoo.Value;
            int count = tensorField.Nodes.Count;
            List<Plane> locations = new List<Plane>();
            List<double> maxS = new List<double>();
            List<double> midS = new List<double>();
            List<double> minS = new List<double>();
            //DA.SetDataList(0, tensorField.Nodes);
            for (int i =0;i <count; i++)
            {
                Tensor tensor = tensorField.Tensors[i];
                locations.Add(tensor.plane);
                maxS.Add(tensor.Magnitude_X);
                midS.Add(tensor.Magnitude_Y);
                minS.Add(tensor.Magnitude_Z);
            }
            //Point3d[] points= new Point3d[count];
            //tensorField.Nodes.CopyTo(points);
            
            DA.SetDataList(0, locations);
            DA.SetDataList(1, maxS);
            DA.SetDataList(2, midS);
            DA.SetDataList(3, minS);
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
                return Resource1.PolyBrickIcons_46;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b07963d5-cede-4a95-8bcd-482bd92c456b"); }
        }
    }
}