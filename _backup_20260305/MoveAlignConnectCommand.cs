using System;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Quoc_MEP.Lib;

namespace Quoc_MEP
{
    /// <summary>
    /// Move, Align and Connect MEP elements - with 3D alignment and intelligent connector selection
    /// Uses MEPGeometryUtils for safe geometric calculations
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class MoveAlignConnectCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                LogHelper.Log("[MOVE_ALIGN_CONNECT] ═══════════════════════════════════════");
                LogHelper.Log("[MOVE_ALIGN_CONNECT] Starting Move, Align & Connect command");
                
                // Step 1: Pick destination element
                LogHelper.Log("[MOVE_ALIGN_CONNECT] Step 1: Select destination element...");
                Reference destRef = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    new MEPSelectionFilter(),
                    "Chọn element đích (sẽ giữ nguyên vị trí)");

                Element destElement = doc.GetElement(destRef);
                LogHelper.Log($"[MOVE_ALIGN_CONNECT] Destination: {destElement.Category?.Name} (ID: {destElement.Id})");

                // Step 2: Pick source element to move
                LogHelper.Log("[MOVE_ALIGN_CONNECT] Step 2: Select source element...");
                Reference srcRef = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    new MEPSelectionFilter(),
                    "Chọn element nguồn (sẽ được di chuyển và kết nối)");

                Element srcElement = doc.GetElement(srcRef);
                LogHelper.Log($"[MOVE_ALIGN_CONNECT] Source: {srcElement.Category?.Name} (ID: {srcElement.Id})");

                // Validate elements
                LogHelper.Log("[MOVE_ALIGN_CONNECT] Step 3: Validating elements...");
                
                if (srcElement.Id == destElement.Id)
                {
                    LogHelper.Log("[MOVE_ALIGN_CONNECT] ✗ Error: Same element selected");
                    TaskDialog.Show("Lỗi", "Không thể kết nối element với chính nó!");
                    return Result.Failed;
                }
                
                // Check if elements have connectors
                var srcConnectorMgr = GetConnectorManager(srcElement);
                var destConnectorMgr = GetConnectorManager(destElement);
                
                if (srcConnectorMgr == null || destConnectorMgr == null)
                {
                    LogHelper.Log("[MOVE_ALIGN_CONNECT] ✗ Error: Elements don't have connectors");
                    TaskDialog.Show("Lỗi", "Một hoặc cả hai element không có connector!");
                    return Result.Failed;
                }
                
                LogHelper.Log($"[MOVE_ALIGN_CONNECT] Source connectors: {srcConnectorMgr.Connectors.Size}");
                LogHelper.Log($"[MOVE_ALIGN_CONNECT] Destination connectors: {destConnectorMgr.Connectors.Size}");

                // Execute move, align and connect with alignment enforcement
                LogHelper.Log("[MOVE_ALIGN_CONNECT] Step 4: Executing move, align & connect...");
                
                using (Transaction trans = new Transaction(doc, "Move, Align & Connect"))
                {
                    trans.Start();
                    
                    try
                    {
                        bool success = ConnectionHelper.MoveConnectAndAlign(doc, srcElement, destElement);

                        if (success)
                        {
                            trans.Commit();
                            LogHelper.Log("[MOVE_ALIGN_CONNECT] ✓ Success: Elements connected");
                            LogHelper.Log("[MOVE_ALIGN_CONNECT] ═══════════════════════════════════════\n");
                            
                            TaskDialog.Show("Thành công", 
                                $"Đã di chuyển, căn chỉnh và kết nối thành công!\n\n" +
                                $"Source: {srcElement.Category?.Name} (ID: {srcElement.Id})\n" +
                                $"→ Destination: {destElement.Category?.Name} (ID: {destElement.Id})");
                            return Result.Succeeded;
                        }
                        else
                        {
                            trans.RollBack();
                            LogHelper.Log("[MOVE_ALIGN_CONNECT] ✗ Failed: Could not connect elements");
                            LogHelper.Log("[MOVE_ALIGN_CONNECT] ═══════════════════════════════════════\n");
                            
                            TaskDialog.Show("Thất bại", 
                                "Không thể di chuyển, căn chỉnh và kết nối các element.\n\n" +
                                "Kiểm tra xem:\n" +
                                "• Elements có tương thích không?\n" +
                                "• Có connector khả dụng không?\n" +
                                "• Kích thước connector có khớp không?");
                            return Result.Failed;
                        }
                    }
                    catch (Exception ex)
                    {
                        trans.RollBack();
                        LogHelper.Log($"[MOVE_ALIGN_CONNECT] ✗ Exception during operation: {ex.Message}");
                        throw;
                    }
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                LogHelper.Log("[MOVE_ALIGN_CONNECT] Operation cancelled by user");
                LogHelper.Log("[MOVE_ALIGN_CONNECT] ═══════════════════════════════════════\n");
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[MOVE_ALIGN_CONNECT] ✗✗✗ EXCEPTION: {ex.Message}");
                LogHelper.Log($"[MOVE_ALIGN_CONNECT] Stack trace: {ex.StackTrace}");
                LogHelper.Log("[MOVE_ALIGN_CONNECT] ═══════════════════════════════════════\n");
                
                message = ex.Message;
                TaskDialog.Show("Lỗi", $"Đã xảy ra lỗi:\n\n{ex.Message}");
                return Result.Failed;
            }
        }
        
        /// <summary>
        /// Get connector manager from element
        /// </summary>
        private static ConnectorManager GetConnectorManager(Element element)
        {
            if (element is FamilyInstance familyInstance)
            {
                return familyInstance.MEPModel?.ConnectorManager;
            }
            else if (element is MEPCurve mepCurve)
            {
                return mepCurve.ConnectorManager;
            }
            return null;
        }
    }
}
