using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace Quoc_MEP
{
    /// <summary>Filter chỉ cho phép chọn Pipe và Duct.</summary>
    public class MEPCurveSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem) => elem is Pipe || elem is Duct;
        public bool AllowReference(Reference reference, XYZ position) => false;
    }

    /// <summary>Filter cho phép chọn Pipe, Duct và các MEP FamilyInstance có connector.</summary>
    public class ConnectEndpointMEPFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is Pipe || elem is Duct || elem is FlexPipe || elem is FlexDuct)
                return true;
            if (elem is FamilyInstance fi)
            {
                if (fi.MEPModel != null) return true;
                var cat = fi.Category;
                if (cat != null)
                {
                    var bic = (BuiltInCategory)cat.Id.IntegerValue;
                    return bic == BuiltInCategory.OST_PipeFitting ||
                           bic == BuiltInCategory.OST_PipeAccessory ||
                           bic == BuiltInCategory.OST_PlumbingFixtures ||
                           bic == BuiltInCategory.OST_MechanicalEquipment ||
                           bic == BuiltInCategory.OST_DuctFitting ||
                           bic == BuiltInCategory.OST_DuctAccessory ||
                           bic == BuiltInCategory.OST_DuctTerminal;
                }
            }
            return false;
        }
        public bool AllowReference(Reference reference, XYZ position) => false;
    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class UpdatePipeEndpointCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            while (ConnectEndpoint(uiDoc, doc)) { }
            return Result.Succeeded;
        }

        /// <summary>
        /// Một vòng pick: chọn Pipe/Duct cần kéo endpoint, rồi chọn đối tượng đích.
        /// Trả về false khi user nhấn ESC ở lần pick đầu tiên.
        /// </summary>
        private bool ConnectEndpoint(UIDocument uiDoc, Document doc)
        {
            Element movedElement = null;
            XYZ movedPoint = null;
            Element targetElement = null;
            XYZ targetPoint = null;

            try
            {
                Reference refMoved = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    new MEPCurveSelectionFilter(),
                    "Chọn Pipe/Duct cần kéo endpoint (ESC để thoát)");
                movedElement = doc.GetElement(refMoved);
                movedPoint = refMoved.GlobalPoint;

                Reference refTarget = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    new ConnectEndpointMEPFilter(),
                    "Chọn đối tượng để kết nối tới");
                targetElement = doc.GetElement(refTarget);
                targetPoint = refTarget.GlobalPoint;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // ESC ở pick 1 → thoát vòng lặp
                // ESC ở pick 2 → tiếp tục vòng lặp (pick cặp mới)
                return movedElement != null;
            }

            ConnectorManager movedCM = GetConnectorManager(movedElement);
            ConnectorManager targetCM = GetConnectorManager(targetElement);
            if (movedCM == null || targetCM == null)
            {
                TaskDialog.Show("Lỗi", "Không tìm thấy connector trên element đã chọn.");
                return true;
            }

            Connector movedConnector     = GetNearest(movedCM.Connectors, movedPoint);
            Connector movedConnector_far = GetFarthest(movedCM.Connectors, movedPoint);
            Connector targetConnector    = GetNearest(targetCM.Connectors, targetPoint);

            if (movedConnector == null || targetConnector == null) return true;

            using (Transaction trans = new Transaction(doc, "Connect Endpoint"))
            {
                trans.Start();
                try
                {
                    LocationCurve loc = movedElement.Location as LocationCurve;
                    UpdateEndpoint(loc, movedConnector, movedConnector_far, targetConnector.Origin);
                    movedConnector.ConnectTo(targetConnector);
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    trans.RollBack();
                    TaskDialog.Show("Lỗi kết nối", ex.Message);
                }
            }

            return true;
        }

        /// <summary>
        /// Cập nhật LocationCurve của Pipe/Duct: đầu gần điểm click di chuyển đến newEndpoint,
        /// đầu còn lại giữ nguyên.
        /// </summary>
        private void UpdateEndpoint(LocationCurve loc, Connector movedConnector,
            Connector farConnector, XYZ newEndpoint)
        {
            if (loc == null)
                throw new InvalidOperationException("Element không có LocationCurve.");

            XYZ startPt = loc.Curve.GetEndPoint(0);
            bool movedIsStart = movedConnector.Origin.DistanceTo(startPt) < 0.1;

            XYZ sp = movedIsStart ? newEndpoint : farConnector.Origin;
            XYZ ep = movedIsStart ? farConnector.Origin : newEndpoint;
            loc.Curve = Line.CreateBound(sp, ep);
        }

        private ConnectorManager GetConnectorManager(Element element)
        {
            if (element is MEPCurve mepCurve) return mepCurve.ConnectorManager;
            if (element is FamilyInstance fi && fi.MEPModel != null) return fi.MEPModel.ConnectorManager;
            return null;
        }

        private Connector GetNearest(ConnectorSet connectors, XYZ point)
        {
            Connector nearest = null;
            double minDist = double.MaxValue;
            foreach (Connector c in connectors)
            {
                double d = c.Origin.DistanceTo(point);
                if (d < minDist) { minDist = d; nearest = c; }
            }
            return nearest;
        }

        private Connector GetFarthest(ConnectorSet connectors, XYZ point)
        {
            Connector farthest = null;
            double maxDist = double.MinValue;
            foreach (Connector c in connectors)
            {
                double d = c.Origin.DistanceTo(point);
                if (d > maxDist) { maxDist = d; farthest = c; }
            }
            return farthest;
        }
    }
}