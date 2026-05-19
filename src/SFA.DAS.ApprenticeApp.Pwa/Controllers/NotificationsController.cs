using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.ApprenticeApp.Application;
using SFA.DAS.ApprenticeApp.Domain.Interfaces;
using SFA.DAS.ApprenticeApp.Domain.Models;
using SFA.DAS.ApprenticeApp.Pwa.Helpers;
using SFA.DAS.ApprenticeApp.Pwa.ViewModels;

namespace SFA.DAS.ApprenticeApp.Pwa.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly ILogger<NotificationsController> _logger;
        private readonly IOuterApiClient _client;
        private readonly IApprenticeContext _apprenticeContext;
        private const string LearnerNotificationsSeenCookie = "SFA.DAS.ApprenticeApp.LearnerNotificationsSeen";

        public NotificationsController(
                    ILogger<NotificationsController> logger,
                    IOuterApiClient client,
                    IApprenticeContext apprenticeContext
                    )
        {
            _logger = logger;
            _client = client;
            _apprenticeContext = apprenticeContext;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var apprenticeId = _apprenticeContext.ApprenticeId;

            if (!string.IsNullOrEmpty(apprenticeId))
            {
                try
                {
                    var surveryCookie = Request.Cookies["SFA.DAS.ApprenticeApp.SurveyNotificationSeen"];
                    var surveryCookieValue = 0;
                    if (surveryCookie != null)
                    {
                        surveryCookieValue = int.Parse(Request.Cookies["SFA.DAS.ApprenticeApp.SurveyNotificationSeen"]);
                    }                    
                    
                    var notificationsResult = await _client.GetTaskReminderNotifications(new Guid(apprenticeId));
                    var learnerNotificationsSeen = Request.Cookies[LearnerNotificationsSeenCookie] != null;
                    var vm = new NotificationPageModel
                    {
                        TaskReminders = notificationsResult.TaskReminders,
                        SurveyNotificationSeen = Convert.ToBoolean(surveryCookieValue),
                        LearnerNotifications = new List<LearnerNotificationViewModel>
                        {
                            new LearnerNotificationViewModel
                            {
                                NotificationId = 1,
                                Heading = "Evidence submission due",
                                Body = "Your evidence submission is due tomorrow.",
                                Category = "Tasks",
                                Urgency = "High",
                                StatusId = (byte)LearnerNotificationStatus.Unread,
                                StatusName = "Unread",
                                NotificationTime = DateTime.UtcNow,
                                TimeReceived = DateTime.UtcNow,
                                TimeToExpire = DateTime.UtcNow.AddMonths(3),
                                IsNew = !learnerNotificationsSeen,
                                Link = "/Notifications"
                            },
                            new LearnerNotificationViewModel
                            {
                                NotificationId = 2,
                                Heading = "Workshop booking confirmed",
                                Body = "You are booked onto the safeguarding workshop.",
                                Category = "Training",
                                Urgency = "Low",
                                StatusId = (byte)LearnerNotificationStatus.Unread,
                                StatusName = "Unread",
                                NotificationTime = DateTime.UtcNow.AddHours(-3),
                                TimeReceived = DateTime.UtcNow.AddHours(-3),
                                TimeToExpire = DateTime.UtcNow.AddMonths(3),
                                IsNew = !learnerNotificationsSeen,
                                Link = null
                            }
                        }
                    };
                    return View(vm);
                }
                catch (Exception)
                {
                    _logger.LogWarning("Error in Notifications: GetTaskReminderNotifications");
                } 
            }
            else
            {
                _logger.LogWarning("ApprenticeId not found in user claims for Notifications Index.");
            }
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ConfirmDeleteNotification(int taskId)
        {
            var apprenticeId = _apprenticeContext.ApprenticeId;

            if (!string.IsNullOrEmpty(apprenticeId))
            {
                try
                {
                    var notificationsResult = await _client.GetTaskReminderNotifications(new Guid(apprenticeId));
                    var notification = notificationsResult.TaskReminders?.FirstOrDefault(r => r.TaskId == taskId);
                    if (notification != null)
                    {
                        return View(notification);
                    }
                }
                catch (Exception)
                {
                    _logger.LogWarning("Error in Notifications: ConfirmDeleteNotification");
                }
            }
            else
            {
                _logger.LogWarning("ApprenticeId not found in user claims for Notifications ConfirmDeleteNotification.");
            }
            return RedirectToAction("Index");
        }
        [Authorize]
        [HttpGet]
        public IActionResult ConfirmDeleteLearnerNotification(long notificationId)
        {
            var notification = new LearnerNotificationViewModel
            {
                NotificationId = notificationId
            };

            return View(notification);
        }

        [Authorize]
        [HttpGet]
        public IActionResult ConfirmDeleteSurveyNotification()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public IActionResult DeleteSurveyNotification()
        {
            Response.Cookies.Append(
                "SFA.DAS.ApprenticeApp.SurveyNotificationSeen",
                "1",
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteNotification(int taskId)
        {
            var apprenticeId = _apprenticeContext.ApprenticeId;

            if (!string.IsNullOrEmpty(apprenticeId))
            {
                try
                {
                    _logger.LogInformation("Updating reminder notification for {taskId} to dismissed", taskId);
                    await _client.UpdateTaskReminderStatus(new Guid(apprenticeId), taskId, (int)ReminderStatus.Dismissed );
                }
                catch (Exception)
                {
                    _logger.LogWarning("Error in Notifications: DeleteTaskReminderNotification");
                }
            }
            else
            {
                _logger.LogWarning("ApprenticeId not found in user claims for Notifications DeleteNotification.");
            }
            return RedirectToAction("Index");
        }
        [Authorize]
        [HttpPost]
        public IActionResult DeleteLearnerNotification(long notificationId)
        {
         // temporary behaviour until backend API is ready
           return RedirectToAction("Index");
        }
        [Authorize]
        [HttpPost]
        public IActionResult AcknowledgeLearnerNotification(long notificationId)
        {
            // temporary behaviour until backend API is ready
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        public IActionResult HideLearnerNotification(long notificationId)
        {
            // temporary behaviour until backend API is ready
            return RedirectToAction("Index");
        }
        [Authorize]
        [HttpGet]
        public IActionResult NoNotifications()
        {
            return PartialView("_NoNotifications");
        }
        [Authorize]
        [HttpPost]
        public IActionResult MarkLearnerNotificationsSeen()
        {
            Response.Cookies.Append(
                LearnerNotificationsSeenCookie,
                "1",
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

            return Ok();
        }
    }
}
