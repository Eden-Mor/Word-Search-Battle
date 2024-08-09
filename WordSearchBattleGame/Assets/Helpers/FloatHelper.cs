using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Helpers
{
    public static class FloatHelper
    {
        public static bool HasFloatChanged(float previous, float current, float tolerance)
            => Mathf.Abs(current - previous) > tolerance;
    }
}
