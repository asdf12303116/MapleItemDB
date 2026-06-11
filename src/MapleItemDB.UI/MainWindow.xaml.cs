using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using MapleItemDB.Core.Models;
using MapleItemDB.UI.ViewModels;
using Microsoft.Win32;

namespace MapleItemDB.UI;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;

        SearchResultsGrid.CommandBindings.Add(
            new CommandBinding(ApplicationCommands.Copy, OnCopyDataGrid));
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SearchResults))
        {
            // 等待 DataGrid 渲染完成后滚动到顶部
            Dispatcher.InvokeAsync(() =>
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(SearchResultsGrid);
                scrollViewer?.ScrollToTop();
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void OnSearchResultSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.SelectedItem = SearchResultsGrid.SelectedItem as ItemEntity;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
                return result;
            var descendant = FindVisualChild<T>(child);
            if (descendant != null)
                return descendant;
        }
        return null;
    }

    private void OnCopyDataGrid(object sender, ExecutedRoutedEventArgs e)
    {
        if (SearchResultsGrid.SelectedCells.Count == 0)
            return;

        var sb = new StringBuilder();
        var rowGroups = SearchResultsGrid.SelectedCells.GroupBy(c => c.Item);

        foreach (var row in rowGroups)
        {
            var rowText = string.Join("\t", row.Select(cell =>
            {
                if (cell.Column.GetCellContent(cell.Item) is TextBlock tb)
                    return tb.Text;
                return string.Empty;
            }));
            sb.AppendLine(rowText);
        }

        var text = sb.ToString().TrimEnd('\r', '\n');
        if (text.Length > 0)
        {
            try
            {
                System.Windows.Forms.Clipboard.SetDataObject(text, true, 3, 100);
            }
            catch
            {
                // WinForms Clipboard 内部已重试，忽略
            }
        }

        e.Handled = true;
    }

    private async void OnExtractClick(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "更新wz数据将从游戏目录重新提取所有信息，覆盖现有数据。\n确定要继续吗？",
            "确认更新wz数据",
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

    private async void OnUpdateSnClick(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "更新SN数据将导入新的 SN 映射，并立即回填到现有道具数据。\n确定要继续吗？",
            "确认更新SN数据",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        var dialog = new OpenFileDialog
        {
            Title = "选择 SN 数据文件",
            Filter = "SN 文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog() == true)
        {
            var vm = (MainViewModel)DataContext;
            await vm.UpdateSnDataCommand.ExecuteAsync(dialog.FileName);
        }
    }
}
