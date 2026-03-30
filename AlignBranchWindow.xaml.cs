using System.Windows;

namespace Quoc_MEP
{
    /// <summary>
    /// Dialog cho phép chọn trục X, Y, Z để căn chỉnh nhánh.
    /// Dialog to select X, Y, Z axes for branch alignment.
    /// </summary>
    public partial class AlignBranchWindow : Window
    {
        /// <summary>Align theo X (trái/phải) | Align X (left/right)</summary>
        public bool AlignX { get; private set; }

        /// <summary>Align theo Y (trên/dưới) | Align Y (up/down)</summary>
        public bool AlignY { get; private set; }

        /// <summary>Align theo Z (cao độ) | Align Z (elevation)</summary>
        public bool AlignZ { get; private set; }

        public AlignBranchWindow()
        {
            InitializeComponent();
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            AlignX = cbAlignX.IsChecked == true;
            AlignY = cbAlignY.IsChecked == true;
            AlignZ = cbAlignZ.IsChecked == true;

            // Must select at least one axis
            if (!AlignX && !AlignY && !AlignZ)
            {
                MessageBox.Show(
                    "Vui lòng chọn ít nhất 1 trục!\nPlease select at least 1 axis!",
                    "Cảnh báo | Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
