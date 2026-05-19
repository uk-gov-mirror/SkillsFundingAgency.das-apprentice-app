using SFA.DAS.ApprenticeApp.Domain.Models;

namespace SFA.DAS.ApprenticeApp.Pwa.ViewModels
{
    public class NotificationPageModel
    { 
        public List<ApprenticeTaskReminder> TaskReminders { get; set; } = new();
        public List<LearnerNotificationViewModel> LearnerNotifications { get; set; } = new();
        public bool SurveyNotificationSeen { get; set; }
        public bool HasLearnerNotificationError { get; set; }
        public bool HasNotifications =>
            TaskReminders.Any() ||
            LearnerNotifications.Any() ||
            !SurveyNotificationSeen;
    }  
}