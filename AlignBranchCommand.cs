using System;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Quoc_MEP.Lib;
using Nice3point.Revit.Extensions;

namespace Quoc_MEP
{
    /// <summary>
    /// Căn chỉnh nhánh ống/duct về ống chính — cơ chế theo Microdesk Align Branch.
    /// ① Lấy đường tâm (centerline) của ống chính.
    /// ② Với mỗi branch: tìm connector đầu hở gần main nhất (hoặc midpoint/LocationPoint nếu không có connector hở).
    /// ③ Project điểm tham chiếu branch lên infinite line của main
    ///    → tính vector vuông góc 3D từ brancfpth đến đường tâm main (perpendicular offset).
    /// ④ MoveElement toàn bộ branch theo vector vuông góc đó (không lọc trục — chuẩn Microdesk).
    /// ---
    /// Align branch pipes/ducts to main run — Microdesk Align Branch mechanism.
    /// ① Get main centerline line.
    /// ② For each branch: find closest open-end connector to main (or midpoint / LocationPoint fallback).
    /// ③ Project branch reference point onto the infinite main line
    ///    → compute 3-D perpendicular offset vector from branch to main centerline.
    /// ④ MoveElement the whole branch by the perpendicular vector (no axis filtering — Microdesk standard).
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class AlignBranchCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                LogHelper.Log("[ALIGN_BRANCH] ═══════════════════════════════════════");
                LogHelper.Log("[ALIGN_BRANCH] Starting Align Branch command");

