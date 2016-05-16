using System;
using System.Collections.Generic;
using System.Text;

namespace Dpu.ImageProcessing
{
    /// <summary>
    /// Used to represent a detection in an image.
    /// </summary>
    class Detection
    {
        public string File;
        public string Name;
        public List<double> Coordinates;

    }
}
