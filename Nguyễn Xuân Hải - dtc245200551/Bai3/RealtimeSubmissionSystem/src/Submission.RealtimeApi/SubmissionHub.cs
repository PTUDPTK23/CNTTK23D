using Microsoft.AspNetCore.SignalR;

public sealed class SubmissionHub : Hub
{
    public Task WatchSubmission(Guid submissionId)
    {
        var groupName = $"submission:{submissionId}";
        return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }
}
