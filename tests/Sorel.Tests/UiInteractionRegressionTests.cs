using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using Sorel.Controls;
using Sorel.Models;
using Xunit;

namespace Sorel.Tests;

public sealed class UiInteractionRegressionTests
{
    [Fact]
    public void ProfileDisplayHidesTechnicalFieldsWithoutChangingDiagnosticLabel()
    {
        var profile = new LauncherProfile("782-private-profile-id", "Minecraft 1.8", "lunar", "1.8", "1.8.9")
        {
            Loaders = ["ichor"], LunarModule = "lunar"
        };
        Assert.Equal("Minecraft 1.8 · 1.8.9", profile.DisplayLabel);
        Assert.DoesNotContain(profile.Id, profile.DisplayLabel);
        Assert.DoesNotContain("ichor", profile.DisplayLabel);
        Assert.Contains(profile.Id, profile.DetailLabel);
        Assert.Contains("ichor", profile.DetailLabel);
    }

    [Fact]
    public async Task SwitchInitialStateAndRebindingAreStaticAndUserToggleDoesNotAnimateNeighbors()
    {
        await OnDispatcher(async () =>
        {
            var document = XDocument.Load(Path.Combine(RepositoryRoot(), "src/Sorel/MainWindow.xaml"));
            XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
            var element = document.Descendants().Single(e => (string?)e.Attribute(x + "Key") == "SmoothSwitch");
            var context = new ParserContext();
            context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            context.XmlnsDictionary.Add("x", x.NamespaceName);
            var style = (Style)XamlReader.Parse(element.ToString(), context);
            var first = new TestSwitch { Style = style, IsChecked = true };
            var other = new TestSwitch { Style = style, IsChecked = true };
            var panel = new StackPanel(); panel.Children.Add(first); panel.Children.Add(other);
            var window = new Window
            {
                Content = panel, Width = 150, Height = 150, Left = -20000, Top = -20000,
                ShowActivated = false, ShowInTaskbar = false, Opacity = 0
            };
            try
            {
                window.Show(); window.UpdateLayout();
                TranslateTransform Position(TestSwitch toggle) =>
                    (TranslateTransform)((FrameworkElement)toggle.Template.FindName("Thumb", toggle)).RenderTransform;
                Assert.Equal(20, Position(first).X);
                Assert.False(Position(first).HasAnimatedProperties);
                first.IsChecked = false;
                Assert.Equal(0, Position(first).X);
                first.IsChecked = true;
                Assert.Equal(20, Position(first).X);
                Assert.False(Position(first).HasAnimatedProperties);
                first.Toggle();
                Assert.False(first.IsChecked);
                Assert.Equal(20, Position(other).X);
                Assert.False(Position(other).HasAnimatedProperties);
                await Task.Delay(250);
                Assert.Equal(0, Position(first).X, precision: 3);
                first.Toggle();
                await Task.Delay(250);
                Assert.Equal(20, Position(first).X, precision: 3);
                Assert.Equal(20, Position(other).X);
            }
            finally { window.Close(); }
        });
    }

    private sealed class TestSwitch : SmoothToggleSwitch
    {
        public void Toggle() => OnToggle();
    }

    private static Task OnDispatcher(Func<Task> action)
    {
        var result = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            dispatcher.BeginInvoke(async () =>
            {
                try { await action(); result.SetResult(); }
                catch (Exception error) { result.SetException(error); }
                finally { dispatcher.BeginInvokeShutdown(DispatcherPriority.Send); }
            });
            Dispatcher.Run();
        });
        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return result.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }

    private static string RepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
            if (File.Exists(Path.Combine(current.FullName, "Sorel.sln"))) return current.FullName;
        throw new DirectoryNotFoundException("Sorel.sln");
    }
}
