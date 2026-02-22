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
