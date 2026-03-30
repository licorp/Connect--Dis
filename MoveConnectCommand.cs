using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Quoc_MEP.Lib;

namespace Quoc_MEP
{
    /// <summary>
    /// Di chuyển và kết nối MEP elements (không xoay).
    /// ✨ Multi-Connect Loop: connect xong → tự pick element tiếp, ESC để thoát.
    /// ---
    /// Move and connect MEP elements (without rotation).
    /// ✨ Multi-Connect Loop: after connecting, auto-pick next element. ESC to exit.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class MoveConnectCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                LogHelper.Log("[MOVE_CONNECT] ═══════════════════════════════════════");
                LogHelper.Log("[MOVE_CONNECT] Starting Move & Connect (Loop mode)");

                // Bước 1: Chọn element đích (chỉ pick 1 lần)
                // Step 1: Pick destination (only once)
                Reference destRef = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    new SelectionHelper.MEPFamilySelectionFilter(),
                    "Ch\u1ecdn MEP family \u0111\u00edch | Pick destination MEP family");

                if (destRef == null) return Result.Cancelled;
                Element destElement = doc.GetElement(destRef);
                LogHelper.Log($"[MOVE_CONNECT] Destination: {destElement.Category?.Name} (ID: {destElement.Id})");

                // ===== MULTI-CONNECT LOOP =====
                int totalConnected = 0;
                int totalFailed = 0;

                while (true)
                {
                    try
                    {
                        string prompt = totalConnected == 0
                            ? "Ch\u1ecdn element c\u1ea7n di chuy\u1ec3n (ESC \u0111\u1ec3 d\u1eebng) | Pick source (ESC to stop)"
                            : $"\u2713 \u0110\u00e3 n\u1ed1i {totalConnected} | Ch\u1ecdn ti\u1ebfp ho\u1eb7c ESC";

                        Reference sourceRef = uidoc.Selection.PickObject(
                            ObjectType.Element,
                            new SelectionHelper.MEPFamilySelectionFilter(),
                            prompt);

                        if (sourceRef == null) break;
                        Element sourceElement = doc.GetElement(sourceRef);

                        // Validate
                        if (destElement.Id == sourceElement.Id) continue;

                        // Execute
                        using (Transaction trans = new Transaction(doc, "Move Connect MEP"))
                        {
                            trans.Start();

                            ConnectionHelper.UnpinElementIfPinned(doc, sourceElement);
                            ConnectionHelper.UnpinElementIfPinned(doc, destElement);

                            bool success = ConnectionHelper.MoveAndConnect(doc, sourceElement, destElement);

                            if (success)
                            {
                                trans.Commit();
                                totalConnected++;
                                LogHelper.Log($"[MOVE_CONNECT] ✓ Connected #{totalConnected}: {sourceElement.Id}");
                            }
                            else
                            {
                                trans.RollBack();
                                totalFailed++;
                                LogHelper.Log($"[MOVE_CONNECT] ✗ Failed: {sourceElement.Id}");
                            }
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        LogHelper.Log("[MOVE_CONNECT] Loop ended by user (ESC)");
                        break;
                    }
                }

                // Hiện kết quả | Show summary
                LogHelper.Log($"[MOVE_CONNECT] Summary: {totalConnected} connected, {totalFailed} failed");

                if (totalConnected > 0 || totalFailed > 0)
                {
                    TaskDialog.Show("K\u1ebft qu\u1ea3 | Result",
                        $"\u2713 Th\u00e0nh c\u00f4ng: {totalConnected} k\u1ebft n\u1ed1i\n" +
                        (totalFailed > 0 ? $"\u2717 Th\u1ea5t b\u1ea1i: {totalFailed}\n" : "") +
                        $"\nDestination: {destElement.Category?.Name} (ID: {destElement.Id})");
                }

                return totalConnected > 0 ? Result.Succeeded : Result.Cancelled;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = $"L\u1ed7i: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}
