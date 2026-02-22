using System.Windows;
using MapleItemDB.UI.ViewModels;
using Microsoft.Win32;

namespace MapleItemDB.UI;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void OnExtractClick(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "更新数据将从游戏目录重新提取所有信息，覆盖现有数据。\n确定要继续吗？",
            "确认更新数据",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        var dialog = new OpenFolderDialog
        {
            Title = "选择冒险岛游戏目录 (包含 Base.wz 的文件夹)"
        };

        if (dialog.ShowDialog() == true)
        {
            var vm = (MainViewModel)DataContext;
            await vm.ExtractDataCommand.ExecuteAsync(dialog.FolderName);
        }
    }
}
