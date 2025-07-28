# Hướng dẫn cài đặt MEP Connector Add-in cho Revit

## Yêu cầu hệ thống
- Autodesk Revit (bất kỳ phiên bản từ 2020-2026)
- Windows 10/11 (64-bit)
- .NET Framework 4.8 (thường đã có sẵn)

## Phiên bản Revit được hỗ trợ
✅ Revit 2020, 2021, 2022, 2023, 2024, 2025, 2026

## Cách cài đặt

### 🚀 Cách 1: Tự động cho tất cả phiên bản (Khuyến nghị)

1. **Build tất cả phiên bản:**
   - Double-click file `BuildAllVersions.bat`
   - Chờ script build xong cho tất cả phiên bản

2. **Cài đặt tự động:**
   - Double-click file `InstallAllVersions.bat`
   - Script sẽ tự động phát hiện và cài đặt cho các phiên bản Revit có trên máy

### 🎯 Cách 2: Cài đặt cho phiên bản cụ thể

1. **Build cho phiên bản cụ thể:**
   ```
   BuildAllVersions.bat
   ```
   Hoặc build riêng:
   ```
   dotnet build MEPConnector.csproj --configuration Release2024
   ```

2. **Cài đặt cho phiên bản cụ thể:**
   ```
   InstallToRevit.bat 2024
   ```
   (Thay 2024 bằng năm Revit của bạn)

### 🔧 Cách 3: Thủ công
### 🔧 Cách 3: Thủ công

1. **Build cho phiên bản bạn cần:**
   ```bash
   dotnet build MEPConnector.csproj --configuration Release2024
   ```

2. **Tìm thư mục Revit Addins:**
   - Mở Windows Explorer
   - Đi đến: `%APPDATA%\Autodesk\Revit\Addins\2024\`
   - (Thay 2024 bằng phiên bản Revit của bạn)

3. **Copy 2 files:**
   - `bin\Release2024\MEPConnector2024.dll`
   - `Addins\MEPConnector2024.addin`

4. **Khởi động lại Revit**

## File Structure sau khi build
```
bin/
├── Release2020/MEPConnector2020.dll
├── Release2021/MEPConnector2021.dll
├── Release2022/MEPConnector2022.dll
├── Release2023/MEPConnector2023.dll
├── Release2024/MEPConnector2024.dll
├── Release2025/MEPConnector2025.dll
└── Release2026/MEPConnector2026.dll

Addins/
├── MEPConnector2020.addin
├── MEPConnector2021.addin
├── MEPConnector2022.addin
├── MEPConnector2023.addin
├── MEPConnector2024.addin
├── MEPConnector2025.addin
└── MEPConnector2026.addin
```

### Bước 3: Kiểm tra cài đặt
### Bước 3: Kiểm tra cài đặt
1. Khởi động Revit
2. Kiểm tra xem có tab "MEP Connector" xuất hiện trên ribbon không
3. Tab sẽ chứa 2 nút: "Move Connect" và "Move Connect Align"

## Scripts hỗ trợ

### BuildAllVersions.bat
- Build tất cả phiên bản Revit từ 2020-2026
- Tự động tạo DLL riêng cho từng phiên bản
- Hiển thị kết quả build chi tiết

### InstallToRevit.bat
- Cài đặt cho một phiên bản Revit cụ thể
- Sử dụng: `InstallToRevit.bat 2024`
- Tự động copy files đến đúng thư mục

### InstallAllVersions.bat  
- Tự động phát hiện các phiên bản Revit đã cài
- Cài đặt MEP Connector cho tất cả phiên bản tìm thấy
- Báo cáo kết quả cài đặt chi tiết

## Cách sử dụng

### Move Connect
1. Click vào nút "Move Connect" trên tab MEP Connector
2. Click vào MEP family đích (nơi bạn muốn kết nối đến)
3. Click vào MEP family muốn di chuyển
4. Add-in sẽ tự động:
   - Unpin các element nếu cần
   - Di chuyển element thứ 2 đến vị trí kết nối
   - Thực hiện kết nối

### Move Connect Align  
1. Click vào nút "Move Connect Align" trên tab MEP Connector
2. Click vào MEP family đích (nơi bạn muốn kết nối đến)
3. Click vào MEP family muốn di chuyển
4. Add-in sẽ tự động:
   - Unpin các element nếu cần
   - Di chuyển element thứ 2 đến vị trí kết nối
   - Căn chỉnh hướng để kết nối hoàn hảo
   - Thực hiện kết nối

## Các element được hỗ trợ
- **HVAC**: Duct, Duct Fitting, Duct Accessory, Duct Terminal, Mechanical Equipment
- **Plumbing**: Pipe, Pipe Fitting, Pipe Accessory, Plumbing Fixture
- **Electrical**: Conduit, Cable Tray, Cable Tray Fitting, Conduit Fitting, Electrical Equipment, Electrical Fixture, Lighting Fixture

## Troubleshooting

### Add-in không xuất hiện
- Kiểm tra file .addin và .dll đã copy đúng vị trí
- Kiểm tra đường dẫn trong file .addin có chính xác không
- Restart Revit
- Kiểm tra Windows Event Log để xem có lỗi loading không

### Không thể kết nối elements
- Đảm bảo chọn đúng MEP elements (có connector)
- Kiểm tra connector chưa được kết nối với element khác
- Đảm bảo connector tương thích (cùng domain, size, flow direction)

### Element bị pin
- Add-in sẽ tự động unpin, nhưng nếu không được thì unpin thủ công trước

## Liên hệ hỗ trợ
Software Licorp - 2025
