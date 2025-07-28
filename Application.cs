using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace MEPConnector
{
    /// <summary>
    /// Lớp Application chính cho MEP Connector add-in
    /// </summary>
    public class Application : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Tạo ribbon tab mới cho MEP Connector
                string tabName = "MEP Connector";
                application.CreateRibbonTab(tabName);

                // Tạo ribbon panel
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Kết nối MEP");

                // Thêm nút Move Connect
                PushButtonData moveConnectData = new PushButtonData(
                    "MoveConnect",
                    "Move Connect",
                    Assembly.GetExecutingAssembly().Location,
                    "MEPConnector.Commands.MoveConnectCommand");

                PushButton moveConnectButton = panel.AddItem(moveConnectData) as PushButton;
                moveConnectButton.ToolTip = "Di chuyển và kết nối các MEP family";
                moveConnectButton.LongDescription = "Click vào một MEP family đích, sau đó click vào MEP family muốn di chuyển. " +
                    "Family thứ hai sẽ được di chuyển để kết nối với family đầu tiên.";

                // Thêm nút Move Connect Align
                PushButtonData moveConnectAlignData = new PushButtonData(
                    "MoveConnectAlign",
                    "Move Connect\nAlign",
                    Assembly.GetExecutingAssembly().Location,
                    "MEPConnector.Commands.MoveConnectAlignCommand");

                PushButton moveConnectAlignButton = panel.AddItem(moveConnectAlignData) as PushButton;
                moveConnectAlignButton.ToolTip = "Di chuyển, căn chỉnh và kết nối các MEP family";
                moveConnectAlignButton.LongDescription = "Click vào một MEP family đích, sau đó click vào MEP family muốn di chuyển. " +
                    "Family thứ hai sẽ được di chuyển và căn chỉnh để kết nối hoàn hảo với family đầu tiên.";

                // Thêm nút Disconnect
                PushButtonData disconnectData = new PushButtonData(
                    "Disconnect",
                    "Disconnect",
                    Assembly.GetExecutingAssembly().Location,
                    "MEPConnector.Commands.DisconnectCommand");

                PushButton disconnectButton = panel.AddItem(disconnectData) as PushButton;
                disconnectButton.ToolTip = "Ngắt kết nối MEP family";
                disconnectButton.LongDescription = "Click vào một MEP family để ngắt tất cả các kết nối của nó.";

                // Thêm icon nếu có
                try
                {
                    // Bạn có thể thêm icon 32x32 pixel ở đây
                    // moveConnectButton.LargeImage = new BitmapImage(new Uri("pack://application:,,,/MEPConnector;component/Resources/MoveConnect.png"));
                    // moveConnectAlignButton.LargeImage = new BitmapImage(new Uri("pack://application:,,,/MEPConnector;component/Resources/MoveConnectAlign.png"));
                    // disconnectButton.LargeImage = new BitmapImage(new Uri("pack://application:,,,/MEPConnector;component/Resources/Disconnect.png"));
                }
                catch
                {
                    // Nếu không tìm thấy icon, bỏ qua lỗi
                }

                return Result.Succeeded;
            }
            catch
            {
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            // Cleanup nếu cần thiết
            return Result.Succeeded;
        }
    }
}
