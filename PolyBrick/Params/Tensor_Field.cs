using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Microsoft.VisualBasic.FileIO;
using System.Globalization;
using Grasshopper.Kernel;
using System.Drawing;
using Rhino.Collections;
//using MathNet.Numerics.Statistics;

namespace PolyBrick.Params
{
    public class Tensor
    {
        private double magnitude_x;
        private double magnitude_y;
        private double magnitude_z;
        public Plane plane;

        public double Magnitude_X
        {
            get { return magnitude_x; }
            set { magnitude_x = value; }
        }

        public double Magnitude_Y
        {
            get { return magnitude_y; }
            set { magnitude_y = value; }
        }

        public double Magnitude_Z
        {
            get { return magnitude_z; }
            set { magnitude_z = value; }
        }

        public Tensor()
        {
            magnitude_x = 0;
            magnitude_y = 0;
            magnitude_z = 0;
            plane = Plane.WorldXY;
        }

        public Tensor(Tensor tensor)
        {
            magnitude_x = tensor.magnitude_x;
            magnitude_y = tensor.magnitude_y;
            magnitude_z = tensor.magnitude_z;
            plane = tensor.plane;
        }

        public Tensor(Plane p, double mx,double my, double mz)
        {
            magnitude_x = mx;
            magnitude_y = my;
            magnitude_z = mz;
            plane = p;
        }

        public Tensor(double mx, double my, double mz, double rx, double ry, double rz, double x, double y, double z)
        {
            magnitude_x = mx;
            magnitude_y = my;
            magnitude_z = mz;
            plane = Plane.WorldXY;
            plane.Rotate(rx, plane.XAxis);
            plane.Rotate(ry, plane.YAxis);
            plane.Rotate(rz, plane.ZAxis);
            plane.OriginY = x;
            plane.OriginZ = y;
            plane.OriginX = z;
        }
    }

    public class TensorField
    {
        public readonly List<Tensor> Tensors;
        public readonly Point3dList Nodes;
        public readonly double MaxStress;
        public readonly double MinStress;
        public readonly double stressThreshold;
        
        public RTree RTreeNodes;
        double PERCENTILE = 0.85;
        public TensorField()
        {
            Tensors = new List<Tensor>();
            Nodes = new Point3dList();
            MaxStress = 0;
            MinStress = 0;
            RTreeNodes = new RTree();
        }

        public TensorField(List<Plane> planes, List<double> xs, List<double> ys, List<double> zs)
        {
            Tensors = new List<Tensor>();
            MaxStress = Double.MinValue;
            MinStress = Double.MaxValue;
            Nodes = new Point3dList();
            RTreeNodes = new RTree();
            List<double> allStressMagnitude = new List<double>();

            for (int i = 0; i < planes.Count; i++)
            {
                Tensor tensor = new Tensor(planes[i], xs[i], ys[i], zs[i]);
                Tensors.Add(tensor);
                //Nodes.Add(tensor.plane.Origin);
                //allStressMagnitude.Add(Math.Abs(xs[i]));
                //allStressMagnitude.Add(Math.Abs(ys[i]));
                //allStressMagnitude.Add(Math.Abs(zs[i]));
            }
            Tensors.OrderBy(i => i.Magnitude_X + i.Magnitude_Y + i.Magnitude_Z);
            for (int i = 0; i < Tensors.Count; i++)
            {
                Nodes.Add(Tensors[i].plane.Origin);
                RTreeNodes.Insert(planes[i].Origin, i);
            }
            List<double> stresses = new List<double>();
            stresses.AddRange(xs.Select(x=>Math.Abs(x)).ToList());
            stresses.AddRange(ys.Select(x => Math.Abs(x)).ToList());
            stresses.AddRange(zs.Select(x => Math.Abs(x)).ToList());
            stresses.Sort();
            MaxStress = stresses[stresses.Count - 1];
            MinStress = stresses[0];
            //Percentile percentile = new Percentile(stresses);
            stressThreshold = stresses[(int)Math.Floor(stresses.Count*PERCENTILE)];
        }

