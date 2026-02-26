namespace UEAT.Notification.Core.Email;

public record EmailMessage(string To, string Subject, string Content);