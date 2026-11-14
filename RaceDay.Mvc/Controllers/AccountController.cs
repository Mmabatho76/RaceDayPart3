using Microsoft.AspNetCore.Mvc;
public class AccountController:BaseController { private readonly RaceDayApiClient _api; public AccountController(RaceDayApiClient api)=>_api=api;
 [HttpGet] public IActionResult Login()=>View(new LoginVm());
 [HttpPost] public async Task<IActionResult> Login(LoginVm vm){if(!ModelState.IsValid)return View(vm);var (u,e)=await _api.Login(vm.Email,vm.Password);if(u==null){ModelState.AddModelError("",e);return View(vm);}SetUser(u);return RedirectToAction("Index",u.Role=="Organiser"?"Organiser":"Participant");}
 [HttpGet] public IActionResult Register()=>View(new RegisterVm());
 [HttpPost] public async Task<IActionResult> Register(RegisterVm vm){if(!ModelState.IsValid)return View(vm);var (u,e,ok)=await _api.Send<UserDto>(HttpMethod.Post,"api/auth/register",vm);if(!ok){ModelState.AddModelError("",e);return View(vm);}TempData["Success"]="Registration successful. Please log in.";return RedirectToAction(nameof(Login));}
 public async Task<IActionResult> Logout(){if(SignedIn)await _api.Send<object>(HttpMethod.Post,"api/auth/logout");HttpContext.Session.Clear();return RedirectToAction("Index","Home");}
}