        public TensorField(string path)
        {
            Nodes = new Point3dList();
            Tensors = new List<Tensor>();
            MaxStress = Double.MinValue;
            MinStress = Double.MaxValue;
            List<double> allStressMagnitude = new List<double>();
            RTreeNodes = new RTree();
            double SMALL_LENGTH = RhinoMath.SqrtEpsilon;

            //List<Point3d> tensorLocations = new List<Point3d>();
            TextFieldParser parser = new TextFieldParser(path);
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            parser.ReadLine(); //Skip the header line
            while (!parser.EndOfData)
            {
                string[] fields = parser.ReadFields();
                Tensor tensor = new Tensor();
                tensor.plane.Rotate(Double.Parse(fields[7], NumberStyles.Float) / 180 * Math.PI, tensor.plane.XAxis); // was 7 XAxis
                tensor.plane.Rotate(Double.Parse(fields[8], NumberStyles.Float) / 180 * Math.PI, tensor.plane.YAxis); // was 8 YAxis
                tensor.plane.Rotate(Double.Parse(fields[9], NumberStyles.Float) / 180 * Math.PI, tensor.plane.ZAxis); // was 9 ZAxis
                tensor.plane.Rotate(0.5 * Math.PI, tensor.plane.ZAxis);
                tensor.plane.Rotate(0.5 * Math.PI, tensor.plane.XAxis);
                tensor.plane.OriginX = Double.Parse(fields[3], NumberStyles.Float); // was 3
                tensor.plane.OriginY = Double.Parse(fields[1], NumberStyles.Float); // was 1
                tensor.plane.OriginZ = Double.Parse(fields[2], NumberStyles.Float); // was 2
                //tensorLocations.Add(tensor.plane.Origin);
                tensor.Magnitude_X = Double.Parse(fields[4], NumberStyles.Float); //was 6
                if (tensor.Magnitude_X == 0) tensor.Magnitude_X = SMALL_LENGTH;
                MaxStress = Math.Max(MaxStress, Math.Abs(tensor.Magnitude_X)); 
                MinStress = Math.Min(MinStress, Math.Abs(tensor.Magnitude_X));
                allStressMagnitude.Add(Math.Abs(tensor.Magnitude_X));
                tensor.Magnitude_Y = Double.Parse(fields[5], NumberStyles.Float); //was 4
                if (tensor.Magnitude_X == 0) tensor.Magnitude_Y = SMALL_LENGTH;
                MaxStress = Math.Max(MaxStress, Math.Abs(tensor.Magnitude_Y));
                MinStress = Math.Min(MinStress, Math.Abs(tensor.Magnitude_Y));
                allStressMagnitude.Add(Math.Abs(tensor.Magnitude_Y));
                tensor.Magnitude_Z = Double.Parse(fields[6], NumberStyles.Float); //was 5
                if (tensor.Magnitude_X == 0) tensor.Magnitude_Z = SMALL_LENGTH;
                MaxStress = Math.Max(MaxStress, Math.Abs(tensor.Magnitude_Z));
                MinStress = Math.Min(MinStress, Math.Abs(tensor.Magnitude_Z));
                allStressMagnitude.Add(Math.Abs(tensor.Magnitude_Z));
                Tensors.Add(tensor);
            }
            Tensors.OrderBy(i => Math.Abs(i.Magnitude_X) + Math.Abs(i.Magnitude_Y) + Math.Abs(i.Magnitude_Z));
            //Percentile percentile = new Percentile(allStressMagnitude);
            allStressMagnitude.Sort();
            stressThreshold = allStressMagnitude[(int)Math.Floor(allStressMagnitude.Count * PERCENTILE)];
            
            for (int i = 0; i < Tensors.Count; i++)
            {
                Nodes.Add(Tensors[i].plane.Origin);
                RTreeNodes.Insert(Tensors[i].plane.Origin, i);
            }
        }

        public TensorField(TensorField tensorField)
        {
            
            Tensors = new List<Tensor>();
            for (int i = 0; i < tensorField.Tensors.Count; i++)
            {
                Tensors.Add(new Tensor(tensorField.Tensors[i]));
            }
            
            Nodes = new Point3dList(tensorField.Nodes);
            MaxStress = tensorField.MaxStress;
            MinStress = tensorField.MinStress;
            RTreeNodes = RTree.CreateFromPointArray(tensorField.Nodes);
            stressThreshold = tensorField.stressThreshold;
        }

        private int GetClosestIndex(Point3d testPoint)
        {
            return Nodes.ClosestIndex(testPoint);
        }

        public Tensor Interpolate(Point3d point)
        {
            

            int searchRaius = 10;

            Sphere searchSphere = new Sphere(point, searchRaius);
            List<int> indices = new List<int>();
            RTreeNodes.Search(searchSphere, (sender, args) => { indices.Add(args.Id); });

            if (indices.Count == 0)
            {
                int index = Nodes.ClosestIndex(point);
                Tensor tensor = Tensors[index];
                return new Tensor(tensor);
            }
            else
            {
                Vector3d maxV = new Vector3d();
                Vector3d midV = new Vector3d();
                Vector3d minV = new Vector3d();
                double maxS = 0;
                double midS = 0;
                double minS = 0;

                double totalWeigh = 0;
                double[] weighArray = new double[indices.Count];

                for (int i = 0; i < indices.Count; i++)
                {
                    int index = indices[i];
                    double curentWeigh = 1 / Math.Pow(Tensors[index].plane.Origin.DistanceTo(point), 3);
                    totalWeigh += curentWeigh;
                    weighArray[i] = curentWeigh;
                }
                Tensor firstTensor = Tensors[indices[0]];

                for (int i = 0; i < indices.Count; i++)
                {
                    int index = indices[i];
                    Tensor tensor = Tensors[index];
                    Vector3d curMaxV = tensor.plane.XAxis;
                    Vector3d curMidV = tensor.plane.YAxis;
                    Vector3d curMinV = tensor.plane.ZAxis;
                    double curMaxS = tensor.Magnitude_X;
                    double curMidS = tensor.Magnitude_Y;
                    double curMinS = tensor.Magnitude_Z;
                    if (Vector3d.Multiply(curMaxV, firstTensor.plane.XAxis) < 0) curMaxV.Reverse();
                    if (Vector3d.Multiply(curMidV, firstTensor.plane.YAxis) < 0) curMidV.Reverse();
                    if (Vector3d.Multiply(curMinV, firstTensor.plane.ZAxis) < 0) curMinV.Reverse();
                    maxV += curMaxV * Math.Abs(tensor.Magnitude_X) * weighArray[i] / totalWeigh;
                    midV += curMidV * Math.Abs(tensor.Magnitude_Y) * weighArray[i] / totalWeigh;
                    minV += curMinV * Math.Abs(tensor.Magnitude_Z) * weighArray[i] / totalWeigh;
                    maxS += curMaxS * weighArray[i] / totalWeigh;
                    midS += curMidS * weighArray[i] / totalWeigh;
                    minS += curMinS * weighArray[i] / totalWeigh;
                }
                Plane helpPlane = new Plane(point, maxV);
                midV.Transform(Transform.PlanarProjection(helpPlane));
                return new Tensor(new Plane(point, maxV, midV), maxS, midS, minS);
            }
        }

