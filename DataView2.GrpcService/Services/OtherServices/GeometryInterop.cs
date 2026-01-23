using System;
using System.Runtime.InteropServices;

namespace DataView2.GrpcService.Services.OtherServices
{
    public static class GeometryInterop
    {
        [DllImport("Geometry_calc.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Geometry_calc(
            ref emxArray_real_T chainage,
            ref emxArray_real_T pitch,
            ref emxArray_real_T roll,
            ref emxArray_real_T yaw,
            double proc_int,
            double max_curvature,
            ref emxArray_real_T tpl_slope,
            ref emxArray_real_T chainage_proc,
            ref emxArray_real_T grade_proc,
            ref emxArray_real_T cross_slope_proc,
            ref emxArray_real_T rad_proc);
    }
}
