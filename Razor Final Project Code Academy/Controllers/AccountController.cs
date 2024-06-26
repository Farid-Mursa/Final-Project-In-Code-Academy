﻿using System;
using Razor_Final_Project_Code_Academy.Entities;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Razor_Final_Project_Code_Academy.ViewModel;
using Razor_Final_Project_Code_Academy.ViewModel.Roles;

namespace Final_Project_Razor.Controllers
{
	public class AccountController:Controller
	{
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, RoleManager<IdentityRole> roleManager)
		{
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }


		public IActionResult Register()
		{
			return View();
		}

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM account)
        {
            if (!ModelState.IsValid) return View();
            User user = new()
            {
                Fullname = string.Concat(account.FirstName, " ", account.LastName),
                Email = account.Email,
                UserName = account.UserName
            };
            if (_userManager.Users.Any(x => x.NormalizedEmail == account.Email.ToUpper()))
            {
                ModelState.AddModelError("Email", "Bu e-poçtda istifadəçi mövcuddur");
                return View();
            }
            IdentityResult result = await _userManager.CreateAsync(user, account.Password);
            if (!result.Succeeded)
            {
                foreach (IdentityError message in result.Errors)
                {
                    ModelState.AddModelError("", message.Description);
                }
                return  View();
            }

            await _userManager.AddToRoleAsync(user, Roles.User.ToString());

            string token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            string link = Url.Action(nameof(VerifyEmail), "Account", new { email = user.Email, token }, Request.Scheme, Request.Host.ToString());
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress("razor.familysites@gmail.com", "Welcome To Razor!");
            mailMessage.To.Add(new MailAddress(user.Email));
            mailMessage.Subject = "Verify Email";
            mailMessage.Body = string.Empty;
            string body = string.Empty;
            using (StreamReader reader = new StreamReader("wwwroot/assets/template/verifyemail.html"))
            {
                body = reader.ReadToEnd();
            }
            body = body.Replace("{{userFullName}}", user.Fullname);
            mailMessage.Body = body.Replace("{{link}}", link);
            mailMessage.IsBodyHtml = true;
            SmtpClient smtp = new SmtpClient();
            smtp.Host = "smtp.gmail.com";
            smtp.Port = 587;
            smtp.EnableSsl = true;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential("razor.familysites@gmail.com", "lrzxazlzxwtshywn");
            smtp.Send(mailMessage);
            return RedirectToAction("ConfirmRegister", "Welcome");

        }

        public async Task<IActionResult> VerifyEmail(string email, string token)
        {
            User user = await _userManager.FindByEmailAsync(email);
            if (user == null) return BadRequest();
            await _userManager.ConfirmEmailAsync(user, token);
            await _signInManager.SignInAsync(user, true);
            return RedirectToAction("Index", "Welcome");
        }

        public IActionResult Login()
		{
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
		}

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM login)
        {
            if (!ModelState.IsValid) return View();

            User user = await _userManager.FindByNameAsync(login.Username);
            if (user is null)
            {
                ModelState.AddModelError("", "Username or password is incorrect");
                return View();
            }
            IList<string> roles = await _userManager.GetRolesAsync(user);
          
            Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(user, login.Password, login.RememberMe, true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    ModelState.AddModelError("", "you blocked for 5 minutes");
                    return View();
                }
                ModelState.AddModelError("", "Username or password is incorrect");
                return View();
            }
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> LogOut()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Forgot()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Forgot(ForgotVM account)
        {
            if (account.User.Email is null) return View() ;
            User user = await _userManager.FindByEmailAsync(account.User.Email);
            if (user is null) return View();
            string token = await _userManager.GeneratePasswordResetTokenAsync(user);
            string link = Url.Action(nameof(RestartPassword), "Account", new { email = user.Email, token }, Request.Scheme, Request.Host.ToString());
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("razor.familysites@gmail.com", "Welcome To Razor!");
            mail.To.Add(new MailAddress(user.Email));

            mail.Subject = "Reset Password";
            string body = string.Empty;
            using (StreamReader reader = new StreamReader("wwwroot/assets/template/ForgotPassword.html"))
            {
                body = reader.ReadToEnd();
            }
            body = body.Replace("{{userFullName}}", user.Fullname);
            mail.Body = body.Replace("{{link}}", link);
            mail.IsBodyHtml = true;
            SmtpClient smtp = new SmtpClient();
            smtp.Host = "smtp.gmail.com";
            smtp.Port = 587;
            smtp.EnableSsl = true;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential("razor.familysites@gmail.com", "lrzxazlzxwtshywn");
            smtp.Send(mail);
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> RestartPassword(string email, string token)
        {
            User user = await _userManager.FindByEmailAsync(email);
            if (user == null) BadRequest();
            ForgotVM model = new()
            {
                User = user,
                Token = token
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestartPassword(ForgotVM forgot)
        {
            User user = await _userManager.FindByEmailAsync(forgot.User.Email);
            ForgotVM model = new()
            {
                User = user,
                Token = forgot.Token
            };
            await _userManager.ResetPasswordAsync(user, forgot.Token, forgot.Password);
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> AccountDetails()
		{
			User user = await _userManager.FindByNameAsync(User.Identity.Name);
			if (user is null)
			{
				return RedirectToAction(nameof(Login));
			}
			AccountSettingVM accountSettingVM = new()
			{
				Email = user.Email,
				UserName = user.UserName,
			};

			return View(accountSettingVM);
		}

        [HttpPost]
        public async Task<IActionResult> AccountDetails(AccountSettingVM accountSettingVM)
        {
            if (!ModelState.IsValid) return View();

            User member = await _userManager.FindByNameAsync(User.Identity.Name);

            if (!string.IsNullOrWhiteSpace(accountSettingVM.ConfirmNewPassword) && !string.IsNullOrWhiteSpace(accountSettingVM.NewPassword))
            {
                var passwordChangeResult = await _userManager.ChangePasswordAsync(member, accountSettingVM.CurrentPassword, accountSettingVM.NewPassword);

                if (!passwordChangeResult.Succeeded)
                {
                    foreach (var item in passwordChangeResult.Errors)
                    {
                        ModelState.AddModelError("", item.Description);
                    }

                    return View();
                }
            }
            if (member.Email != accountSettingVM.Email && _userManager.Users.Any(x => x.NormalizedEmail == accountSettingVM.Email.ToUpper()))
            {
                ModelState.AddModelError("Email", "This email has already been taken");
                return View();
            }
            member.Email = accountSettingVM.Email;
            member.UserName = accountSettingVM.UserName;
            var result = await _userManager.UpdateAsync(member);
            if (!result.Succeeded)
            {
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError("", item.Description);
                }
                return View();
            }
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }


        //public async Task CreateRoles()
        //{
        //    await _roleManager.CreateAsync(new IdentityRole(Roles.Admin.ToString()));
        //    await _roleManager.CreateAsync(new IdentityRole(Roles.User.ToString()));
        //    await _roleManager.CreateAsync(new IdentityRole(Roles.SuperAdmin.ToString()));
        //}

    }
}

