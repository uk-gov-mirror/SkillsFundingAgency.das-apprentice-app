namespace SFA.DAS.ApprenticeApp.Pwa.ViewModels
{
    public class LearnerNotificationViewModel
    {
    public long NotificationId { get; set; }
    public Guid? CorrelationId { get; set; }
    public Guid? LearnerAccountId { get; set; }
    public string? Category { get; set; }
    public string? Heading { get; set; }
    public string? Body { get; set; }
    public byte? StatusId { get; set; }
    public string? StatusName { get; set; }
    public string? Urgency { get; set; }
    public DateTime? NotificationTime { get; set; }
    public DateTime? TimeReceived { get; set; }
    public DateTime? TimeToExpire { get; set; }
    public string? Link { get; set; }
    public bool IsNew { get; set; }
    }
}
