using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI.Selection;

namespace Quoc_MEP
{
    /// <summary>
    /// Helper class cho vi\u1ec7c ch\u1ecdn v\u00e0 ki\u1ec3m tra MEP elements.
    /// Helper class for MEP element selection and validation.
    /// </summary>
    public static class SelectionHelper
    {
        // Danh sách category MEP hợp lệ (dùng chung) | Shared valid MEP categories
        private static readonly BuiltInCategory[] _mepCategories =
        {
            BuiltInCategory.OST_MechanicalEquipment,
            BuiltInCategory.OST_DuctFitting,
            BuiltInCategory.OST_DuctAccessory,
            BuiltInCategory.OST_DuctTerminal,
            BuiltInCategory.OST_PipeFitting,
            BuiltInCategory.OST_PipeAccessory,
            BuiltInCategory.OST_PlumbingFixtures,
            BuiltInCategory.OST_ElectricalEquipment,
            BuiltInCategory.OST_ElectricalFixtures,
            BuiltInCategory.OST_LightingFixtures,
            BuiltInCategory.OST_CableTrayFitting,
            BuiltInCategory.OST_ConduitFitting
        };
        /// <summary>
        /// Selection filter cho MEP family instances v\u00e0 MEP curves.
        /// Selection filter for MEP family instances and MEP curves.
        /// </summary>
        public class MEPFamilySelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                // MEP curves (ducts, pipes, cable trays, conduits)
                if (elem is MEPCurve)
                    return true;

                // MEP family instances
                if (elem is FamilyInstance familyInstance)
                {
                    if (familyInstance.MEPModel != null)
                        return true;

                    // Fallback: kiểm tra category | Fallback: check category
                    return IsMEPCategory(elem);
                }

                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }

        /// <summary>
        /// Ki\u1ec3m tra element c\u00f3 ph\u1ea3i MEP element kh\u00f4ng.
        /// Check if element is a valid MEP element.
        /// </summary>
        public static bool IsMEPElement(Element element)
        {
            if (element == null) return false;
            if (element is MEPCurve) return true;

            if (element is FamilyInstance familyInstance)
            {
                if (familyInstance.MEPModel != null) return true;
                return IsMEPCategory(element);
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra category có thuộc MEP không.
        /// Check if element category is MEP.
        /// </summary>
        private static bool IsMEPCategory(Element element)
        {
            var category = element?.Category;
            if (category == null) return false;
            var categoryId = category.Id.IntegerValue;
            return _mepCategories.Any(c => (int)c == categoryId);
        }

        /// <summary>
        /// L\u1ea5y t\u00ean hi\u1ec3n th\u1ecb c\u1ee7a MEP element.
        /// Get display name of a MEP element.
        /// </summary>
        public static string GetMEPElementDisplayName(Element element)
        {
            if (element == null) return "Unknown";

            try
            {
                if (element is FamilyInstance familyInstance)
                    return $"{familyInstance.Symbol.FamilyName} - {familyInstance.Symbol.Name}";

                if (element is MEPCurve mepCurve)
                    return $"{mepCurve.MEPSystem?.Name ?? "System"} - {mepCurve.Name}";

                return element.Name ?? $"Element {element.Id}";
            }
            catch
            {
                return $"Element {element.Id}";
            }
        }
    }
}
