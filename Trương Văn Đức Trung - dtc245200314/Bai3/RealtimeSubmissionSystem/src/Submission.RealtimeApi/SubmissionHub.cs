using Microsoft.AspNetCore.SignalR;

sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(string submissionId)
    {
        if (!Guid.TryParse(submissionId, out var id))
            throw new HubException("SubmissionId khong hop le.");

        var group = SubmissionGroups.ForSubmission(id);
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        Console.WriteLine($"[Hub] {Context.ConnectionId} theo doi {group}");
    }
}
