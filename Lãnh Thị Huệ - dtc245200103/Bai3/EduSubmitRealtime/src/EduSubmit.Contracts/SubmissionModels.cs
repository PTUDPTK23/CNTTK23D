namespace EduSubmit.Contracts;

public record SubmitRequest(
    string StudentId,
    string StudentName,
    string ClassId,
    string AssignmentTitle,
    string FileName
);

public record SubmissionNotification(
    string SubmissionId,
    string StudentId,
    string StudentName,
    string ClassId,
    string AssignmentTitle,
    string FileName,
    DateTime SubmittedAt,
    string Status
);