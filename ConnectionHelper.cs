using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Plumbing;
using Quoc_MEP.Lib;
using Nice3point.Revit.Extensions;

namespace Quoc_MEP
{
    /// <summary>
    /// Helper class xử lý kết nối MEP elements.
    /// Sử dụng thuật toán Best Pair Matching — hoạt động ở MỌI khoảng cách.
    /// ---
    /// Helper class for MEP element connection operations.
    /// Uses Best Pair Matching algorithm — works at ANY distance.
    /// </summary>
    public static class ConnectionHelper
    {
        #region Kết nối | Connect Operations

        /// <summary>
        /// Di chuyển và kết nối hai MEP element (không xoay).
        /// Move source element to destination connector and connect (no rotation).
        /// Hoạt động ở mọi khoảng cách | Works at any distance.
        /// </summary>
        public static bool MoveAndConnect(Document doc, Element sourceElement, Element destElement)
        {
            try
            {
                // Tìm cặp connector tốt nhất | Find best connector pair
                var pair = FindBestConnectorPair(sourceElement, destElement);
                if (pair == null)
                {
                    LogHelper.Log("[ConnectionHelper] MoveAndConnect: No compatible connector pair found");
                    return false;
                }

                var (srcConn, destConn) = pair.Value;
                LogHelper.Log($"[ConnectionHelper] MoveAndConnect: Pair found — " +
                    $"Src[{srcConn.Id}] {srcConn.Domain} Ø{RadiusToMM(srcConn.Radius)}mm → " +
                    $"Dest[{destConn.Id}] {destConn.Domain} Ø{RadiusToMM(destConn.Radius)}mm");

                // Di chuyển | Move source to align
                XYZ moveVector = destConn.Origin - srcConn.Origin;
                ElementTransformUtils.MoveElement(doc, sourceElement.Id, moveVector);

                // Re-fetch connector sau khi move | Re-fetch after move
                var pairAfter = FindBestConnectorPair(sourceElement, destElement);
                if (pairAfter == null)
                {
                    LogHelper.Log("[ConnectionHelper] MoveAndConnect: Lost pair after move");
                    return false;
                }

                var (srcAfter, destAfter) = pairAfter.Value;
                srcAfter.ConnectTo(destAfter);
                LogHelper.Log("[ConnectionHelper] MoveAndConnect: ✓ Connected");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[ConnectionHelper] MoveAndConnect error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Di chuyển, xoay căn chỉnh và kết nối hai MEP element.
        /// Thuật toán Microdesk MoCo-Align: Rotate → Move → Connect.
        /// Hoạt động ở mọi khoảng cách | Works at any distance.
        /// ---
        /// Move, rotate to align, and connect two MEP elements.
        /// Microdesk MoCo-Align approach: Rotate → Move → Connect.
        /// </summary>
        public static bool MoveConnectAndAlign(Document doc, Element sourceElement, Element destElement)
        {
            try
            {
                // Tìm cặp connector tốt nhất | Find best connector pair
                var pair = FindBestConnectorPair(sourceElement, destElement);
                if (pair == null)
                {
                    LogHelper.Log("[ConnectionHelper] MoveConnectAndAlign: No compatible pair");
                    return false;
                }

                var (srcConn, destConn) = pair.Value;
                LogHelper.Log($"[ConnectionHelper] MoveConnectAndAlign: Pair — " +
                    $"Src[{srcConn.Id}] → Dest[{destConn.Id}]");

                XYZ destConnOrigin = destConn.Origin;

                // ===== BƯỚC 1: XOAY | Step 1: ROTATE =====
                // Xoay source để connector hướng đối diện dest connector
                // Rotate source so its connector faces opposite to dest connector
                XYZ srcDir = srcConn.CoordinateSystem.BasisZ;
                XYZ destDir = destConn.CoordinateSystem.BasisZ.Negate(); // Cần đối diện

                XYZ rotationAxis = srcDir.CrossProduct(destDir);
                double rotAxisLen = rotationAxis.GetLength();

                if (rotAxisLen > 0.001)
                {
                    // Hai vector không song song → xoay quanh cross product axis
                    rotationAxis = rotationAxis.Normalize();
                    double dot = Math.Max(-1.0, Math.Min(1.0, srcDir.DotProduct(destDir)));
                    double angle = Math.Acos(dot);

                    if (Math.Abs(angle) > 0.001)
                    {
                        // Dùng vị trí connector làm tâm xoay | Use connector origin as rotation center
                        XYZ rotCenter = srcConn.Origin;
                        Line axisLine = Line.CreateBound(rotCenter, rotCenter + rotationAxis);
                        ElementTransformUtils.RotateElement(doc, sourceElement.Id, axisLine, angle);
                        LogHelper.Log($"[ConnectionHelper] Rotated {angle * 180 / Math.PI:F1}°");
                    }
                }
                else if (srcDir.DotProduct(destDir) < -0.999)
                {
                    // Hai vector đối diện (180°) → xoay 180° quanh trục vuông góc
                    // Vectors are opposite → rotate 180° around perpendicular axis
                    XYZ perpAxis = GetPerpendicularAxis(srcDir);
                    XYZ rotCenter = srcConn.Origin;
                    Line axisLine = Line.CreateBound(rotCenter, rotCenter + perpAxis);
                    ElementTransformUtils.RotateElement(doc, sourceElement.Id, axisLine, Math.PI);
                    LogHelper.Log("[ConnectionHelper] Rotated 180° (opposite vectors)");
                }

                // ===== BƯỚC 2: DI CHUYỂN | Step 2: MOVE =====
                // Re-fetch connector sau khi xoay | Re-fetch after rotation
                var pairAfterRotate = FindBestConnectorPair(sourceElement, destElement);
                if (pairAfterRotate == null)
                {
                    LogHelper.Log("[ConnectionHelper] MoveConnectAndAlign: Lost pair after rotate");
                    return false;
                }

                var (srcAfterRotate, _) = pairAfterRotate.Value;
                XYZ moveVector = destConnOrigin - srcAfterRotate.Origin;
                ElementTransformUtils.MoveElement(doc, sourceElement.Id, moveVector);
                LogHelper.Log($"[ConnectionHelper] Moved {moveVector.GetLength().ToMillimeters():F0}mm");

                // ===== BƯỚC 3: KẾT NỐI | Step 3: CONNECT =====
                var pairFinal = FindBestConnectorPair(sourceElement, destElement);
                if (pairFinal == null)
                {
                    LogHelper.Log("[ConnectionHelper] MoveConnectAndAlign: Lost pair after move");
                    return false;
                }

                var (srcFinal, destFinal) = pairFinal.Value;

                // Kiểm tra khoảng cách sau khi align — phải rất gần
                // Verify distance after align — must be very close
                double finalDist = srcFinal.Origin.DistanceTo(destFinal.Origin);
                if (finalDist > 0.01) // > ~3mm → có vấn đề
                {
                    LogHelper.Log($"[ConnectionHelper] Warning: Gap after align = {finalDist.ToMillimeters():F1}mm, attempting force connect");
                }

                srcFinal.ConnectTo(destFinal);
                LogHelper.Log("[ConnectionHelper] MoveConnectAndAlign: ✓ Connected");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[ConnectionHelper] MoveConnectAndAlign error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Tìm cặp Connector | Best Pair Matching

        /// <summary>
        /// Tìm cặp connector tốt nhất giữa 2 elements.
        /// Ưu tiên: ① Compatibility (domain+size) → ② Direction (đối diện) → ③ Distance.
        /// KHÔNG phụ thuộc khoảng cách giữa 2 elements.
        /// ---
        /// Find best connector pair between two elements.
        /// Priority: ① Compatibility (domain+size) → ② Direction (opposing) → ③ Distance.
        /// Distance-INDEPENDENT — works at any distance.
        /// </summary>
        public static (Connector src, Connector dest)? FindBestConnectorPair(
            Element sourceElement, Element destElement)
        {
            try
            {
                ConnectorManager srcCM = GetConnectorManager(sourceElement);
                ConnectorManager destCM = GetConnectorManager(destElement);
                if (srcCM == null || destCM == null) return null;

                // Thu thập tất cả cặp khả thi | Collect all viable pairs
                var candidates = new List<(Connector src, Connector dest, double score)>();

                foreach (Connector sc in srcCM.Connectors)
                {
                    if (sc.IsConnected) continue;

                    foreach (Connector dc in destCM.Connectors)
                    {
                        if (dc.IsConnected) continue;

                        // ① Compatibility check — BẮT BUỘC
                        if (sc.Domain != dc.Domain) continue;
                        if (Math.Abs(sc.Radius - dc.Radius) > 0.01) continue; // Nới lỏng tolerance

                        // Flow direction check
                        if (sc.Direction == FlowDirectionType.In && dc.Direction == FlowDirectionType.In)
                            continue;
                        if (sc.Direction == FlowDirectionType.Out && dc.Direction == FlowDirectionType.Out)
                            continue;

                        // ② Direction score — ưu tiên connector đối diện
                        double dirScore = 1.0;
                        try
                        {
                            XYZ srcDir = sc.CoordinateSystem.BasisZ;
                            XYZ destDir = dc.CoordinateSystem.BasisZ;
                            double dot = srcDir.DotProduct(destDir);

                            // Đối diện (dot ≈ -1) → score thấp = tốt
                            // Cùng hướng (dot ≈ +1) → score cao = xấu
                            dirScore = 1.0 + dot; // Range: [0, 2] — 0=đối diện, 2=cùng hướng
                        }
                        catch { }

                        // ③ Distance (chỉ dùng để tie-break, KHÔNG phải yếu tố chính)
                        double distance = sc.Origin.DistanceTo(dc.Origin);
                        double normalizedDist = Math.Min(distance / 100.0, 1.0); // Normalize, cap ở 100ft

                        // Tổng score: direction quan trọng gấp 10 lần distance
                        // Total: direction is 10x more important than distance
                        double totalScore = dirScore * 10.0 + normalizedDist;

                        candidates.Add((sc, dc, totalScore));
                    }
                }

                if (candidates.Count == 0)
                {
                    LogHelper.Log("[ConnectionHelper] FindBestPair: No compatible pairs found");
                    return null;
                }

                // Sắp xếp theo score — thấp nhất = tốt nhất | Sort by score — lowest = best
                candidates.Sort((a, b) => a.score.CompareTo(b.score));
                var best = candidates[0];

                LogHelper.Log($"[ConnectionHelper] FindBestPair: {candidates.Count} pairs found, " +
                    $"best score={best.score:F2}");

                return (best.src, best.dest);
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[ConnectionHelper] FindBestPair error: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Ngắt kết nối | Disconnect Operations

        /// <summary>
        /// Ngắt tất cả kết nối của một MEP element.
        /// Disconnect all connectors of a MEP element.
        /// </summary>
        /// <returns>Số connector đã ngắt | Number of disconnected connectors</returns>
        public static int DisconnectElement(Element element)
        {
            try
            {
                ConnectorManager connectorManager = GetConnectorManager(element);
                if (connectorManager == null) return 0;

                int count = 0;
                foreach (Connector connector in connectorManager.Connectors)
                {
                    if (!connector.IsConnected) continue;

                    var toDisconnect = new List<Connector>();
                    foreach (Connector connected in connector.AllRefs)
                    {
                        if (connected.Owner.Id != element.Id)
                            toDisconnect.Add(connected);
                    }

                    foreach (var connected in toDisconnect)
                    {
                        try
                        {
                            connector.DisconnectFrom(connected);
                            count++;
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Log($"[ConnectionHelper] DisconnectFrom failed: {ex.Message}");
                        }
                    }
                }

                return count;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[ConnectionHelper] DisconnectElement error: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region Kiểm tra | Validation

        /// <summary>
        /// Kiểm tra element có connector đang kết nối không.
        /// Check if element has any connected connectors.
        /// </summary>
        public static bool HasConnectedConnectors(Element element)
        {
            try
            {
                ConnectorManager cm = GetConnectorManager(element);
                if (cm == null) return false;
                foreach (Connector c in cm.Connectors)
                {
                    if (c.IsConnected) return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[ConnectionHelper] HasConnectedConnectors error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Tiện ích | Utilities

        /// <summary>
        /// Unpin element nếu đang bị pin.
        /// Unpin element if it is currently pinned.
        /// </summary>
        public static void UnpinElementIfPinned(Document doc, Element element)
        {
            try
            {
                if (element.Pinned) element.Pinned = false;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"[ConnectionHelper] UnpinElementIfPinned error: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy ConnectorManager từ element (MEPCurve hoặc FamilyInstance).
        /// Get ConnectorManager from element (supports MEPCurve and FamilyInstance).
        /// </summary>
        public static ConnectorManager GetConnectorManager(Element element)
        {
            if (element is MEPCurve mepCurve)
                return mepCurve.ConnectorManager;
            if (element is FamilyInstance familyInstance)
                return familyInstance.MEPModel?.ConnectorManager;
            return null;
        }

        /// <summary>
        /// Lấy vị trí trung tâm của element.
        /// Get center location of element.
        /// </summary>
        public static XYZ GetElementLocation(Element element)
        {
            if (element.Location is LocationPoint lp) return lp.Point;
            if (element.Location is LocationCurve lc) return lc.Curve.Evaluate(0.5, true);
            return null;
        }

        /// <summary>
        /// Chuyển radius (feet) sang mm hiển thị.
        /// Convert radius (feet) to mm for display.
        /// </summary>
        public static double RadiusToMM(double radiusFeet)
        {
            return Math.Round(radiusFeet * 2.ToMillimeters(), 0); // diameter in mm
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Tìm trục vuông góc với vector cho trước (dùng khi cần xoay 180°).
        /// Find a perpendicular axis to the given vector (used for 180° rotation).
        /// </summary>
        private static XYZ GetPerpendicularAxis(XYZ vector)
        {
            // Chọn trục không song song với vector | Pick axis not parallel to vector
            XYZ candidate = Math.Abs(vector.Z) < 0.9 ? XYZ.BasisZ : XYZ.BasisX;
            return vector.CrossProduct(candidate).Normalize();
        }

        #endregion
    }
}
