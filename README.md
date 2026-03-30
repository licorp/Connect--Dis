# MEP Connector - Revit Add-in

## Mô tả
Add-in cho Autodesk Revit với tính năng kết nối các MEP family (Mechanical, Electrical, Plumbing).
**Hỗ trợ từ Revit 2020 đến 2026!**

## Version History

| Version | Date | Description |
|---------|------|-------------|
| **v1.3.0** | 2025-03-30 | Thêm Align Branch, Disconnect batch mode, Update Pipe Endpoint |
| **v1.2.0** | 2025-03-22 | Tối ưu code, tinh gọn, sửa lỗi kết nối |
| **v1.1.0** | 2025-03-15 | Thêm tính năng Disconnect, cải thiện Move Connect |
| **v1.0.0** | 2025-03-01 | Phiên bản đầu tiên - Move Connect, Move Connect Align |

## Tính năng chính

### 1. Move Connect
- **Mô tả**: Di chuyển và kết nối hai MEP family
- **Cách sử dụng**:
1. Click vào MEP family đích (destination)
2. Click vào MEP family muốn di chuyển (source)
3. Family source sẽ được di chuyển và kết nối với family destination

### 2. Move Connect Align
- **Mô tả**: Di chuyển, căn chỉnh và kết nối hai MEP family một cách chính xác
- **Cách sử dụng**:
1. Click vào MEP family đích (destination)
2. Click vào MEP family muốn di chuyển (source)
3. Family source sẽ được di chuyển, căn chỉnh hướng và kết nối hoàn hảo với family destination

### 3. Align Branch ✨ NEW
- **Mô tả**: Căn chỉnh các nhánh ống/duct về ống chính theo cơ chế Microdesk
- **Cách sử dụng**:
1. Click vào ống chính (main)
2. Chọn các nhánh cần căn chỉnh
3. Các nhánh sẽ được di chuyển vuông góc với ống chính
- **Ưu điểm**: Tự động tìm connector đầu hở, hỗ trợ fitting/equipment

### 4. Disconnect ✨ NEW
- **Mô tả**: Ngắt kết nối MEP elements - hỗ trợ single hoặc batch mode
- **Single mode**: Chọn 1 element → ngắt tất cả kết nối
- **Batch mode**: Chọn nhiều elements → ngắt tất cả cùng lúc

### 5. Update Pipe Endpoint ✨ NEW
- **Mô tả**: Kéo endpoint của Pipe/Duct đến đối tượng đích và tự động kết nối
- **Cách sử dụng**:
1. Click vào Pipe/Duct cần kéo endpoint
2. Click vào đối tượng đích để kết nối
- **Lặp lại**: Tiếp tục chọn cặp mới hoặc ESC để thoát

### 6. Tính năng Unpin tự động
- Tự động unpin các element trước khi thực hiện kết nối nếu chúng đang bị pin
- Thông báo cho người dùng khi thực hiện unpin

## Phiên bản Revit được hỗ trợ
- Revit 2020
- Revit 2021
- Revit 2022
- Revit 2023
- Revit 2024
- Revit 2025
- Revit 2026

## Yêu cầu hệ thống
- Autodesk Revit (bất kỳ phiên bản từ 2020-2026)
- .NET Framework 4.8
- Windows 10/11

## Cài đặt

### Cách 1: Tự động (Khuyến nghị)
1. **Build tất cả phiên bản:**
   ```
   BuildAllVersions.bat
   ```

2. **Cài đặt cho phiên bản Revit bạn đang dùng:**
   ```
   InstallToRevit.bat 2024
   ```
   (Thay 2024 bằng phiên bản Revit của bạn: 2020, 2021, 2022, 2023, 2024, 2025, 2026)

### Cách 2: Thủ công
1. **Build project cho phiên bản cụ thể:**
   ```
   dotnet build MEPConnector.csproj --configuration Release2024
   ```

2. **Copy files vào thư mục Revit:**
   - Copy `bin\Release2024\MEPConnector2024.dll`
   - Copy `Addins\MEPConnector2024.addin`
   - Đến thư mục: `%APPDATA%\Autodesk\Revit\Addins\2024\`

3. **Khởi động lại Revit**

### Build cho nhiều phiên bản cùng lúc
Sử dụng script `BuildAllVersions.bat` để build cho tất cả phiên bản Revit từ 2020-2026.

## Cấu trúc dự án
```
MEPConnector/
├── Commands/
│   ├── MoveConnectCommand.cs          # Command cho Move Connect
│   └── MoveConnectAlignCommand.cs     # Command cho Move Connect Align
├── Utils/
│   ├── ConnectionHelper.cs            # Helper xử lý kết nối
│   └── SelectionHelper.cs            # Helper xử lý selection
├── Properties/
│   └── AssemblyInfo.cs               # Thông tin assembly
├── Addins/                           # Add-in files cho từng phiên bản
│   ├── MEPConnector2020.addin
│   ├── MEPConnector2021.addin
│   ├── MEPConnector2022.addin
│   ├── MEPConnector2023.addin
│   ├── MEPConnector2024.addin
│   ├── MEPConnector2025.addin
│   └── MEPConnector2026.addin
├── Application.cs                    # Main application class
├── MEPConnector.addin               # Default add-in manifest (2024)
├── MEPConnector.csproj              # Multi-version project file
├── BuildAllVersions.bat             # Script build tất cả phiên bản
└── InstallToRevit.bat              # Script cài đặt tự động
```

## Build Configurations
- **Release2020** - Build cho Revit 2020 → MEPConnector2020.dll
- **Release2021** - Build cho Revit 2021 → MEPConnector2021.dll  
- **Release2022** - Build cho Revit 2022 → MEPConnector2022.dll
- **Release2023** - Build cho Revit 2023 → MEPConnector2023.dll
- **Release2024** - Build cho Revit 2024 → MEPConnector2024.dll
- **Release2025** - Build cho Revit 2025 → MEPConnector2025.dll
- **Release2026** - Build cho Revit 2026 → MEPConnector2026.dll

## Các MEP Element được hỗ trợ
- **Mechanical**: Duct, Duct Fitting, Duct Accessory, Duct Terminal, Mechanical Equipment
- **Plumbing**: Pipe, Pipe Fitting, Pipe Accessory, Plumbing Fixture  
- **Electrical**: Conduit, Cable Tray, Cable Tray Fitting, Conduit Fitting, Electrical Equipment, Electrical Fixture, Lighting Fixture

## Lưu ý kỹ thuật
- Add-in sử dụng ConnectorManager để xử lý kết nối
- Tự động kiểm tra compatibility giữa các connector (domain, size, flow direction)
- Hỗ trợ xoay element để căn chỉnh đúng hướng kết nối
- Transaction được sử dụng để đảm bảo có thể rollback nếu có lỗi

## Phát triển
- Language: C#
- Framework: .NET 4.8  
- API: Revit API 2024
- IDE: Visual Studio 2022 hoặc VS Code

## Tác giả
Software Licorp - 2025
