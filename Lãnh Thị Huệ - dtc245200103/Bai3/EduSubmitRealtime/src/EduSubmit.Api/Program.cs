using EduSubmit.Api.Hubs;
using EduSubmit.Contracts;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

var submissions = new List<SubmissionNotification>();

app.MapGet("/", () => Results.Ok(new { service = "EduSubmitApi", status = "running" }));

app.MapGet("/api/submissions/{classId}", (string classId) =>
{
    var result = submissions.Where(s => s.ClassId == classId).ToList();
    return Results.Ok(result);
});

app.MapPost("/api/submissions", async (SubmitRequest req, IHubContext<SubmissionHub> hubContext) =>
{
    var notification = new SubmissionNotification(
        SubmissionId: Guid.NewGuid().ToString("N")[..8].ToUpper(),
        StudentId: req.StudentId,
        StudentName: req.StudentName,
        ClassId: req.ClassId,
        AssignmentTitle: req.AssignmentTitle,
        FileName: req.FileName,
        SubmittedAt: DateTime.Now,
        Status: "Da nop thanh cong"
    );

    submissions.Add(notification);

    // Đẩy thông báo realtime tới đúng nhóm lớp học đang theo dõi
    await hubContext.Clients.Group(req.ClassId)
        .SendAsync("NewSubmission", notification);

    return Results.Created($"/api/submissions/{notification.SubmissionId}", notification);
});

app.MapHub<SubmissionHub>("/hubs/submissions");

app.Run();