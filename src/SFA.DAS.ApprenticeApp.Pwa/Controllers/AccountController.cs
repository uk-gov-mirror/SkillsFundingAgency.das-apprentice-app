using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.ApprenticeApp.Application;
using SFA.DAS.ApprenticeApp.Domain.Interfaces;
using SFA.DAS.ApprenticeApp.Domain.Models;
using SFA.DAS.ApprenticeApp.Pwa.Configuration;
using SFA.DAS.ApprenticeApp.Pwa.Helpers;
using SFA.DAS.ApprenticeApp.Pwa.Models;
using SFA.DAS.ApprenticeApp.Pwa.ViewModels;
using SFA.DAS.ApprenticeApp.Pwa.Services;
using SFA.DAS.GovUK.Auth.Services;
using System.Security.Claims;

namespace SFA.DAS.ApprenticeApp.Pwa.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IStubAuthenticationService _stubAuthenticationService;
        private readonly ICommitmentsService _commitmentsService;
        private readonly IConfiguration _config;
        public static ApplicationConfiguration _appConfig { get; set; }
        private readonly IOuterApiClient _client;
        private readonly IApprenticeContext _apprenticeContext;
        
        public AccountController(ILogger<AccountController> logger,
            IStubAuthenticationService stubAuthenticationService,
            ICommitmentsService commitmentsService,
            ApplicationConfiguration appConfig,
            IConfiguration configuration,
            IOuterApiClient client,
            IApprenticeContext apprenticeContext
        )
        {
            _logger = logger;
            _stubAuthenticationService = stubAuthenticationService;
            _commitmentsService = commitmentsService;
            _appConfig = appConfig;
            _config = configuration;
            _client = client;
            _apprenticeContext = apprenticeContext;
        }
        
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Authenticated()
        {
            var authenticatedApprenticeId = _apprenticeContext.ApprenticeId;
            if (string.IsNullOrWhiteSpace(authenticatedApprenticeId)) return RedirectToAction("AccountNotFound", "Account");

            if (!Guid.TryParse(authenticatedApprenticeId, out var apprenticeId))
            {
                return RedirectToAction("AccountNotFound", "Account");
            }

            try
            {
                var apprenticeDetails = await _client.GetApprenticeDetails(apprenticeId);

                if (apprenticeDetails == null)
                {
                    return RedirectToAction("AccountNotFound", "Account");
                }
                
                // Check terms
                if (apprenticeDetails.Apprentice.TermsOfUseAccepted == false) return RedirectToAction("Index", "Terms");

                // Check if cmad completed
                var nextStep = await _commitmentsService.HandleConfirmationStatus(apprenticeDetails, apprenticeId);

                if (!string.IsNullOrEmpty(nextStep.ConfirmModelJson))
                {
                    TempData["ConfirmModel"] = nextStep.ConfirmModelJson;
                }

                return nextStep.NavigationType switch
                {
                    CmadNavigationType.WelcomeIndex => RedirectToAction("Index", "Welcome"),

                    CmadNavigationType.ConfirmApprenticeshipDetails => RedirectToAction("ConfirmApprenticeshipDetails", "Cmad"),

                    // Default to ConfirmDetils for any other cases
                    _ => RedirectToAction("ConfirmDetails", "Cmad")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MyApprenticeship data error or not found for {ApprenticeId}", apprenticeId);
                return RedirectToAction("AccountNotFound", "Account");
            }
        }

        [HttpGet]
        [Route("account-details", Name = RouteNames.StubAccountDetailsGet)]
        public IActionResult AccountDetails([FromQuery] string returnUrl)
        {
            if (_config["ResourceEnvironmentName"].ToUpper() == "PRD")
            {
                return NotFound();
            }

            return View("AccountDetails", new StubAuthenticationViewModel
            {
                ReturnUrl = returnUrl
            });
        }

        [HttpPost]
        [Route("account-details", Name = RouteNames.StubAccountDetailsPost)]
        public async Task<IActionResult> AccountDetails(StubAuthenticationViewModel model)
        {
            if (_config["ResourceEnvironmentName"].ToUpper() == "PRD")
            {
                return NotFound();
            }

            try
            {                
                model.Email = model.Email.ToLower();
                var claims = await _stubAuthenticationService.GetStubSignInClaims(model);
                var apprenticeId = claims?.Claims?.First(c => c.Type == Constants.ApprenticeIdClaimKey)?.Value;

                // Set extended cookie expiration here
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMonths(1),
                    AllowRefresh = true
                };

                ApprenticeDetails apprenticeDetails = null;
                if (!string.IsNullOrEmpty(apprenticeId))
                {
                    apprenticeDetails = await _client.GetApprenticeDetails(new Guid(apprenticeId));
                }

                if (apprenticeDetails?.MyApprenticeship?.Title != null)
                {
                    var identity = claims.Identities.First();
                    identity.AddClaim(new Claim(Constants.ApprenticeshipTitleClaimKey, apprenticeDetails.MyApprenticeship.Title));
                }
                else
                {
                    // Optional fallback for local testing if no title exists
                    var identity = claims.Identities.First();
                    identity.AddClaim(new Claim(Constants.ApprenticeshipTitleClaimKey, "Test Apprenticeship Title"));
                }

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    claims,
                    authProperties);

                if (apprenticeDetails == null)
                {
                    return RedirectToAction("AccountNotFound", "Account");
                }
                
                // Check terms
                if (apprenticeDetails.Apprentice.TermsOfUseAccepted == false) return RedirectToAction("Index", "Terms");

                // Check if cmad completed
                var nextStep = await _commitmentsService.HandleConfirmationStatus(apprenticeDetails, Guid.Parse(apprenticeId));

                if (!string.IsNullOrEmpty(nextStep.ConfirmModelJson))
                {
                    TempData["ConfirmModel"] = nextStep.ConfirmModelJson;
                }

                return nextStep.NavigationType switch
                {
                    CmadNavigationType.WelcomeIndex => RedirectToAction("Index", "Welcome"),
                    CmadNavigationType.ConfirmApprenticeshipDetails => RedirectToAction("ConfirmApprenticeshipDetails", "Cmad"),
                    _ => RedirectToAction("ConfirmDetails", "Cmad")
                };
            }
            catch (Exception)
            {
                return RedirectToAction("Error", "Account");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SigningOut()
        {
            var idToken = await HttpContext.GetTokenAsync("id_token");

            var authenticationProperties = new AuthenticationProperties();
            authenticationProperties.Parameters.Clear();
            authenticationProperties.Parameters.Add("id_token", idToken);

            var schemes = new List<string>
        {
            CookieAuthenticationDefaults.AuthenticationScheme
        };
            _ = bool.TryParse(_appConfig.StubAuth, out var stubAuth);
            if (!stubAuth)
            {
                schemes.Add(OpenIdConnectDefaults.AuthenticationScheme);
            }

            return SignOut(
                authenticationProperties,
                schemes.ToArray());
        }

        [HttpGet]
        [Authorize]
        [Route("Stub-Auth", Name = RouteNames.StubSignedIn)]
        public async Task<IActionResult> StubSignedIn()
        {
            if (_config["ResourceEnvironmentName"].ToUpper() == "PRD")
            {
                return NotFound();
            }

            var viewModel = new StubAuthenticationViewModel
            {
                Email = User.Claims.FirstOrDefault(c => c.Type.Equals(ClaimTypes.Email))?.Value.ToLower(),
                Id = User.Claims.FirstOrDefault(c => c.Type.Equals(ClaimTypes.NameIdentifier))?.Value
            };
            
            return RedirectToAction("Index", "Terms");
        }

        [HttpGet]
        public IActionResult AccountNotFound()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Error()
        {
            return View();
        }

        [HttpGet]
        public IActionResult CmadError()
        {
            return View();
        }

        [HttpGet]
        public IActionResult EmailMismatchError()
        {
            return View();
        }
        
        [HttpGet]
        public async Task<IActionResult> YourAccount()
        {
            var apprenticeId = _apprenticeContext.ApprenticeId;

            var apprenticeAccountModel = new ApprenticeAccountModel();
            var apprenticeKsbsPageModel = new ApprenticeKsbsPageModel();


            if (!string.IsNullOrEmpty(apprenticeId))
            {
                var apprenticeKsbResult = await _client.GetApprenticeshipKsbs(new Guid(apprenticeId));
                var allKsbs = apprenticeKsbResult?.ToList() ?? new List<ApprenticeKsb>();

                if (Request.Cookies[Constants.KsbFiltersCookieName] != null)
                {
                    var filterKsbs = Filter.FilterKsbResults(
                        apprenticeKsbResult,
                        Request.Cookies[Constants.KsbFiltersCookieName]);

                    if (filterKsbs.HasFilterRun)
                    {
                        apprenticeKsbResult = filterKsbs.FilteredKsbs;
                    }
                }

                var apprenticeDetails = await _client.GetApprenticeDetails(new Guid(apprenticeId));

                apprenticeKsbsPageModel.AllKsbs = allKsbs;
                apprenticeKsbsPageModel.Ksbs = apprenticeKsbResult;
                apprenticeKsbsPageModel.KnowledgeCount = apprenticeKsbResult?.Count(k => k.Type == KsbType.Knowledge);
                apprenticeKsbsPageModel.SkillCount = apprenticeKsbResult?.Count(k => k.Type == KsbType.Skill);
                apprenticeKsbsPageModel.BehaviourCount = apprenticeKsbResult?.Count(k => k.Type == KsbType.Behaviour);
                apprenticeKsbsPageModel.SearchTerm = null;
                apprenticeKsbsPageModel.MyApprenticeship = apprenticeDetails?.MyApprenticeship;
            }

            apprenticeAccountModel.apprenticeKsbsPageModel = apprenticeKsbsPageModel;

            return View(apprenticeAccountModel);
        }
    }
}