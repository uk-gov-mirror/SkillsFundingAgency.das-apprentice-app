using Microsoft.AspNetCore.Mvc;
using SFA.DAS.ApprenticeApp.Application;
using SFA.DAS.ApprenticeApp.Domain.Interfaces;
using SFA.DAS.ApprenticeApp.Pwa.Helpers;

namespace SFA.DAS.ApprenticeApp.Pwa.ViewComponents
{
    public class NotificationCounter : ViewComponent
    {
        private readonly IOuterApiClient _client;
        private readonly IApprenticeContext _apprenticeContext;

        public NotificationCounter(IOuterApiClient client, IApprenticeContext apprenticeContext)
        {
            _client = client;
            _apprenticeContext = apprenticeContext;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var apprenticeId = _apprenticeContext.ApprenticeId;
            if (!string.IsNullOrEmpty(apprenticeId))
            {
                var notificationValue = 0;
                
                var learnerNotificationsSeen = Request.Cookies["SFA.DAS.ApprenticeApp.LearnerNotificationsSeen"] != null;
                if (!learnerNotificationsSeen)
                {
                    notificationValue += 3;
                }
               
                return View("_NotificationCount", notificationValue);
                
            }
            return Content(string.Empty);
        }
    }
}