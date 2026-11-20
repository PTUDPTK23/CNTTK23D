using Microsoft.AspNetCore.SignalR;

namespace EduSubmit.Api.Hubs;

public class SubmissionHub : Hub
{
    public async Task JoinClass(string classId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, classId);
        await Clients.Caller.SendAsync("JoinedClass", $"Da tham gia kenh theo doi lop: {classId}");
    }

    public async Task LeaveClass(string classId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, classId);
    }
}