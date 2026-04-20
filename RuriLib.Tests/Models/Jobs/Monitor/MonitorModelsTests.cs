using RuriLib.Models.Conditions.Comparisons;
using RuriLib.Models.Jobs;
using RuriLib.Models.Jobs.Monitor;
using RuriLib.Models.Jobs.Monitor.Actions;
using RuriLib.Models.Jobs.Monitor.Triggers;
using RuriLib.Services;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace RuriLib.Tests.Models.Jobs.Monitor;

public class MonitorModelsTests
{
    [Fact]
    public async Task TriggeredAction_WhenJobMissing_DoesNothing()
    {
        var action = new TriggeredAction
        {
            JobId = 123,
            Triggers = [],
            Actions = []
        };

        await action.CheckAndExecute([]);

        Assert.False(action.IsExecuting);
        Assert.Equal(0, action.Executions);
    }

    [Fact]
    public async Task MultiRunJobAction_WhenTargetIsNotMultiRunJob_ThrowsInvalidOperationException()
    {
        var action = new SetBotsAction
        {
            TargetJobId = 1,
            Amount = 5
        };
        var jobs = new Job[] { new TestJob(CreateSettingsService(), CreatePluginRepository()) { Id = 1 } };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => action.Execute(1, jobs));

        Assert.Equal("The job with id 1 is not a MultiRunJob", exception.Message);
    }

    [Fact]
    public void MultiRunJobTrigger_WhenJobIsNotMultiRunJob_ThrowsInvalidOperationException()
    {
        var trigger = new TestedCountTrigger
        {
            Comparison = NumComparison.GreaterThanOrEqualTo,
            Amount = 0
        };
        var job = new TestJob(CreateSettingsService(), CreatePluginRepository());

        var exception = Assert.Throws<InvalidOperationException>(() => trigger.CheckStatus(job));

        Assert.Equal("The job must be a MultiRunJob", exception.Message);
    }

    [Fact]
    public async Task TriggeredAction_WhenTriggersMatch_ExecutesActions()
    {
        var job = new TestMultiRunJob(CreateSettingsService(), CreatePluginRepository())
        {
            Id = 7
        };
        job.SetStatus(JobStatus.Running);
        var action = new TriggeredAction
        {
            JobId = 7,
            Triggers = [new JobStatusTrigger { Status = JobStatus.Running }],
            Actions = [new SetBotsAction { TargetJobId = 7, Amount = 4 }]
        };

        await action.CheckAndExecute([job]);

        Assert.Equal(1, action.Executions);
        Assert.Equal(4, job.Bots);
        Assert.False(action.IsExecuting);
    }

    private static RuriLibSettingsService CreateSettingsService()
        => new(Path.Combine(Path.GetTempPath(), $"ob2-monitor-tests-{Guid.NewGuid():N}"));

    private static PluginRepository CreatePluginRepository()
        => (PluginRepository)RuntimeHelpers.GetUninitializedObject(typeof(PluginRepository));

    private sealed class TestJob(RuriLibSettingsService settings, PluginRepository pluginRepo) : Job(settings, pluginRepo)
    {
    }

    private sealed class TestMultiRunJob(RuriLibSettingsService settings, PluginRepository pluginRepo)
        : MultiRunJob(settings, pluginRepo)
    {
        public void SetStatus(JobStatus status) => Status = status;
    }
}
