using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using PolyBrick.Params;
using Rhino;
using System.Linq;
using Rhino.Collections;

namespace PolyBrick.FEA
{
    public class StressCurveComponent : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        /// <summary>
        /// Initializes a new instance of the StressCurveComponent class.
        /// </summary>
        public StressCurveComponent()
          : base("Stress Curve", "Stress Curve",
              "Generate stress curves of a tensor field",
              "PolyBrick", "Tensor Field")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new TensorFieldParameter(), "TensorField", "TF", "Tensor Field to generate stress curves from.", GH_ParamAccess.item);
            pManager.AddPointParameter("Point", "P", "Seed points to grow stress curves from.", GH_ParamAccess.item);
            pManager.AddGeometryParameter("Boundary", "B", "Boundary of the stress curves.", GH_ParamAccess.item);
            pManager.AddNumberParameter("StepSize", "S", "Step size of curve segments", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_CurveParam("CurveMax", "CMax", "Maximum principal stress curves.", GH_ParamAccess.list);
            pManager.Register_CurveParam("CurveMid", "CMid", "Medium principal stress curves.", GH_ParamAccess.list);
            pManager.Register_CurveParam("CurveMin", "CMin", "Minimum principal stress curves.", GH_ParamAccess.list);
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
            Point3d seedPoint = new Point3d();
            DA.GetData(1, ref seedPoint);
            GeometryBase geometry = default(GeometryBase);
            DA.GetData(2, ref geometry);
            double stepSize=0;
            DA.GetData(3, ref stepSize);

            Mesh boundary;

            try
            {
                boundary = (Mesh)geometry;
            }
            catch
            {
                try
                {
                    Brep boundaryBrep = (Brep)geometry;
                    Mesh[] meshes = Mesh.CreateFromBrep(boundaryBrep, MeshingParameters.QualityRenderMesh);
                    Mesh convertedMesh = new Mesh();
                    foreach (Mesh m in meshes)
                    {
                        convertedMesh.Append(m);
                    }
                    boundary = convertedMesh;
                }
                catch
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid boundary input");
                    return;
                }
            }

            //Stop if seed point is not inside;
            if (!boundary.IsPointInside(seedPoint, RhinoMath.SqrtEpsilon, true)) return;

            Vector3d maxDirection = new Vector3d();
            Vector3d midDirection = new Vector3d();
            Vector3d minDirection = new Vector3d();
            Vector3d placeHolder1 = new Vector3d();
            Vector3d placeHolder2 = new Vector3d();
            getInterpolateVector(tensorField, seedPoint, ref maxDirection, ref midDirection, ref minDirection);

            Point3d currentMaxSeed1 = seedPoint;
            Point3d currentMaxSeed2 = seedPoint;
            Point3d lastSeedMax1 = seedPoint;
            Point3d lastSeedMax2 = seedPoint;
            Vector3d maxDirection1 = maxDirection;
            Vector3d maxDirection2 = -maxDirection;
            bool stopMax1 = false;
            bool stopMax2 = false;

            Point3d currentMidSeed1 = seedPoint;
            Point3d currentMidSeed2 = seedPoint;
            Point3d lastSeedMid1 = seedPoint;
            Point3d lastSeedMid2 = seedPoint;
            Vector3d midDirection1 = midDirection;
            Vector3d midDirection2 = -midDirection;
            bool stopMid1 = false;
            bool stopMid2 = false;

            Point3d currentMinSeed1 = seedPoint;
            Point3d currentMinSeed2 = seedPoint;
            Point3d lastSeedMin1 = seedPoint;
            Point3d lastSeedMin2 = seedPoint;
            Vector3d minDirection1 = minDirection;
            Vector3d minDirection2 = -minDirection;
            bool stopMin1 = false;
            bool stopMin2 = false;

            List<Line> maxLine = new List<Line>();
            List<Line> midLine = new List<Line>();
            List<Line> minLine = new List<Line>();

            double stopTolerance = 5;

            int k = 0;
            while (k < 1000)
            {
                if (stopMax1 && stopMax2) break;
                if (currentMaxSeed1.DistanceTo(currentMaxSeed2) < stopTolerance && k>50)
                {
                    maxLine.Add(new Line(currentMaxSeed1, currentMaxSeed2));
                    break;
                }
                if (!stopMax1)
                {
                    if (k == 0){
                        lastSeedMax1 = currentMaxSeed1;
                        currentMaxSeed1 = stepSize * unitize(maxDirection1) + currentMaxSeed1;

                        Line line1 = new Line(lastSeedMax1, currentMaxSeed1);
                        if (!boundary.IsPointInside(currentMaxSeed1, RhinoMath.SqrtEpsilon, true))
                        {
                            int[] ids;
                            Point3d[] intersections = Rhino.Geometry.Intersect.Intersection.MeshLine(boundary, line1, out ids);
                            if (intersections.Length == 0) break;
                            currentMaxSeed1 = intersections[0];
                            line1 = new Line(lastSeedMax1, currentMaxSeed1);
                            stopMax1 = true;
                        }
                        getInterpolateVector(tensorField, currentMaxSeed1, ref maxDirection1, ref placeHolder1, ref placeHolder2);
                        maxLine.Add(line1);
                    }
                    else
                    {
                        Point3d currentMaxSeed1_1 = stepSize * unitize(maxDirection1) + currentMaxSeed1;
                        Point3d currentMaxSeed1_2 = -stepSize * unitize(maxDirection1) + currentMaxSeed1;
                        if (lastSeedMax1.DistanceTo(currentMaxSeed1_1) < lastSeedMax1.DistanceTo(currentMaxSeed1_2))
                        {
                            lastSeedMax1 = currentMaxSeed1;
                            currentMaxSeed1 = currentMaxSeed1_2;
                        }
                        else
                        {
                            lastSeedMax1 = currentMaxSeed1;
                            currentMaxSeed1 = currentMaxSeed1_1;
                        }
                        Line line1 = new Line(lastSeedMax1, currentMaxSeed1);
                        if (!boundary.IsPointInside(currentMaxSeed1, RhinoMath.SqrtEpsilon, true))
                        {
                            int[] ids;
                            Point3d[] intersections = Rhino.Geometry.Intersect.Intersection.MeshLine(boundary, line1, out ids);
                            if (intersections.Length == 0) break;
                            currentMaxSeed1 = intersections[0];
                            line1 = new Line(lastSeedMax1, currentMaxSeed1);
                            stopMax1 = true;
                        }
                        getInterpolateVector(tensorField, currentMaxSeed1, ref maxDirection1, ref placeHolder1, ref placeHolder2);
                        maxLine.Add(line1);
                    }
                }
                if (!stopMax2)
                {
                    if (k == 0)
                    {
                        lastSeedMax2 = currentMaxSeed2;
                        currentMaxSeed2 = stepSize * unitize(maxDirection2) + currentMaxSeed2;

                        Line line2 = new Line(lastSeedMax2, currentMaxSeed2);
                        if (!boundary.IsPointInside(currentMaxSeed2, RhinoMath.SqrtEpsilon, true))
                        {
                            int[] ids;
                            Point3d[] intersections = Rhino.Geometry.Intersect.Intersection.MeshLine(boundary, line2, out ids);
                            if (intersections.Length == 0) break;
                            currentMaxSeed2 = intersections[0];
                            line2 = new Line(lastSeedMax2, currentMaxSeed2);
                            stopMax2 = true;
                        }
                        getInterpolateVector(tensorField, currentMaxSeed2, ref maxDirection2, ref placeHolder1, ref placeHolder2);
                        maxLine.Add(line2);
                    }
                    else
                    {
                        Point3d currentMaxSeed2_1 = stepSize * unitize(maxDirection2) + currentMaxSeed2;
                        Point3d currentMaxSeed2_2 = -stepSize * unitize(maxDirection2) + currentMaxSeed2;
                        if (lastSeedMax2.DistanceTo(currentMaxSeed2_1) < lastSeedMax2.DistanceTo(currentMaxSeed2_2))
                        {
                            lastSeedMax2 = currentMaxSeed2;
                            currentMaxSeed2 = currentMaxSeed2_2;
                        }
                        else
                        {
                            lastSeedMax2 = currentMaxSeed2;
                            currentMaxSeed2 = currentMaxSeed2_1;
                        }
                        Line line2 = new Line(lastSeedMax2, currentMaxSeed2);
                        if (!boundary.IsPointInside(currentMaxSeed2, RhinoMath.SqrtEpsilon, true))
                        {
                            int[] ids;
                            Point3d[] intersections = Rhino.Geometry.Intersect.Intersection.MeshLine(boundary, line2, out ids);
                            if (intersections.Length == 0) break;
                            currentMaxSeed2 = intersections[0];
                            line2 = new Line(lastSeedMax2, currentMaxSeed2);
                            stopMax2 = true;
                        }
                        getInterpolateVector(tensorField, currentMaxSeed2, ref maxDirection2, ref placeHolder1, ref placeHolder2);
                        maxLine.Add(line2);
                    }
                }
                k++;
            }

            k = 0;
            while (k < 1000)
            {
                if (stopMid1 && stopMid2) break;
                if (currentMidSeed1.DistanceTo(currentMidSeed2) < stopTolerance && k > 50)
                {
                    midLine.Add(new Line(currentMidSeed1, currentMidSeed2));
                    break;
                }
                if (!stopMid1)
                {
                    if (k == 0)
                    {
                        lastSeedMid1 = currentMidSeed1;
                        currentMidSeed1 = stepSize * unitize(midDirection1) + currentMidSeed1;

                        Line line1 = new Line(lastSeedMid1, currentMidSeed1);
                        if (!boundary.IsPointInside(currentMidSeed1, RhinoMath.SqrtEpsilon, true))
                        {
                            int[] ids;
                            Point3d[] intersections = Rhino.Geometry.Intersect.Intersection.MeshLine(boundary, line1, out ids);
                            if (intersections.Length == 0) break;
                            currentMidSeed1 = intersections[0];
                            line1 = new Line(lastSeedMid1, currentMidSeed1);
                            stopMid1 = true;
                        }
                        getInterpolateVector(tensorField, currentMidSeed1, ref placeHolder1, ref midDirection1, ref placeHolder2);
                        midLine.Add(line1);
                    }
                    else
                    {
                        Point3d currentMidSeed1_1 = stepSize * unitize(midDirection1) + currentMidSeed1;
                        Point3d currentMidSeed1_2 = -stepSize * unitize(midDirection1) + currentMidSeed1;
                        if (lastSeedMid1.DistanceTo(currentMidSeed1_1) < lastSeedMid1.DistanceTo(currentMidSeed1_2))
                        {
                            lastSeedMid1 = currentMidSeed1;
                            currentMidSeed1 = currentMidSeed1_2;
                        }
                        else
                        {
                            lastSeedMid1 = currentMidSeed1;
                            currentMidSeed1 = currentMidSeed1_1;
                        }
                        Line line1 = new Line(lastSeedMid1, currentMidSeed1);
                        if (!boundary.IsPointInside(currentMidSeed1, RhinoMath.SqrtEpsilon, true))
                        {
                            int[] ids;
                            Point3d[] intersections = Rhino.Geometry.Intersect.Intersection.MeshLine(boundary, line1, out ids);
                            if (intersections.Length == 0) break;
                            currentMidSeed1 = intersections[0];
                            line1 = new Line(lastSeedMid1, currentMidSeed1);
                            stopMid1 = true;
                        }
                        getInterpolateVector(tensorField, currentMidSeed1, ref placeHolder1, ref midDirection1, ref placeHolder2);
                        midLine.Add(line1);
                    }
                }
                if (!stopMid2)
                {
                    if (k == 0)
                    {
                        lastSeedMid2 = currentMidSeed2;
                        currentMidSeed2 = stepSize * unitize(midDirection2) + currentMidSeed2;

                        Line line2 = new Line(lastSeedMid2, currentMidSeed2);
                        if (!boundary.IsPointInside(currentMidSeed2, RhinoMath.SqrtEpsilon, true))
                        {
                            int[] ids;
                            Point3d[] intersections = Rhino.Geometry.Intersect.Intersection.MeshLine(boundary, line2, out ids);
                            if (intersections.Length == 0) break;
                            currentMidSeed2 = intersections[0];
                            line2 = new Line(lastSeedMid2, currentMidSeed2);
                            stopMid2 = true;
                        }
                        getInterpolateVector(tensorField, currentMidSeed2, ref placeHolder1, ref midDirection2, ref placeHolder2);
                        midLine.Add(line2);
                    }
                    else
                    {
                        Point3d currentMidSeed2_1 = stepSize * unitize(midDirection2) + currentMidSeed2;
                        Point3d currentMidSeed2_2 = -stepSize * unitize(midDirection2) + currentMidSeed2;
                        if (lastSeedMid2.DistanceTo(currentMidSeed2_1) < lastSeedMid2.DistanceTo(currentMidSeed2_2))
                        {
                            lastSeedMid2 = currentMidSeed2;
                            currentMidSeed2 = currentMidSeed2_2;
                        }
                        else
                        {
                            lastSeedMid2 = currentMidSeed2;
                            currentMidSeed2 = currentMidSeed2_1;
                        }
                        Line line2 = new Line(lastSeedMid2, currentMidSeed2);
                        if (!boundary.IsPointInside(currentMidSeed2, RhinoMath.SqrtEpsilon, true))
                        {
                            int[] ids;
                            Point3d[] intersections = Rhino.Geometry.Intersect.Intersection.MeshLine(boundary, line2, out ids);
                            if (intersections.Length == 0) break;
                            currentMidSeed2 = intersections[0];
                            line2 = new Line(lastSeedMid2, currentMidSeed2);
                            stopMid2 = true;
                        }
                        getInterpolateVector(tensorField, currentMidSeed2, ref placeHolder1, ref midDirection2, ref placeHolder2);
                        midLine.Add(line2);
                    }
                }
                k++;
            }

            k = 0;
            while (k < 1000)
            {
                if (stopMin1 && stopMin2) break;
                if (currentMinSeed1.DistanceTo(currentMinSeed2) < stopTolerance && k > 50)
                {
                    minLine.Add(new Line(currentMinSeed1, currentMinSeed2));
                    break;
                }
                if (!stopMin1)
                {
                    if (k == 0)
                    {
                        lastSeedMin1 = currentMinSeed1;
                        currentMinSeed1 = stepSize * unitize(minDirection1) + currentMinSeed1;

                        Line line1 = new Line(lastSeedMin1, currentMinSeed1);
                        if (!boundary.IsPointInside(currentMinSeed1, RhinoMath.SqrtEpsilon, true))
                        {
                            int[] ids;
                            Point3d[] intersections = Rhino.Geometry.Intersect.Intersection.MeshLine(boundary, line1, out ids);
                            if (intersections.Length == 0) break;
                            currentMinSeed1 = intersections[0];
                            line1 = new Line(lastSeedMin1, currentMinSeed1);
                            stopMin1 = true;
                        }
                        getInterpolateVector(tensorField, currentMinSeed1, ref placeHolder1, ref placeHolder2, ref minDirection1);
                        minLine.Add(line1);
                    }
                    else
                    {
                        Point3d currentMinSeed1_1 = stepSize * unitize(minDirection1) + currentMinSeed1;
                        Point3d currentMinSeed1_2 = -stepSize * unitize(minDirection1) + currentMinSeed1;
                        if (lastSeedMin1.DistanceTo(currentMinSeed1_1) < lastSeedMin1.DistanceTo(currentMinSeed1_2))
                        {
                            lastSeedMin1 = currentMinSeed1;
                            currentMinSeed1 = currentMinSeed1_2;
                        }
                        else
                        {
                            lastSeedMin1 = currentMinSeed1;
                            currentMinSeed1 = currentMinSeed1_1;
                        }
                        Line line1 = new Line(lastSeedMin1, currentMinSeed1);
                        if (!boundary.IsPointInside(currentMinSeed1, RhinoMath.SqrtEpsilon, true))
                        {
                            int[] ids;
                            Point3d[] intersections = Rhino.Geometry.Intersect.Intersection.MeshLine(boundary, line1, out ids);
                            if (intersections.Length == 0) break;
                            currentMinSeed1 = intersections[0];
                            line1 = new Line(lastSeedMin1, currentMinSeed1);
                            stopMin1 = true;
                        }
                        getInterpolateVector(tensorField, currentMinSeed1, ref placeHolder1, ref placeHolder2, ref minDirection1);
                        minLine.Add(line1);
                    }
                }
                if (!stopMin2)
                {
                    if (k == 0)
                    {
                        lastSeedMin2 = currentMinSeed2;
                        currentMinSeed2 = stepSize * unitize(minDirection2) + currentMinSeed2;

                        Line line2 = new Line(lastSeedMin2, currentMinSeed2);
                        if (!boundary.IsPointInside(currentMinSeed2, RhinoMath.SqrtEpsilon, true))
                        {
                            int[] ids;
                            Point3d[] intersections = Rhino.Geometry.Intersect.Intersection.MeshLine(boundary, line2, out ids);
                            if (intersections.Length == 0) break;
                            currentMinSeed2 = intersections[0];
                            line2 = new Line(lastSeedMin2, currentMinSeed2);
                            stopMin2 = true;
                        }
                        getInterpolateVector(tensorField, currentMinSeed2, ref placeHolder1, ref placeHolder2, ref minDirection2);
                        minLine.Add(line2);
                    }
                    else
                    {
                        Point3d currentMinSeed2_1 = stepSize * unitize(minDirection2) + currentMinSeed2;
                        Point3d currentMinSeed2_2 = -stepSize * unitize(minDirection2) + currentMinSeed2;
                        if (lastSeedMin2.DistanceTo(currentMinSeed2_1) < lastSeedMin2.DistanceTo(currentMinSeed2_2))
                        {
                            lastSeedMin2 = currentMinSeed2;
                            currentMinSeed2 = currentMinSeed2_2;
                        }
                        else
                        {
                            lastSeedMin2 = currentMinSeed2;
                            currentMinSeed2 = currentMinSeed2_1;
                        }
                        Line line2 = new Line(lastSeedMin2, currentMinSeed2);
                        if (!boundary.IsPointInside(currentMinSeed2, RhinoMath.SqrtEpsilon, true))
                        {
                            int[] ids;
                            Point3d[] intersections = Rhino.Geometry.Intersect.Intersection.MeshLine(boundary, line2, out ids);
                            if (intersections.Length == 0) break;
                            currentMinSeed2 = intersections[0];
                            line2 = new Line(lastSeedMin2, currentMinSeed2);
                            stopMin2 = true;
                        }
                        getInterpolateVector(tensorField, currentMinSeed2, ref placeHolder1, ref placeHolder2, ref minDirection2);
                        minLine.Add(line2);
                    }
                }
                k++;
            }

            List<LineCurve> lineCurveMax = new List<LineCurve>();
            List<LineCurve> lineCurveMid = new List<LineCurve>();
            List<LineCurve> lineCurveMin = new List<LineCurve>();
            foreach (Line l in maxLine)
            {
                lineCurveMax.Add(new LineCurve(l));
            }
            foreach (Line l in midLine)
            {
                lineCurveMid.Add(new LineCurve(l));
            }
            foreach (Line l in minLine)
            {
                lineCurveMin.Add(new LineCurve(l));
            }
            DA.SetDataList(0,Curve.JoinCurves(lineCurveMax));
            DA.SetDataList(1, Curve.JoinCurves(lineCurveMid));
            DA.SetDataList(2, Curve.JoinCurves(lineCurveMin));
        }

        private void getInterpolateVector(TensorField TF, Point3d point, ref Vector3d maxV, ref Vector3d midV, ref Vector3d minV)
        {
            /*
            int index = TF.Nodes.ClosestIndex(point);
            Tensor tensor = TF.Tensors[index];
            maxV = tensor.plane.XAxis * tensor.Magnitude_X;
            midV = tensor.plane.YAxis * tensor.Magnitude_Y;
            minV = tensor.plane.ZAxis * tensor.Magnitude_Z;
            

            
            int nearestCount = 5;

            var indices = Enumerable.Range(0, TF.Nodes.Count).ToArray();

            int[] sortedIndices = indices.OrderBy(index => TF.Nodes[index].DistanceTo(point)).ToArray();
            int[] closestIndices = sortedIndices.Take(nearestCount).ToArray();

            double totalWeigh = 0;
            double[] weighArray = new double[nearestCount];
            
            for (int i =0; i < nearestCount; i++)
            {
                int index = closestIndices[i];
                double curentWeigh = 1/Math.Pow(TF.Nodes[index].DistanceTo(point), 3);
                totalWeigh += curentWeigh;
                weighArray[i] = curentWeigh;
            }

            for (int i = 0; i < nearestCount; i++)
            {
                int index = closestIndices[i];
                Tensor tensor = TF.Tensors[index];
                maxV += tensor.plane.XAxis * tensor.Magnitude_X * weighArray[i] / totalWeigh;
                midV += tensor.plane.YAxis * tensor.Magnitude_Y * weighArray[i] / totalWeigh;
                minV += tensor.plane.ZAxis * tensor.Magnitude_Z * weighArray[i] / totalWeigh;
            }
            */
            maxV = new Vector3d();
            midV = new Vector3d();
            minV = new Vector3d();

            int searchRaius = 4;

            Sphere searchSphere = new Sphere(point, searchRaius);
            List<int> indices = new List<int>();
            TF.RTreeNodes.Search(searchSphere, (sender, args) => { indices.Add(args.Id); });

            if (indices.Count == 0)
            {
                int index = TF.Nodes.ClosestIndex(point);
                Tensor tensor = TF.Tensors[index];
                maxV = tensor.plane.XAxis * tensor.Magnitude_X;
                midV = tensor.plane.YAxis * tensor.Magnitude_Y;
                minV = tensor.plane.ZAxis * tensor.Magnitude_Z;
            }
            else
            {
                double totalWeigh = 0;
                double[] weighArray = new double[indices.Count];

                for (int i = 0; i < indices.Count; i++)
                {
                    int index = indices[i];
                    double curentWeigh = 1 / Math.Pow(TF.Tensors[index].plane.Origin.DistanceTo(point), 3);
                    totalWeigh += curentWeigh;
                    weighArray[i] = curentWeigh;
                }
                Tensor firstTensor = TF.Tensors[indices[0]];

                for (int i = 0; i < indices.Count; i++)
                {
                    int index = indices[i];
                    Tensor tensor = TF.Tensors[index];
                    Vector3d curMaxV = tensor.plane.XAxis;
                    Vector3d curMidV = tensor.plane.YAxis;
                    Vector3d curMinV = tensor.plane.ZAxis;
                    if (Vector3d.Multiply(curMaxV, firstTensor.plane.XAxis) < 0) curMaxV.Reverse();
                    if (Vector3d.Multiply(curMidV, firstTensor.plane.YAxis) < 0) curMidV.Reverse();
                    if (Vector3d.Multiply(curMinV, firstTensor.plane.ZAxis) < 0) curMinV.Reverse();
                    //maxV += curMaxV * Math.Abs(tensor.Magnitude_X) * weighArray[i] / totalWeigh;
                    //midV += curMidV * Math.Abs(tensor.Magnitude_Y) * weighArray[i] / totalWeigh;
                    //minV += curMinV * Math.Abs(tensor.Magnitude_Z) * weighArray[i] / totalWeigh;
                    maxV += curMaxV * tensor.Magnitude_X * weighArray[i] / totalWeigh;
                    midV += curMidV * tensor.Magnitude_Y * weighArray[i] / totalWeigh;
                    minV += curMinV * tensor.Magnitude_Z * weighArray[i] / totalWeigh;
                }
                maxV.Unitize();
                midV.Unitize();
                minV.Unitize();
                Plane helpPlane = new Plane(point, maxV);
                minV.Transform(Transform.PlanarProjection(helpPlane));
                midV = Vector3d.CrossProduct(maxV, minV);
            }
        }


        private Vector3d unitize(Vector3d v)
        {
            v.Unitize();
            return v;
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
                return Resource1.PolyBrickIcons_54;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("11d6e391-62f7-42ae-a6e9-b3a9be44af52"); }
        }
    }
}