using DataView2.Core.Models.Other;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataView2.Core.Helper
{
    public class PCIDefectJSON
    {
        public static string jsonString = @"{
      ""Concrete"": [
        {
          ""Name"": ""Blowup"",
          ""UnitOfMeasure"": ""Per Slab (2 slabs present on both sides of a joint)"",
          ""LowSeverityDefinition"": ""Runway and High-Speed Taxiway: <13mm (height) <br /> Apron and Other Taxiways: <25mm (height)"",
          ""MediumSeverityDefinition"": ""Runway and High-Speed Taxiway: 13 – 25mm (height) <br /> Apron and Other Taxiways: 25 – 50mm (height)"",
          ""HighSeverityDefinition"": ""Runway and High-Speed Taxiway: Inoperable <br /> Apron and Other Taxiways: >50mm (height)"",
          ""PotentialEffectOnPCIDeduct"": ""Very High"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""PCI Corner Break"",
          ""UnitOfMeasure"": ""Per Slab with CNR Break"",
          ""LowSeverityDefinition"": ""Crack with WIDTH < 3mm Or <br /> ANY FILLED CRACK"",
          ""MediumSeverityDefinition"": ""Crack with WIDTH 3-25mm Or  <br /> Cracks with Moderate Spalling Or <br /> Filled Cracks with Unsatisfactory Filler Or <br /> Area between the corner break and the joints is lightly cracked (starting to shatter)"",
          ""HighSeverityDefinition"": ""Crack with WIDTH > 25mm Or <br /> Cracks with Severely Spalled (risk of FOD) Or <br /> Area between the corner break and the joints is Severely Cracked (shattered and risk of FOD)"",
          ""PotentialEffectOnPCIDeduct"": ""High"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""Longitudinal, Transverse and Diagonal Cracks"",
          ""UnitOfMeasure"": ""Per Slab with Cracking"",
          ""LowSeverityDefinition"": ""Crack with WIDTH < 3mm Or <br /> ANY FILLED CRACK Or <br /> the slab is divided into three pieces by two or more cracks, one of which is at least Or <br /> the slab is divided into three pieces by two or more cracks, one of which is at least Low severity"",
          ""MediumSeverityDefinition"": ""Crack with WIDTH 3-25mm Or <br /> Cracks with Moderate Spalling Or <br /> Filled Cracks with Unsatisfactory Filler Or <br /> the slab is divided into three pieces by two or more cracks, one of which is at least medium severity"",
          ""HighSeverityDefinition"": ""Crack with WIDTH > 25mm Or <br /> Cracks with Severely Spalled (risk of FOD) Or <br /> the slab is divided into three pieces by two or more cracks, one of which is at least High severity"",
          ""PotentialEffectOnPCIDeduct"": ""High"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""Durability Cracking"",
          ""UnitOfMeasure"": ""Per Slab with Cracking"",
          ""LowSeverityDefinition"": ""hairline cracks occurring in a limited area of the slab, such as one or two corners or along one joint. Little or no disintegration has occurred. No FOD potential"",
          ""MediumSeverityDefinition"": ""cracking has developed over a considerable amount of slab area with little or no disintegration or FOD potential. Or <br /> Cracking has started disintegrating with some FOD potential"",
          ""HighSeverityDefinition"": ""has developed over a considerable amount of slab area with disintegration or FOD potential"",
          ""PotentialEffectOnPCIDeduct"": ""High"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""Joint Seal Damage"",
          ""UnitOfMeasure"": ""Per Sample Unit"",
          ""LowSeverityDefinition"": ""Joint seal damage is at low severity if a few of the joints have sealer which has debonded from, but is still in contact with, the joint edge. This condition exists if a knife blade can be inserted between sealer and joint face without resistance."",
          ""MediumSeverityDefinition"": ""Joint seal damage is at medium severity if a few of the joints have any of the following conditions:<br /> (1) joint sealer is in place, but water access is possible through visible openings no more than 1⁄8 in. (3 mm) wide. If a knife blade cannot be inserted easily between sealer and joint face, this condition does not exist; <br /> (2) pumping debris are evident at the joint; <br /> (3) joint sealer is oxidized and “lifeless” but pliable (like a rope), and generally fills the joint opening; <br /> (4) vegetation in the joint is obvious, but does not obscure the joint opening."",
          ""HighSeverityDefinition"": ""Joint sealer is in generally poor condition over the entire surveyed sample"",
          ""PotentialEffectOnPCIDeduct"": ""Low"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""Small Patch (<0.5m2)"",
          ""UnitOfMeasure"": ""Per Slab with Patch"",
          ""LowSeverityDefinition"": ""Patch is functioning well with little or no deterioration."",
          ""MediumSeverityDefinition"": ""Patch that has deterioration or moderate spalling, or both, can be seen around the edges. Patch material can be dislodged with considerable effort (minor FOD potential)."",
          ""HighSeverityDefinition"": ""Patch deterioration, either by spalling around the patch or cracking within the patch, to a state that warrants replacement."",
          ""PotentialEffectOnPCIDeduct"": ""Medium"",
          ""AutomaticOrManual"": ""Manual (from Images)""
        },
        {
          ""Name"": ""Large Patch & Utility Cut (>0.5m2)"",
          ""UnitOfMeasure"": ""Per Slab with Patch"",
          ""LowSeverityDefinition"": ""Patch is functioning well with very little or no deterioration."",
          ""MediumSeverityDefinition"": ""Patch deterioration or moderate spalling, or both, can be seen around the edges. Patch material can be dislodged with considerable effort, causing some FOD potential"",
          ""HighSeverityDefinition"": ""Patch has deteriorated to a state that causes considerable roughness or high FOD potential, or both. The extent of the deterioration warrants replacement of the patch"",
          ""PotentialEffectOnPCIDeduct"": ""High"",
          ""AutomaticOrManual"": ""Manual (from Images)""
        },
        {
          ""Name"": ""Popouts (Pickouts)"",
          ""UnitOfMeasure"": ""Per Slab with Pickouts above density"",
          ""LowSeverityDefinition"": """",
          ""MediumSeverityDefinition"": """",
          ""HighSeverityDefinition"": """",
          ""GeneralDefinition"":""The density of the distress must be measured. If there is any doubt about the average being greater than three popouts per square yard (per square meter), at least three random 1 yd2 (1 m2) areas should be checked. When the average is greater than this density, the slab is counted."",
          ""PotentialEffectOnPCIDeduct"": ""Low"",
          ""AutomaticOrManual"": ""Automatic""
        },
        {
          ""Name"": ""PCI Pumping"",
          ""UnitOfMeasure"": ""Per Slab"",
          ""LowSeverityDefinition"": """",
          ""MediumSeverityDefinition"": """",
          ""HighSeverityDefinition"": """",
          ""GeneralDefinition"":""Slabs are counted as follows: one pumping joint between two slabs is counted as two slabs. However, if the remaining joints around the slab are also pumping, one slab is added per additional pumping joint."",
          ""PotentialEffectOnPCIDeduct"": ""Medium"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""Scaling"",
          ""UnitOfMeasure"": ""Per Slab"",
          ""LowSeverityDefinition"": ""Minimal loss of surface paste that poses no FOD hazard, limited to less than 1 % of the slab area. No FOD potential"",
          ""MediumSeverityDefinition"": ""The loss of surface paste that poses some FOD potential including isolated fragments of loose mortar, exposure of the sides of coarse aggregate (less than ¼ of the width of coarse aggregate), or evidence of coarse aggregate coming loose from the surface. Surface paste loss is greater than 1 % of the slab area but less than 10 %."",
          ""HighSeverityDefinition"": ""High severity is associated with low durability concrete that will continue to pose a high FOD hazard; normally the layer of surface mortar is observable at the perimeter of the scaled area, and is likely to continue to delaminate or disintegrate due to environmental or other factors. Routine sweeping is not sufficient to avoid FOD issues, is an indication that high FOD hazard is present. Surface paste loss is greater than 10 % of the slab area."",
          ""PotentialEffectOnPCIDeduct"": ""High"",
          ""AutomaticOrManual"": ""Automatic???""
        },
        {
          ""Name"": ""Settlement or Faulting"",
          ""UnitOfMeasure"": ""Per Slab"",
          ""LowSeverityDefinition"": ""Runways/Taxiways < 6 mm (height) <br /> Aprons 3 - 13 mm (height)"",
          ""MediumSeverityDefinition"": ""Runways/Taxiways 6 - 13mm (height) <br /> Aprons 13 - 25mm (height)"",
          ""HighSeverityDefinition"": ""Runways/Taxiways > 13mm (height) <br /> Aprons > 25mm (height)"",
          ""PotentialEffectOnPCIDeduct"": ""High"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""Shattered Slab/Intersecting Cracks"",
          ""UnitOfMeasure"": ""Per Slab"",
          ""LowSeverityDefinition"": ""Slab is broken into four or five pieces predominantly defined by low-severity cracks"",
          ""MediumSeverityDefinition"": ""Slab is broken into four or five pieces with over 15 % of the cracks of medium severity (no high-severity cracks); slab is broken into six or more pieces with over 85 % of the cracks of low severity."",
          ""HighSeverityDefinition"": ""Shattered slab. Slab is broken into four or five pieces with high severity cracks. <br /> slab is broken into six or more pieces with over 15% med or high severity"",
          ""PotentialEffectOnPCIDeduct"": ""Very High"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""Shrinkage Cracking"",
          ""UnitOfMeasure"": ""Per Slab"",
          ""LowSeverityDefinition"": """",
          ""MediumSeverityDefinition"": """",
          ""HighSeverityDefinition"": """",
          ""GeneralDefinition"":""No degrees of severity are defined. It is sufficient to indicate that shrinkage cracking exists"",
          ""PotentialEffectOnPCIDeduct"": ""Low"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""Joint Spalling"",
          ""UnitOfMeasure"": ""Per Slab with a Spalled Joint"",
          ""LowSeverityDefinition"": ""Spall >0.6 m long: (1) spall is  broken into no more than three pieces defined by low- or medium-severity cracks; little or no FOD potential exists; or (2) joint is lightly frayed; little or no FOD potential. <br /> Lightly frayed means the upper edge of the joint  is broken away leaving a spall <25mm wide and <13mm deep. The material is missing and the  joint creates little or no FOD potential."",
          ""MediumSeverityDefinition"": ""Spall >0.6 m long: (1) spall is broken into more than three pieces defined by light or medium cracks; (2) spall is broken into no more than three pieces with one or more of the cracks being severe with some FOD potential existing; or (3) joint is moderately frayed with some FOD potential (see Fig. X2.64). Spall less than 2 ft long: spall is broken into pieces or fragmented with some of the pieces loose or absent, causing considerable FOD or tire damage potential. <br /> Moderately frayed means the upper edge of the joint is broken away leaving a spall >25mm Wide and >13mm Deep. The material is mostly missing with some FOD potential."",
          ""HighSeverityDefinition"": ""Spall >0.6 m long: (1) spall is broken into more than three pieces defined by one or more high-severity cracks with high FOD potential and high possibility of the pieces becoming dislodged, or (2) joint is severely frayed with high FOD potential."",
          ""PotentialEffectOnPCIDeduct"": ""High"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""Corner Spalling"",
          ""UnitOfMeasure"": ""Per Slab with a Spalled Corner"",
          ""LowSeverityDefinition"": ""One of the following conditions exists: <br /> (1) spall is broken into one or two pieces defined by low-severity cracks (little or no FOD potential); or <br /> (2) spall is defined by one medium-severity crack (little or no FOD potential)"",
          ""MediumSeverityDefinition"": ""One of the following conditions exists: <br /> (1) spall is broken into two or more pieces defined by medium severity crack(s), and a few small fragments may be absent or loose. <br /> (2) spall is defined by one severe, fragmented crack that may be accompanied by a few hairline cracks; or, <br /> (3) spall has deteriorated to the point where loose material is causing some FOD potential"",
          ""HighSeverityDefinition"": ""One of the following conditions exists: <br /> (1) spall is broken into two or more pieces defined by high-severity fragmented crack(s) with loose or absent fragments; <br /> (2) pieces of the spall have been displaced to the extent that a tire damage hazard exists; or <br /> (3) spall has deteriorated to the point where loose material is causing high FOD potential"",
          ""PotentialEffectOnPCIDeduct"": ""Medium"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""Alkali Silica Reaction (ASR)"",
          ""UnitOfMeasure"": ""Per Slab"",
          ""LowSeverityDefinition"": ""Minimal to no FOD potential from cracks,  joints or ASR-related popouts; cracks at the surface are tight (predominantly 1.0 mm or less). Little to no evidence of movement in pavement or surrounding structures or elements"",
          ""MediumSeverityDefinition"": ""Some FOD potential; but increased sweeping or other FOD removal methods may be required. May be evidence of slab movement or some damage (or both) to adjacent structures or elements. Medium ASR distress is differentiated from low by having one or more of the follow ing: increased FOD potential, crack density increases, some fragments along cracks or at crack intersections present, surface popouts of concrete may occur, pattern of wider cracks (predominantly 1.0 mm or wider) that may be subdivided by tighter cracks"",
          ""HighSeverityDefinition"": ""One or both of the following exist: <br /> (1) loose or missing concrete fragments and poses high FOD potential, or <br /> (2) slab surface integrity and function significantly degraded and pavement requires immediate repairs; may also require repairs to adjacent structures or elements"",
          ""PotentialEffectOnPCIDeduct"": ""High"",
          ""AutomaticOrManual"": ""Manual (from Images)""
        }
      ],
      ""Asphalt"": [
        {
          ""Name"": ""Alligator cracking (Fatigue)"",
          ""UnitOfMeasure"": ""Area (m2)"",
          ""LowSeverityDefinition"": ""Fine, longitudinal hairline cracks running parallel together. No or few interconnecting cracks."",
          ""MediumSeverityDefinition"": ""further development of light alligator cracks into a network of cracks."",
          ""HighSeverityDefinition"": ""Pattern cracking progressed to well defined pieces and spalled at the edge"",
          ""PotentialEffectOnPCIDeduct"": ""Very High"",
          ""AutomaticOrManual"": ""Automatic""
        },
        {
          ""Name"": ""Bleeding"",
          ""UnitOfMeasure"": ""Area (m2)"",
          ""LowSeverityDefinition"": """",
          ""MediumSeverityDefinition"": """",
          ""HighSeverityDefinition"": """",
          ""GeneralDefinition"":""No Severity Levels Defined (present or not present)"",
          ""PotentialEffectOnPCIDeduct"": ""Medium"",
          ""AutomaticOrManual"": ""Automatic""
        },
        {
          ""Name"": ""Block Cracking"",
          ""UnitOfMeasure"": ""Area (m2)"",
          ""LowSeverityDefinition"": ""Blocks are defined by cracks that are nonspalled (sides of the crack are vertical) or lightly spalled, causing no FOD potential. Nonfilled cracks have <6 mm mean width and filled cracks have filler in satisfactory condition"",
          ""MediumSeverityDefinition"": ""Blocks are defined by either: filled or nonfilled cracks that are moderately spalled (some FOD potential); nonfilled cracks that are not spalled or have only minor spalling (some FOD potential), but have a mean width greater than approximately 1⁄4 in. (6 mm); or filled cracks greater than 1⁄4 in. that are not spalled or have only minor spalling (some FOD potential), but have filler in unsatisfactory condition."",
          ""HighSeverityDefinition"": ""Blocks are well defined by cracks that are severely spalled, causing a definite FOD potential"",
          ""PotentialEffectOnPCIDeduct"": ""Medium"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""Corrugation"",
          ""UnitOfMeasure"": ""Area (m2)"",
          ""LowSeverityDefinition"": ""Corrugations are minor and do not significantly affect ride quality. <br /> RUNWAY = <6mm mean height <br /> TAXI/APRON = <13mm mean height."",
          ""MediumSeverityDefinition"": ""Corrugations are noticeable and significantly affect ride quality. <br /> RUNWAY = <6-13mm mean height <br /> TAXI/APRON = <13-25mm mean height."",
          ""HighSeverityDefinition"": ""Corrugations are easily noticed and severely affect ride quality. <br /> RUNWAY = +13mm mean height <br /> TAXI/APRON = +25mm mean height"",
          ""PotentialEffectOnPCIDeduct"": ""Very High"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""Depression"",
          ""UnitOfMeasure"": ""Area (m2)"",
          ""LowSeverityDefinition"": ""Depression can be observed or located by stained areas, only slightly affects pavement riding quality, and may cause hydroplaning potential on runways. <br /> RUNWAY = 3-13mm depth <br /> TAXI/APRON = 13-25mm depth."",
          ""MediumSeverityDefinition"": ""The depression can be observed, moderately affects pavement riding quality, and causes hydroplaning potential on runways. <br /> RUNWAY = 13-25mm depth <br /> TAXI/APRON = 25-51mm depth."",
          ""HighSeverityDefinition"": ""The depression can be readily observed, severely affects pavement riding quality, and causes definite hydroplaning potential. <br /> RUNWAY = +25mm depth <br /> TAXI/APRON = +51mm depth."",
          ""PotentialEffectOnPCIDeduct"": ""High"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""Jet-Blast Erosion"",
          ""UnitOfMeasure"": ""Area (m2)"",
          ""LowSeverityDefinition"": """",
          ""MediumSeverityDefinition"": """",
          ""HighSeverityDefinition"": """",
          ""GeneralDefinition"":""No Severity Levels Defined (present or not present)"",
          ""PotentialEffectOnPCIDeduct"": ""Medium"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""Joint Reflection Cracking"",
          ""UnitOfMeasure"": ""Length (m)"",
          ""LowSeverityDefinition"": ""Cracks have only light spalling (little or no FOD potential) or no spalling, and can be filled or nonfilled. If nonfilled, the cracks have a mean width of 6 mm or less; filled cracks are of any width, but their filler material is in satisfactory condition."",
          ""MediumSeverityDefinition"": ""One of the following conditions exists: cracks are moderately spalled (some FOD potential) and can be either filled or nonfilled of any width; filled cracks are not spalled or are lightly spalled, but filler is in unsatisfactory condition; nonfilled cracks are not spalled or are only lightly spalled, but the mean crack width is greater than 6 mm; or light random cracking exists near the crack or at the corners of intersecting cracks."",
          ""HighSeverityDefinition"": ""Cracks are severely spalled with pieces loose or missing causing definite FOD potential. Cracks can be either filled or nonfilled of any width."",
          ""PotentialEffectOnPCIDeduct"": ""Medium"",
          ""AutomaticOrManual"": ""Automatic""
        },
        {
          ""Name"": ""Longitudinal & Transverse Cracking"",
          ""UnitOfMeasure"": ""Length (m)"",
          ""LowSeverityDefinition"": ""Cracks have only light spalling (little or no FOD potential) or no spalling, and can be filled or nonfilled. If nonfilled, the cracks have a mean width of <6 mm; filled cracks of any width, but their filler material is in satisfactory condition."",
          ""MediumSeverityDefinition"": ""One of the following conditions exists: (1) cracks are moderately spalled (some FOD potential) and can be either filled or nonfilled of any width; (2) filled cracks are not spalled or are lightly spalled, but filler is in unsatisfactory condition; (3) nonfilled cracks are not spalled or are only lightly spalled, but the mean crack width +6 mm, or (4) light random cracking exists near the crack or at the corners of intersecting cracks."",
          ""HighSeverityDefinition"": ""Cracks are severely spalled and pieces are loose or missing causing definite FOD potential. Cracks can be either filled or nonfilled of any width"",
          ""PotentialEffectOnPCIDeduct"": ""Medium"",
          ""AutomaticOrManual"": ""Automatic""
        },
        {
          ""Name"": ""Oil Spillage"",
          ""UnitOfMeasure"": ""Area (m2)"",
          ""LowSeverityDefinition"": """",
          ""MediumSeverityDefinition"": """",
          ""HighSeverityDefinition"": """",
          ""GeneralDefinition"":""No Severity Levels Defined (present or not present)"",
          ""PotentialEffectOnPCIDeduct"": ""Low"",
          ""AutomaticOrManual"": ""Manual (from Images)""
        },
        {
          ""Name"": ""Patch & Utility Cut"",
          ""UnitOfMeasure"": ""Area (m2)"",
          ""LowSeverityDefinition"": ""Patch is in good condition and is performing satisfactorily (i.e. Ride Quality)"",
          ""MediumSeverityDefinition"": ""Patch is somewhat deteriorated and affects ride quality to some extent. Moderate amount of distress is present within the patch or has FOD potential, or both."",
          ""HighSeverityDefinition"": ""Patch is badly deteriorated and affects ride quality significantly or has high FOD potential. Patch soon needs replacement."",
          ""PotentialEffectOnPCIDeduct"": ""High"",
          ""AutomaticOrManual"": ""Manual (from Images)""
        },
        {
          ""Name"": ""Polished Aggregate"",
          ""UnitOfMeasure"": ""Area (m2)"",
          ""LowSeverityDefinition"": """",
          ""MediumSeverityDefinition"": """",
          ""HighSeverityDefinition"": """",
          ""GeneralDefinition"":""No Severity Levels Defined (present or not present)"",
          ""PotentialEffectOnPCIDeduct"": ""Medium"",
          ""AutomaticOrManual"": ""No Current Method""
        },
        {
          ""Name"": ""Ravelling"",
          ""UnitOfMeasure"": ""Area (m2)"",
          ""LowSeverityDefinition"": ""In a square meter representative area, the number of coarse aggregate particles missing is between 5 and 20, and/or (2) missing aggregate clusters are less than 2 percent of the examined square meter area. In low severity raveling, there is little or no FOD potential."",
          ""MediumSeverityDefinition"": ""In a square meter representative area, the number of coarse aggregate particles missing is between 21 and 40, and/or (2) missing aggregate clusters are between 2 and 10 percent of the examined square yard (square meter) area. In medium severity raveling, there is some FOD potential."",
          ""HighSeverityDefinition"": ""In a square meter representative area, the number of coarse aggregate particles missing is over 40, and/or (2) missing aggregate clusters are more than 10 percent of the examined square meter area. In high severity raveling, there is significant FOD potential"",
          ""PotentialEffectOnPCIDeduct"": ""Medium"",
          ""AutomaticOrManual"": ""Automatic""
        },
        {
          ""Name"": ""PCI Rutting"",
          ""UnitOfMeasure"": ""Area (m2)"",
          ""LowSeverityDefinition"": ""6-13mm"",
          ""MediumSeverityDefinition"": ""13-25mm"",
          ""HighSeverityDefinition"": "" +25mm"",
          ""PotentialEffectOnPCIDeduct"": ""High"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""PCI Shoving"",
          ""UnitOfMeasure"": ""Area (m2)"",
          ""LowSeverityDefinition"": ""A slight amount of shoving has occurred and no breakup of the asphalt pavement. <20mm height"",
          ""MediumSeverityDefinition"": ""A significant amount of shoving has occurred, causing moderate roughness and little or no breakup of the asphalt pavement. 20-40mm height"",
          ""HighSeverityDefinition"": ""A large amount of shoving has occurred, causing severe roughness or breakup of the asphalt pavement. +40mm height"",
          ""PotentialEffectOnPCIDeduct"": ""High"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""Slippage Cracking"",
          ""UnitOfMeasure"": ""Area (m2)"",
          ""LowSeverityDefinition"": """",
          ""MediumSeverityDefinition"": """",
          ""HighSeverityDefinition"": """",
          ""GeneralDefinition"":""No Severity Levels Defined (present or not present)"",
          ""PotentialEffectOnPCIDeduct"": ""High"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""Swell"",
          ""UnitOfMeasure"": ""Area (m2)"",
          ""LowSeverityDefinition"": ""Swell is barely visible and has a minor effect on the pavement’s ride quality. <br /> RUNWAY = <20mm height, <br /> TAXI/APRON = <40mm height"",
          ""MediumSeverityDefinition"": ""Swell can be observed without difficulty and has a significant effect on the pavement’s ride quality. <br /> RUNWAY = 20-40mm height, <br /> TAXI/APRON = 40-80mm height"",
          ""HighSeverityDefinition"": ""Swell can be readily observed and severely affects the pavement’s ride quality.<br /> RUNWAY = +40mm height, <br /> TAXI/APRON = +80mm height"",
          ""PotentialEffectOnPCIDeduct"": ""Very High"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        },
        {
          ""Name"": ""Weathering"",
          ""UnitOfMeasure"": ""Area (m2)"",
          ""LowSeverityDefinition"": ""Asphalt surface beginning to show signs of aging which may be accelerated by climatic conditions. Loss of the fine aggregate matrix is noticeable and may be accompanied by fading of the asphalt color. Edges of the coarse aggregates are beginning to be exposed (less than 1 mm or 0.05 in.). Pavement may be relatively new (as new as six months old)"",
          ""MediumSeverityDefinition"": ""Loss of fine aggregate matrix is noticeable and edges of coarse aggregate have been exposed up to ¼ width (of the longest side) of the coarse aggregate due to the loss of fine aggregate matrix"",
          ""HighSeverityDefinition"": ""Edges of coarse aggregate have been exposed greater than ¼ width (of the longest side) of the coarse aggregate. There is considerable loss of fine aggregate matrix leading to potential or some loss of coarse aggregate"",
          ""PotentialEffectOnPCIDeduct"": ""Low"",
          ""AutomaticOrManual"": ""Automatic detection BUT Manually added to PCI list""
        }
      ]
    }";
        public static List<PCIDefectDescription> ConvertJsonToPCIDefectDescription()
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonData = JsonSerializer.Deserialize<Dictionary<string, List<PCIDefectDescription>>>(jsonString, options);

            List<PCIDefectDescription> pciDefects = new List<PCIDefectDescription>();
            int id = 1; // Start the Id from 1

            foreach (var entry in jsonData)
            {
                string surfaceType = entry.Key; // "Concrete" or "Asphalt"

                foreach (var defect in entry.Value)
                {
                    defect.Id = id++;
                    defect.Surface = surfaceType;
                    defect.PCIRatingType = "Airport";

                    pciDefects.Add(defect);
                }
            }

            return pciDefects;
        }

        public static int GetDistressCode(string defectName)
        {
            var defectMapping = new Dictionary<string, int>
            {
                //Concrete
                { "Blowup" , 61 },
                { "PCI Corner Break", 62 },
                { "Longitudinal, Transverse and Diagonal Cracks", 63 },
                { "Durability Cracking", 64 },
                { "Joint Seal Damage", 65},
                { "Small Patch (<0.5m2)", 66 },
                { "Large Patch & Utility Cut (>0.5m2)", 67 },
                { "Popouts (Pickouts)", 68 },
                { "PCI Pumping", 69 },
                { "Scaling", 70 },
                { "Settlement or Faulting", 71 },
                { "Shattered Slab/Intersecting Cracks", 72 },
                { "Shrinkage Cracking", 73 },
                { "Joint Spalling", 74 },
                { "Corner Spalling", 75 },
                { "Alkali Silica Reaction (ASR)", 76 },

                //Asphalt
                { "Alligator cracking (Fatigue)", 41 },
                { "Bleeding", 42 },
                { "Block Cracking", 43 },
                { "Corrugation", 44 },
                { "Depression", 45 },
                { "Jet-Blast Erosion", 46 },
                { "Joint Reflection Cracking", 47 },
                { "Longitudinal & Transverse Cracking", 48 },
                { "Oil Spillage", 49 },
                { "Patch & Utility Cut", 50 },
                { "Polished Aggregate", 51 },
                { "Ravelling", 52 },
                { "PCI Rutting", 53 },
                { "PCI Shoving", 54 },
                { "Slippage Cracking", 55 },
                { "Swell", 56 },
                { "Weathering", 57}
            };

            return defectMapping.TryGetValue(defectName, out int code) ? code : -1;
        }
    }
}
