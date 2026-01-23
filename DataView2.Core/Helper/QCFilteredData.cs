using DataView2.Core.Models.LCMS_Data_Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataView2.Core.Helper
{
    public class QCFilteredData
    {
        public string TableName {get;set;}
        public string FilterName { get;set;}
        public string Query { get;set;}
        public List<LCMS_Concrete_Joints>? lCMS_Concrete_Joints { get;set;}
        public List<LCMS_Corner_Break>? lCMS_Corner_Breaks { get;set;}
        public List<LCMS_Cracking_Raw>? lCMS_Cracking_Raws { get;set;}
        public List<LCMS_Patch_Processed>? lCMS_Patch_Processed { get;set;}
        public List<LCMS_PickOuts_Raw>? lCMS_PickOuts_Raws { get;set;}
        public List<LCMS_Potholes_Processed>? lCMS_Potholes_Processeds { get;set;}
        public List<LCMS_Ravelling_Raw>? lCMS_Ravelling_Raws { get;set;}
        public List<LCMS_Spalling_Raw>? lCMS_Spalling_Raws { get;set;}
    }
}
