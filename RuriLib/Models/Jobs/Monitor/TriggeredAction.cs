using RuriLib.Models.Jobs.Monitor.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Action = RuriLib.Models.Jobs.Monitor.Actions.Action;

namespace RuriLib.Models.Jobs.Monitor;

// TODO: Add some log output to see errors or just activities that have been performed, like the cron jobs log
/// <summary>
/// Combines triggers and actions that run against a monitored job.
/// </summary>
public class TriggeredAction
{
    /// <summary>Gets the unique identifier.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    /// <summary>Gets the display name.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether the triggered action is active.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>Gets or sets a value indicating whether the triggered action is currently executing.</summary>
    public bool IsExecuting { get; set; }
    /// <summary>Gets or sets a value indicating whether the triggered action can execute multiple times.</summary>
    public bool IsRepeatable { get; set; }
    /// <summary>Gets or sets the number of executions.</summary>
    public int Executions { get; set; }

    // The job this triggered action refers to
    /// <summary>Gets or sets the identifier of the monitored job.</summary>
    public int JobId { get; set; }

    // All triggers must be verified at the same time
    /// <summary>Gets the trigger list.</summary>
    public List<Trigger> Triggers { get; init; } = [];

    // Actions are executed sequentially, so stop - delay - start is possible
    /// <summary>Gets the action list.</summary>
    public List<Action> Actions { get; init; } = [];

    // Fire and forget
    /// <summary>
    /// Checks the triggers and executes the actions when they all match.
    /// </summary>
    /// <param name="jobs">The available jobs.</param>
    /// <returns>A task that completes when the check finishes.</returns>
    public async Task CheckAndExecute(IEnumerable<Job> jobs)
    {
        var jobsArray = jobs as Job[] ?? jobs.ToArray();
        var job = jobsArray.FirstOrDefault(j => j.Id == JobId);

        if (job == null)
        {
            return;
        }

        try
        {
            // Check the status of triggers on the current job
            if (Triggers.All(t => t.CheckStatus(job)))
            {
                Executions++;
                IsExecuting = true;

                foreach (var action in Actions)
                {
                    // Try to execute action on current job or any of the other jobs
                    try
                    {
                        await action.Execute(JobId, jobsArray);
                    }
                    catch
                    {
                        // Something went bad with actions
                    }
                }
            }
        }
        catch
        {
            // Something went bad with triggers (maybe the job isn't there anymore)
        }
        finally
        {
            IsExecuting = false;
        }
    }

    /// <summary>
    /// Resets the execution state and counter.
    /// </summary>
    public void Reset()
    {
        IsExecuting = false;
        Executions = 0;
    }
}
