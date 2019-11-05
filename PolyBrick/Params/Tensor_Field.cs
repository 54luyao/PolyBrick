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

        public TensorField()
        {
            Tensors = new List<Tensor>();
            Nodes = new Point3dList();
            MaxStress = 0;
            MinStress = 0;
        }

        public TensorField(List<Plane> planes, List<double> xs, List<double> ys, List<double> zs)
        {
            Tensors = new List<Tensor>();
            MaxStress = Double.MinValue;
            MinStress = Double.MaxValue;
            Nodes = new Point3dList();
            for (int i = 0; i < planes.Count; i++)
            {
                Tensor tensor = new Tensor(planes[i], xs[i], ys[i], zs[i]);
                Tensors.Add(tensor);
                //Nodes.Add(tensor.plane.Origin);
            }
            Tensors.OrderBy(i => i.Magnitude_X + i.Magnitude_Y + i.Magnitude_Z);
            for (int i = 0; i < Tensors.Count; i++)
            {
                Nodes.Add(Tensors[i].plane.Origin);
            }
            List<double> stresses = new List<double>();
            stresses.AddRange(xs.Select(x=>Math.Abs(x)).ToList());
            stresses.AddRange(ys.Select(x => Math.Abs(x)).ToList());
            stresses.AddRange(zs.Select(x => Math.Abs(x)).ToList());
            stresses.Sort();
            MaxStress = stresses[stresses.Count - 1];
            MinStress = stresses[0];
        }

        public TensorField(string path)
        {
            Tensors = new List<Tensor>();
            MaxStress = Double.MinValue;
            MinStress = Double.MaxValue;
            //List<Point3d> tensorLocations = new List<Point3d>();
            TextFieldParser parser = new TextFieldParser(path);
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            parser.ReadLine(); //Skip the header line
            while (!parser.EndOfData)
            {
                string[] fields = parser.ReadFields();
                Tensor tensor = new Tensor();
                tensor.plane.Rotate(Double.Parse(fields[7], NumberStyles.Float) / 180 * Math.PI, tensor.plane.XAxis);
                tensor.plane.Rotate(Double.Parse(fields[8], NumberStyles.Float) / 180 * Math.PI, tensor.plane.YAxis);
                tensor.plane.Rotate(Double.Parse(fields[9], NumberStyles.Float) / 180 * Math.PI, tensor.plane.ZAxis);
                tensor.plane.OriginX = Double.Parse(fields[3], NumberStyles.Float);
                tensor.plane.OriginY = Double.Parse(fields[1], NumberStyles.Float);
                tensor.plane.OriginZ = Double.Parse(fields[2], NumberStyles.Float);
                //tensorLocations.Add(tensor.plane.Origin);
                tensor.Magnitude_X = Double.Parse(fields[6], NumberStyles.Float);
                MaxStress = Math.Max(MaxStress, Math.Abs(tensor.Magnitude_X));
                MinStress = Math.Min(MinStress, Math.Abs(tensor.Magnitude_X));
                tensor.Magnitude_Y = Double.Parse(fields[4], NumberStyles.Float);
                MaxStress = Math.Max(MaxStress, Math.Abs(tensor.Magnitude_Y));
                MinStress = Math.Min(MinStress, Math.Abs(tensor.Magnitude_Y));
                tensor.Magnitude_Z = Double.Parse(fields[5], NumberStyles.Float);
                MaxStress = Math.Max(MaxStress, Math.Abs(tensor.Magnitude_Z));
                MinStress = Math.Min(MinStress, Math.Abs(tensor.Magnitude_Z));
                Tensors.Add(tensor);
            }
            Tensors.OrderBy(i => i.Magnitude_X + i.Magnitude_Y + i.Magnitude_Z);
            for (int i = 0; i < Tensors.Count; i++)
            {
                Nodes.Add(Tensors[i].plane.Origin);
            }
        }

        public TensorField(TensorField tensorField)
        {
            Tensors = new List<Tensor>(tensorField.Tensors);
            Nodes = new Point3dList(tensorField.Nodes);
            MaxStress = tensorField.MaxStress;
            MinStress = tensorField.MinStress;
        }

        private int GetClosestIndex(Point3d testPoint)
        {
            return Nodes.ClosestIndex(testPoint);
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
                majorFactor = (stresses[2] - MinStress) / (MaxStress - MinStress);
                minorFactor = (stresses[1] - MinStress) / (MaxStress - MinStress);
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

        public override string TypeDescription => "Defines a tensor field that each unit contains triplet of vectors.";
    }

    public class TensorFieldParameter : GH_Param<TensorFieldGoo>
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

        //protected override GH_GetterResult Prompt_Plural(ref List<TensorFieldGoo> values)
        //{
        //    return GH_GetterResult.cancel;
        //}

        //protected override GH_GetterResult Prompt_Singular(ref TensorFieldGoo value)
        //{
        //    return GH_GetterResult.cancel;
        //}
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
