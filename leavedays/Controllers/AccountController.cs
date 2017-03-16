﻿using leavedays.Models;
using leavedays.Models.ViewModels.Account;
using leavedays.Services;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace leavedays.Controllers
{

    public class AccountController : Controller
    {
        private readonly CompanyService companyService;

        private readonly UserManager<AppUser, int> userManager;
        private readonly SignInManager<AppUser, int> signInManager;
        public AccountController(
            UserManager<AppUser, int> userManager,
            SignInManager<AppUser, int> signInManager,
            CompanyService companyService)
        {
            this.companyService = companyService;
            this.userManager = userManager;
            this.signInManager = signInManager;
        }



        public async Task<ActionResult> CreateCustomer()
        {

            var user = new AppUser() { UserName = "dimas", Password = "dimas123" };
            var result = await userManager.CreateAsync(user, "dimas123");
            if (result.Succeeded)
            {
                await signInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return Content(result.Succeeded.ToString());


        }


        public ActionResult AddTo(string role)
        {
            userManager.AddToRole(1, role);
            return Content(userManager.IsInRole(1, role).ToString());
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl = "")
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var user = new AppUser { UserName = model.UserName, Password = model.Password };
            var result = await signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    {
                        if (string.IsNullOrEmpty(returnUrl) || returnUrl == "/") return Content("OK");
                        return Redirect(returnUrl);
                    }
                //case SignInStatus.LockedOut:
                //    return View("Lockout");
                //case SignInStatus.RequiresVerification:
                //    return RedirectToAction("SendCode", new { ReturnUrl = "", RememberMe = model.RememberMe });
                //case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }


        // [Authorize(Roles="Customer")]
        [HttpGet]
        [Authorize(Roles = "Customer")]
        public ActionResult CreateEmployee()
        {
            var model = new CreateEmployeeViewModel();
            model.Roles = new string[] { "Worker", "Manager" };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult> CreateEmployee(CreateEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Password = "";
                return View(model);
            }
            var companyId = companyService.GetUserById(User.Identity.GetUserId<int>()).CompanyId;

            var user = new AppUser() { UserName = model.UserName, Roles = ",customer" + companyService.GetRolesFromLine(model.RolesLine), CompanyId = companyId };
            var result = await userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Error while creating new customer");
                return View(model);
            }
            return Content(result.Succeeded.ToString());
        }

        [HttpGet]
        [Authorize(Roles = "FinanceAdmin")]
        public ActionResult CreateCompany()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "FinanceAdmin")]
        public async Task<ActionResult> CreateCompany(CreateCompanyViewModel model)
        {
            if (!ModelState.IsValid) return View(model);


            var company = new Company()
            {
                FullName = model.CompanyName,
                UrlName = string.Join("", model.CompanyName.Split(',', '.', ' ', '_')),

            };
            var companyId = companyService.SaveCompany(company);

            var user = new AppUser() { UserName = model.UserName, Roles = ",customer" + companyService.GetRolesFromLine(model.RolesLine), CompanyId = companyId };
            var result = await userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Error while creating new customer");
                return View(model);
            }
          
            return Content(result.Succeeded.ToString());
        }
    }
}