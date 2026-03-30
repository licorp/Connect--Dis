using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Quoc_MEP.Lib;

namespace Quoc_MEP
{
    /// <summary>
    /// Di chuyển, căn chỉnh và kết nối MEP elements — hỗ trợ 3D alignment.
    /// ✨ Multi-Connect Loop: connect xong → tự pick element tiếp, ESC để thoát.
    /// ---
    /// Move, Align and Connect MEP elements — with 3D alignment.
    /// ✨ Multi-Connect Loop: after connecting, auto-pick next element. ESC to exit.
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
                LogHelper.Log("[MOVE_ALIGN_CONNECT] Starting Move, Align & Connect (Loop mode)");

                // ===== MULTI-CONNECT LOOP =====
                // Mỗi vòng: pick dest → pick source → connect → lặp lại
                // CHỈ thoát khi ESC | ONLY exit on ESC
                int totalConnected = 0;
                int totalFailed = 0;

                // Lần đầu: pick dest | First: pick destination
                string destPrompt = "Ch\u1ecdn element \u0111\u00edch (ESC \u0111\u1ec3 d\u1eebng) | Pick destination (ESC to stop)";
                Reference destRef = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    new SelectionHelper.MEPFamilySelectionFilter(),
                    destPrompt);

                Element destElement = doc.GetElement(destRef);
                LogHelper.Log($"[MOVE_ALIGN_CONNECT] Destination: {destElement.Category?.Name} (ID: {destElement.Id})");

                while (true)
                {
                    try
                    {
                        // Pick source
                        string srcPrompt = totalConnected == 0
                            ? "Ch\u1ecdn element ngu\u1ed3n (ESC \u0111\u1ec3 d\u1eebng) | Pick source (ESC to stop)"
                            : $"\u2713 \u0110\u00e3 n\u1ed1i {totalConnected} | Pick source ho\u1eb7c ESC";

                        Reference srcRef = uidoc.Selection.PickObject(
                            ObjectType.Element,
                            new SelectionHelper.MEPFamilySelectionFilter(),
                            srcPrompt);

                        Element srcElement = doc.GetElement(srcRef);

                        // Validate
                        if (srcElement.Id == destElement.Id)
                        {
                            LogHelper.Log("[MOVE_ALIGN_CONNECT] Skipped: same element");
                            continue;
                        }

                        // Execute
                        using (Transaction trans = new Transaction(doc, "Move, Align & Connect"))
                        {
                            trans.Start();

                            ConnectionHelper.UnpinElementIfPinned(doc, srcElement);
                            ConnectionHelper.UnpinElementIfPinned(doc, destElement);

                            bool success = ConnectionHelper.MoveConnectAndAlign(doc, srcElement, destElement);

                            if (success)
                            {
                                trans.Commit();
                                totalConnected++;
                                LogHelper.Log($"[MOVE_ALIGN_CONNECT] \u2713 Connected #{totalConnected}: {srcElement.Id}");
                            }
                            else
                            {
                                trans.RollBack();
                                totalFailed++;
                                LogHelper.Log($"[MOVE_ALIGN_CONNECT] \u2717 Failed: {srcElement.Id}");
                            }
                        }

                        // Sau mỗi lần → pick dest mới cho lần tiếp theo
                        // After each → pick new dest for next iteration
                        string nextDestPrompt = $"\u2713 {totalConnected} \u0111\u00e3 n\u1ed1i | Ch\u1ecdn \u0111\u00edch m\u1edbi ho\u1eb7c ESC";
                        Reference nextDestRef = uidoc.Selection.PickObject(
                            ObjectType.Element,
                            new SelectionHelper.MEPFamilySelectionFilter(),
                            nextDestPrompt);

                        destElement = doc.GetElement(nextDestRef);
                        LogHelper.Log($"[MOVE_ALIGN_CONNECT] New dest: {destElement.Category?.Name} (ID: {destElement.Id})");
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        LogHelper.Log("[MOVE_ALIGN_CONNECT] Ended by user (ESC)");
                        break;
                    }
                }

                // Hiện kết quả tổng | Show summary
                LogHelper.Log($"[MOVE_ALIGN_CONNECT] Summary: {totalConnected} connected, {totalFailed} failed");

                if (totalConnected > 0 || totalFailed > 0)
                {
                    TaskDialog.Show("K\u1ebft qu\u1ea3 | Result",
                        $"\u2713 Th\u00e0nh c\u00f4ng: {totalConnected} k\u1ebft n\u1ed1i\n" +
                        (totalFailed > 0 ? $"\u2717 Th\u1ea5t b\u1ea1i: {totalFailed}\n" : ""));
                }

                return totalConnected > 0 ? Result.Succeeded : Result.Cancelled;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[MOVE_ALIGN_CONNECT] Exception: {ex.Message}");
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
