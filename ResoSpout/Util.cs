using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResoSpout
{
    static class Util
    {
        public static string getNameFromTextureResolution(int width, int height)
        {
            return width.ToString() + "x" + height.ToString();
        }
    }
}
