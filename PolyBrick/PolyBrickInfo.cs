using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace PolyBrick
{
    public class PolyBrickInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "PolyBrick";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("96cf8bbf-e04b-4451-a72a-121f28f850b8");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Yao Lu <Jenny Sabin Lab>";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "yl3229@cornell.edu";
            }
        }
    }
}
