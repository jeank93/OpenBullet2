using OpenBullet2.Native.Extensions;
using OpenBullet2.Native.ViewModels;
using System.Windows.Controls;

namespace OpenBullet2.Native.Views.Pages;

/// <summary>
/// Interaction logic for ConfigReadme.xaml
/// </summary>
public partial class ConfigReadme : Page
{
    private readonly ConfigReadmeViewModel vm;

    public ConfigReadme(ConfigReadmeViewModel vm)
    {
        this.vm = vm;
        DataContext = vm;

        InitializeComponent();
    }

    // TODO: Find out why the preview doesn't update when navigating to the page
    public void UpdateViewModel()
    {
        vm.UpdateViewModel();
        readmeRTB.Document.Blocks.Clear();
        readmeRTB.AppendText(vm.Readme ?? string.Empty);
    }

    private void ReadmeChanged(object sender, TextChangedEventArgs e)
    {
        var newText = readmeRTB.GetText();

        if (!string.IsNullOrWhiteSpace(newText))
        {
            vm.Readme = newText;
        }
    }
}