        public void GetOrientation(Point3d testPoint, ref Vector3d orientation, ref double majorFactor, ref double minorFactor)
        {
            //TODO:
            int closestIndex = Nodes.ClosestIndex(testPoint);
            Tensor tensor = Tensors[closestIndex];
            double[] stresses = new double[] { Math.Abs(tensor.Magnitude_X), Math.Abs(tensor.Magnitude_Y), Math.Abs(tensor.Magnitude_Z) };
            int[] indices = new int[] { 0, 1, 2 };
            Array.Sort(stresses, indices);
            if (MaxStress == MinStress)
            {
                majorFactor = 1;
                minorFactor = 1;
            }
            else
            {
                majorFactor = Math.Min((stresses[2] - MinStress) / (stressThreshold - MinStress),1);
                minorFactor = Math.Min((stresses[1] - MinStress) / (stressThreshold - MinStress),1);
            }
            int orientationIndex = indices[2];
            switch (orientationIndex) //Check if the two corrdinate system is the same.
            {
                case 0:
                    orientation = tensor.plane.XAxis;
                    break;
                case 1:
                    orientation = tensor.plane.YAxis;
                    break;
                case 2:
                    orientation = tensor.plane.ZAxis;
                    break;
                default:
                    throw new Exception("Cannot find orientation.");
            }
        }
    }


    public class TensorFieldGoo : GH_Goo<TensorField>
    {
        public TensorFieldGoo()
        {
            Value = new TensorField();
        }

        public TensorFieldGoo(String path)
        {
            Value = new TensorField(path);
        }

        public TensorFieldGoo(TensorField tensorField)
        {
            Value = new TensorField(tensorField);
        }

        public override bool IsValid
        {
            get { return true; }
        }

        public override IGH_Goo Duplicate()
        {
            return DuplicateTensorField();
        }

        public TensorFieldGoo DuplicateTensorField()
        {
            return new TensorFieldGoo(Value);
        }

        public override string TypeName => "TensorField";

        public override string ToString()
        {
            return "PolyBrick Tensor Field";
        }

        public override string TypeDescription => "Defines a tensor field that each unit contains triplets of vectors.";
    }

    public class TensorFieldParameter : GH_PersistentParam<TensorFieldGoo>
    {
        public TensorFieldParameter()
            : base(new GH_InstanceDescription("Tensor Field", "TF", "Contains triples of vectors.", "PolyBrick", "Parameters"))
        {
        }

        protected override Bitmap Icon
        {
            get { return Resource1.PolyBrickIcons_41; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("045bf2e4-4f1b-40b8-80f5-729b539f7b48"); }
        }

        protected override GH_GetterResult Prompt_Plural(ref List<TensorFieldGoo> values)
        {
            return GH_GetterResult.cancel;
        }

        protected override GH_GetterResult Prompt_Singular(ref TensorFieldGoo value)
        {
            return GH_GetterResult.cancel;
        }
    }

    public class TensorFieldComponent : GH_Component
    {
        public TensorFieldComponent()
            : base("Tensor Field From CSV", "TFFromCSV", "Create a tensor field from csv file.", "PolyBrick", "Tensor Field") { }

        protected override Bitmap Icon
        {
            get { return Resource1.PolyBrickIcons_40; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("21ec0fb1-f776-4db8-bfc0-d0b45c3696a7"); }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "Path of the CSV file", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.RegisterParam(new TensorFieldParameter(), "Tensor Field", "TF", "Tensor Field created from CSV.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string path = "";
            if (!DA.GetData(0, ref path)) { return; }
            TensorFieldGoo tensorField = new TensorFieldGoo(path);
            DA.SetData(0, tensorField);
        }
    }
}
