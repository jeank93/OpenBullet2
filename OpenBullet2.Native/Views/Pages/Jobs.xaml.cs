using Newtonsoft.Json;
using OpenBullet2.Core.Models.Jobs;
using OpenBullet2.Core.Repositories;
using OpenBullet2.Native.Helpers;
using OpenBullet2.Native.Services;
using OpenBullet2.Native.ViewModels;
using OpenBullet2.Native.Views.Dialogs;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenBullet2.Native.Views.Pages;

/// <summary>
/// Interaction logic for Jobs.xaml
/// </summary>
public partial class Jobs : Page
{
    private readonly MainWindow mainWindow;
    private readonly IJobRepository jobRepo;
    private readonly JobsViewModel vm;
    private static readonly JsonSerializerSettings JsonSettings = new() { TypeNameHandling = TypeNameHandling.Auto };

    public Jobs()
    {
        mainWindow = SP.GetService<MainWindow>();
        jobRepo = SP.GetService<IJobRepository>();
        vm = SP.GetService<ViewModelsService>().Jobs;
        DataContext = vm;

        InitializeComponent();
    }

    private void NewJob(object sender, RoutedEventArgs e)
        => new MainDialog(new CreateJobDialog(this), "Select job type").ShowDialog();

    private void RemoveAll(object sender, RoutedEventArgs e)
    {
        try
        {
            vm.RemoveAll();
        }
        catch (Exception ex)
        {
            Alert.Exception(ex);
        }
    }

    private void EditJob(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: JobViewModel job })
        {
            EditJob(job);
        }
    }

    public async void EditJob(JobViewModel jobVM)
    {
        var entity = await jobRepo.GetAsync(jobVM.Id);
        var jobOptions = JsonConvert.DeserializeObject<JobOptionsWrapper>(entity.JobOptions ?? string.Empty, JsonSettings)?.Options
            ?? throw new InvalidOperationException("Could not deserialize job options");
        async void onAccept(JobOptions options)
        {
            jobVM = await vm.EditJobAsync(entity, options);
            mainWindow.DisplayJob(jobVM);
        }

        Page page = jobVM switch
        {
            MultiRunJobViewModel => new MultiRunJobOptionsDialog((MultiRunJobOptions)jobOptions, onAccept),
            ProxyCheckJobViewModel => new ProxyCheckJobOptionsDialog((ProxyCheckJobOptions)jobOptions, onAccept),
            _ => throw new NotImplementedException()
        };

        new MainDialog(page, $"Edit job #{entity.Id}", 800, 600).ShowDialog();
    }

    private async void CloneJob(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: JobViewModel jobVM })
        {
            return;
        }

        var entity = await jobRepo.GetAsync(jobVM.Id);
        var oldOptions = JsonConvert.DeserializeObject<JobOptionsWrapper>(entity.JobOptions ?? string.Empty, JsonSettings)?.Options
            ?? throw new InvalidOperationException("Could not deserialize job options");
        var newOptions = JobOptionsFactory.CloneExistant(oldOptions);

        async void onAccept(JobOptions options)
        {
            var cloned = await vm.CloneJobAsync(entity.JobType, options);
            mainWindow.DisplayJob(cloned);
        }

        Page page = jobVM switch
        {
            MultiRunJobViewModel => new MultiRunJobOptionsDialog((MultiRunJobOptions)newOptions, onAccept),
            ProxyCheckJobViewModel => new ProxyCheckJobOptionsDialog((ProxyCheckJobOptions)newOptions, onAccept),
            _ => throw new NotImplementedException()
        };

        new MainDialog(page, $"Clone job #{entity.Id}", 800, 600).ShowDialog();
    }

    private async void RemoveJob(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button { Tag: JobViewModel job })
            {
                await vm.RemoveJobAsync(job);
            }
        }
        catch (Exception ex)
        {
            Alert.Exception(ex);
        }
    }

    public async void CreateJob(JobOptions options) => await vm.CreateJobAsync(options);

    private void ViewJob(object sender, MouseButtonEventArgs e)
    {
        if (sender is WrapPanel { Tag: JobViewModel job })
        {
            SP.GetService<MainWindow>().DisplayJob(job);
        }
    }
}
