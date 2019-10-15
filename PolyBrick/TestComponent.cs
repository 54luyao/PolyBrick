using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace PolyBrick
{
    public class TestComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TestComponent class.
        /// </summary>
        public TestComponent()
          : base("TestComponent", "Nickname",
              "Description",
              "PolyBrick", "Test")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("in", "in", "input integer", GH_ParamAccess.item);
            pManager.AddBooleanParameter("run", "run", "run", GH_ParamAccess.item);
            pManager.AddBooleanParameter("reset", "reset", "reset", GH_ParamAccess.item,false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_IntegerParam("out", "out", "output integer", GH_ParamAccess.item);
        }

        int number=0;
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int inNum = 0;
            if (!DA.GetData(0, ref inNum)) return;
            bool run = true;
            if (!DA.GetData(1, ref run)) return;
            bool reset = false;
            DA.GetData(2, ref reset);

            if (reset) number = inNum;
            if (run)
            {
                number++;
                ExpireSolution(true);
            }
            DA.SetData(0, number);
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
            get { return new Guid("41828999-3fec-496e-a5e1-8cbc8105ab25"); }
        }
    }
}