using System;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Quoc_MEP.Lib;

namespace Quoc_MEP
{
    /// <summary>
    /// Ngắt kết nối MEP elements — hỗ trợ 1 hoặc nhiều elements.
    /// ✨ Batch mode: chọn nhiều elements → disconnect tất cả cùng lúc.
    /// ---
    /// Disconnect MEP elements — supports single or batch mode.
    /// ✨ Batch mode: select multiple elements → disconnect all at once.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class DisconnectCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Hiện dialog chọn chế độ | Show mode selection
                var dlg = new TaskDialog("Ng\u1eaft k\u1ebft n\u1ed1i | Disconnect");
                dlg.MainInstruction = "Ch\u1ecdn ch\u1ebf \u0111\u1ed9 | Select mode:";
                dlg.AddCommandLink(TaskDialogCommandLinkId.CommandLink1,
                    "\u2460 Ng\u1eaft 1 element | Disconnect Single",
                    "Ch\u1ecdn 1 element \u0111\u1ec3 ng\u1eaft t\u1ea5t c\u1ea3 k\u1ebft n\u1ed1i c\u1ee7a n\u00f3");
                dlg.AddCommandLink(TaskDialogCommandLinkId.CommandLink2,
                    "\u2461 Ng\u1eaft nhi\u1ec1u elements | Batch Disconnect",
                    "Ch\u1ecdn nhi\u1ec1u elements r\u1ed3i ng\u1eaft t\u1ea5t c\u1ea3 c\u00f9ng l\u00fac");
                dlg.CommonButtons = TaskDialogCommonButtons.Cancel;

                var result = dlg.Show();

                if (result == TaskDialogResult.CommandLink1)
                {
                    return DisconnectSingle(uidoc, doc);
                }
                else if (result == TaskDialogResult.CommandLink2)
                {
                    return DisconnectBatch(uidoc, doc);
                }
                else
                {
                    return Result.Cancelled;
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        /// <summary>
        /// Ngắt 1 element — chọn rồi disconnect tất cả connectors.
        /// Disconnect single element — pick one and disconnect all its connectors.
        /// </summary>
        private Result DisconnectSingle(UIDocument uidoc, Document doc)
        {
            Reference pickedRef = uidoc.Selection.PickObject(
                ObjectType.Element,
                new SelectionHelper.MEPFamilySelectionFilter(),
                "Ch\u1ecdn MEP element c\u1ea7n ng\u1eaft | Pick MEP element to disconnect");

            Element element = doc.GetElement(pickedRef);

            if (!SelectionHelper.IsMEPElement(element))
            {
                TaskDialog.Show("L\u1ed7i | Error", "Kh\u00f4ng ph\u1ea3i MEP element h\u1ee3p l\u1ec7.");
                return Result.Failed;
            }

            using (Transaction trans = new Transaction(doc, "Disconnect MEP Element"))
            {
                trans.Start();
                int count = ConnectionHelper.DisconnectElement(element);
                trans.Commit();

                if (count > 0)
                    TaskDialog.Show("Th\u00e0nh c\u00f4ng | Success",
                        $"\u0110\u00e3 ng\u1eaft {count} k\u1ebft n\u1ed1i.\nDisconnected {count} connector(s).");
                else
                    TaskDialog.Show("Th\u00f4ng tin | Info",
                        "Kh\u00f4ng c\u00f3 k\u1ebft n\u1ed1i n\u00e0o.\nNo connections found.");
            }

            return Result.Succeeded;
        }

        /// <summary>
        /// Ngắt nhiều elements — chọn hàng loạt rồi disconnect tất cả.
        /// Batch disconnect — select multiple elements then disconnect all.
        /// </summary>
        private Result DisconnectBatch(UIDocument uidoc, Document doc)
        {
            // Cho chọn nhiều elements | Allow multi-select
            IList<Reference> refs = uidoc.Selection.PickObjects(
                ObjectType.Element,
                new SelectionHelper.MEPFamilySelectionFilter(),
                "Ch\u1ecdn nhi\u1ec1u MEP elements (Enter \u0111\u1ec3 x\u00e1c nh\u1eadn) | Pick multiple MEP elements (Enter to confirm)");

            if (refs == null || refs.Count == 0)
            {
                TaskDialog.Show("Th\u00f4ng tin", "Kh\u00f4ng ch\u1ecdn element n\u00e0o.");
                return Result.Cancelled;
            }

            int totalDisconnected = 0;
            int totalElements = 0;

            using (Transaction trans = new Transaction(doc, "Batch Disconnect MEP"))
            {
                trans.Start();

                foreach (Reference r in refs)
                {
                    Element element = doc.GetElement(r);
                    if (element == null || !SelectionHelper.IsMEPElement(element))
                        continue;

                    int count = ConnectionHelper.DisconnectElement(element);
                    if (count > 0)
                    {
                        totalDisconnected += count;
                        totalElements++;
                    }
                }

                trans.Commit();
            }

            TaskDialog.Show("K\u1ebft qu\u1ea3 | Result",
                $"\u0110\u00e3 x\u1eed l\u00fd {refs.Count} elements:\n" +
                $"\u2022 {totalElements} elements c\u00f3 k\u1ebft n\u1ed1i\n" +
                $"\u2022 {totalDisconnected} k\u1ebft n\u1ed1i \u0111\u00e3 ng\u1eaft\n" +
                $"\u2022 {refs.Count - totalElements} elements kh\u00f4ng c\u00f3 k\u1ebft n\u1ed1i");

            return Result.Succeeded;
        }
    }
}
