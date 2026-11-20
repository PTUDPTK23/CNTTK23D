using Microsoft.AspNetCore.SignalR;

public sealed class SubmissionHub : Hub
{
    public Task JoinSubmissionGroup(Guid submissionId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, GroupName(submissionId));
    }

    public static string GroupName(Guid submissionId) => $"submission:{submissionId:D}";
}
