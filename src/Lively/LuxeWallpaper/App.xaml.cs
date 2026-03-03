using System.Configuration;
using System.Data;
using System.Windows;
using Lively.Core;

namespace LuxeWallpaper;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IDesktopCore _desktopCore;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 初始化Lively核心服务
        try
        {
            // 由于WinDesktopCore构造函数需要多个依赖项，这里暂时使用null作为参数
            // 实际项目中应该使用依赖注入来提供这些服务
            _desktopCore = null;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"初始化核心服务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
        // 捕获未处理的异常
        this.DispatcherUnhandledException += (sender, args) =>
        {
            System.Windows.MessageBox.Show($"Dispatcher Error: {args.Exception.Message}\n\n{args.Exception.StackTrace}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            System.Windows.MessageBox.Show($"Domain Error: {exception?.Message}\n\n{exception?.StackTrace}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };
        
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            System.Windows.MessageBox.Show($"Task Error: {args.Exception.Message}\n\n{args.Exception.StackTrace}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.SetObserved();
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        
        // 释放核心服务
        _desktopCore?.Dispose();
    }

    public IDesktopCore DesktopCore => _desktopCore;
}