                // Bước 1: Chọn ống chính (main) | Step 1: Pick main pipe/duct
                Reference mainRef = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    new SelectionHelper.MEPFamilySelectionFilter(),
                    "Chọn ống chính (main) | Pick main pipe/duct");

                Element mainElement = doc.GetElement(mainRef);

                // Lấy centerline của main | Get main centerline
                Line mainLine = GetCenterLine(mainElement);
                if (mainLine == null)
                {
                    TaskDialog.Show("Lỗi | Error",
                        "Ống chính không có đường tâm (LocationCurve).\n" +
                        "Main element has no centerline.");
                    return Result.Failed;
                }

                LogHelper.Log($"[ALIGN_BRANCH] Main: {mainElement.Category?.Name} (ID: {mainElement.Id})");

                // Bước 2: Chọn các nhánh cần căn chỉnh | Step 2: Pick branches to align
                IList<Reference> branchRefs = uidoc.Selection.PickObjects(
                    ObjectType.Element,
                    new SelectionHelper.MEPFamilySelectionFilter(),
                    "Chọn các nhánh cần căn chỉnh (Enter xác nhận) | Pick branches to align (Enter to confirm)");

                if (branchRefs == null || branchRefs.Count == 0)
                {
                    TaskDialog.Show("Thông tin", "Không chọn element nào.");
                    return Result.Cancelled;
                }

                LogHelper.Log($"[ALIGN_BRANCH] Selected {branchRefs.Count} branches");

                // Bước 3: Thực hiện căn chỉnh | Step 3: Execute alignment
                int aligned = 0;
                int skipped = 0;

                using (Transaction trans = new Transaction(doc, "Align Branch"))
                {
                    trans.Start();

                    foreach (Reference bRef in branchRefs)
                    {
                        Element branch = doc.GetElement(bRef);
                        if (branch == null || branch.Id == mainElement.Id)
                        {
                            skipped++;
                            continue;
                        }

                        try
                        {
                            // Unpin nếu cần | Unpin if needed
                            ConnectionHelper.UnpinElementIfPinned(doc, branch);

                            // ── BƯỚC A: Lấy điểm tham chiếu của branch ──────────────────────────
                            // Microdesk: dùng connector đầu hở gần main nhất.
                            // Fallback: midpoint của LocationCurve (cho pipe/duct dài),
                            //           hoặc LocationPoint (cho fitting/equipment).
                            // ── STEP A: Get branch reference point ──────────────────────────────
                            // Microdesk: use closest open-end connector to main.
                            // Fallback: LocationCurve midpoint (for pipe/duct),
                            //           or LocationPoint (for fitting/equipment).
                            XYZ branchRef = GetBranchReferencePoint(branch, mainLine);
                            if (branchRef == null)
                            {
                                LogHelper.Log($"[ALIGN_BRANCH] Skip {branch.Id}: no reference point");
                                skipped++;
                                continue;
                            }

                            // ── BƯỚC B: Tính vector vuông góc từ branchRef đến đường tâm main ──
                            // Microdesk: perpendicular offset = closest point on main − branch ref.
                            // Project branchRef lên infinite line của main → closestOnMain.
                            // perpendicularOffset = closestOnMain − branchRef  (vector 3D vuông góc với main).
                            // ── STEP B: Compute perpendicular offset to main centerline ──────────
                            // perpendicularOffset = closestOnMain − branchRef (3-D, ⊥ to main dir).
                            XYZ closestOnMain = ProjectPointOnInfiniteLine(branchRef, mainLine);
                            XYZ perpendicularOffset = closestOnMain - branchRef;

                            // ── BƯỚC C: Dùng toàn bộ vector vuông góc — chuẩn Microdesk ────────
                            // Microdesk không lọc theo trục: di chuyển branch theo đúng vector
                            // vuông góc từ điểm tham chiếu đến đường tâm main.
                            // ── STEP C: Use full perpendicular offset — Microdesk standard ─────────
                            // No axis filtering: move branch by the exact perpendicular vector.
                            XYZ moveVector = perpendicularOffset;

                            if (moveVector.GetLength() < 0.0001)
                            {
                                LogHelper.Log($"[ALIGN_BRANCH] Skip {branch.Id}: already aligned");
                                skipped++;
                                continue;
                            }

                            // ── BƯỚC D: Di chuyển toàn bộ branch ────────────────────────────────
                            // MoveElement dịch chuyển element nguyên khối theo moveVector.
                            // ── STEP D: Move whole branch element ───────────────────────────────
                            ElementTransformUtils.MoveElement(doc, branch.Id, moveVector);
                            aligned++;

                            LogHelper.Log($"[ALIGN_BRANCH] \u2713 Aligned {branch.Id}: " +
                                $"ref({branchRef.X.ToMillimeters():F0},{branchRef.Y.ToMillimeters():F0},{branchRef.Z.ToMillimeters():F0})mm " +
                                $"→ move({moveVector.X.ToMillimeters():F0},{moveVector.Y.ToMillimeters():F0},{moveVector.Z.ToMillimeters():F0})mm");
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Log($"[ALIGN_BRANCH] \u2717 Failed {branch.Id}: {ex.Message}");
                            skipped++;
                        }
                    }

                    trans.Commit();
                }

                // Kết quả | Result
                TaskDialog.Show("Kết quả | Result",
                    $"\u2713 Đã căn chỉnh: {aligned} elements\n" +
                    (skipped > 0 ? $"\u2022 Bỏ qua: {skipped} elements\n" : "") +
                    $"\nỐng chính: {mainElement.Category?.Name} (ID: {mainElement.Id})");

                LogHelper.Log($"[ALIGN_BRANCH] Done: {aligned} aligned, {skipped} skipped");
                return aligned > 0 ? Result.Succeeded : Result.Failed;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                LogHelper.Log("[ALIGN_BRANCH] Cancelled by user");
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[ALIGN_BRANCH] Exception: {ex.Message}");
                message = ex.Message;
                return Result.Failed;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Lấy centerline (đường tâm) từ pipe/duct qua LocationCurve.
        /// Get centerline from pipe/duct via LocationCurve.
        /// </summary>
        private Line GetCenterLine(Element element)
        {
            var loc = element.Location as LocationCurve;
            return loc?.Curve as Line;
        }

        /// <summary>
        /// Microdesk: lấy điểm tham chiếu di chuyển của branch.
        /// Ưu tiên theo thứ tự:
        ///   1. Connector đầu hở (unconnected) gần main nhất — chuẩn nhất.
        ///   2. Midpoint của LocationCurve — cho pipe/duct không có connector hở.
        ///   3. LocationPoint — cho fitting/equipment.
        /// ---
        /// Microdesk: get branch movement reference point.
        /// Priority order:
        ///   1. Closest unconnected connector to main — most accurate.
        ///   2. LocationCurve midpoint — for pipe/duct with no open connector.
        ///   3. LocationPoint — for fittings / equipment.
        /// </summary>
        private XYZ GetBranchReferencePoint(Element branch, Line mainLine)
        {
            var cm = ConnectionHelper.GetConnectorManager(branch);
            if (cm != null)
            {
                // 1. Ưu tiên connector chưa nối gần main nhất | Prefer closest unconnected connector
                Connector bestUnconnected = null;
                Connector bestAny = null;
                double minUnconnectedDist = double.MaxValue;
                double minAnyDist = double.MaxValue;

                foreach (Connector c in cm.Connectors)
                {
                    XYZ proj = ProjectPointOnInfiniteLine(c.Origin, mainLine);
                    double dist = c.Origin.DistanceTo(proj);

                    if (!c.IsConnected && dist < minUnconnectedDist)
                    {
                        minUnconnectedDist = dist;
                        bestUnconnected = c;
                    }
                    if (dist < minAnyDist)
                    {
                        minAnyDist = dist;
                        bestAny = c;
                    }
                }

                Connector chosen = bestUnconnected ?? bestAny;
                if (chosen != null)
                    return chosen.Origin;
            }

            // 2. Midpoint của LocationCurve | LocationCurve midpoint
            if (branch.Location is LocationCurve lc)
            {
                Curve curve = lc.Curve;
                return curve.Evaluate(0.5, true);
            }

            // 3. LocationPoint (fitting, equipment) | LocationPoint fallback
            if (branch.Location is LocationPoint lp)
                return lp.Point;

            return null;
        }

        /// <summary>
        /// Project điểm lên đường thẳng VÔ HẠN (infinite line) — điểm gần nhất trên line.
        /// Microdesk dùng infinite line để đảm bảo luôn có điểm chiếu dù branch ở vị trí nào.
        /// ---
        /// Project point onto INFINITE line — find the closest point on the infinite line.
        /// Microdesk uses infinite line so projection always exists regardless of branch position.
        /// </summary>
        private XYZ ProjectPointOnInfiniteLine(XYZ point, Line line)
        {
            XYZ lineDir = line.Direction.Normalize();
            XYZ lineOrigin = line.GetEndPoint(0);
            XYZ v = point - lineOrigin;
            double t = v.DotProduct(lineDir);
            return lineOrigin + t * lineDir;
        }

        #endregion
    }
}
