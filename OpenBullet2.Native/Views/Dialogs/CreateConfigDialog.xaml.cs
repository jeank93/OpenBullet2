using OpenBullet2.Core.Services;
using OpenBullet2.Native.DTOs;
using OpenBullet2.Native.Helpers;
using OpenBullet2.Native.Views.Pages;
using RuriLib.Functions.Files;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenBullet2.Native.Views.Dialogs;

/// <summary>
/// Interaction logic for CreateConfigDialog.xaml
/// </summary>
public partial class CreateConfigDialog : Page
{
    private readonly object caller;

    public CreateConfigDialog(object caller)
    {
        InitializeComponent();
        this.caller = caller;

        var settings = SP.GetService<OpenBulletSettingsService>().Settings;
        authorTextbox.Text = settings.GeneralSettings.DefaultAuthor;
        nameTextbox.Focus();

        categoryCombobox.Items.Add("Default");

        var categories = SP.GetService<ConfigService>().Configs
            .Select(c => c.Metadata.Category)
            .Where(category => category != "Default")
            .Distinct();

        foreach (var category in categories)
        {
            categoryCombobox.Items.Add(category);
        }

        categoryCombobox.SelectedIndex = 0;
    }

    private async Task CreateAndCloseAsync()
    {
        if (caller is Configs page)
        {
            var dto = new ConfigForCreationDto
            {
                Name = nameTextbox.Text,
                Category = categoryCombobox.Text,
                Author = authorTextbox.Text
            };

            // Check if name is ok
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                Alert.Error("Invalid name", "The name cannot be blank");
                return;
            }

            await page.CreateConfigAsync(dto);
        }
        ((MainDialog)Parent).Close();
    }

    private async void Accept(object sender, RoutedEventArgs e) => await CreateAndCloseAsync();

    private async void TextboxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await CreateAndCloseAsync();
        }
    }
}
