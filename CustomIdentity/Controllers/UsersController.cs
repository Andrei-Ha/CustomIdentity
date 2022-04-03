using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CustomIdentity.Models;
using CustomIdentity.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CustomIdentity.Controllers
{
    [Authorize(Roles = "admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly string _oraclePirr2n;
        private readonly string _mssqlPirr2n;
        private readonly IConfiguration _configuration;
        public UsersController(UserManager<User> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
            _oraclePirr2n = _configuration.GetConnectionString("OraclePirr2n");
            _mssqlPirr2n = _configuration.GetConnectionString("MSsqlPirr2n");
        }
        private List<SelList> ListPerson()
        {
            List<SelList> myList = new List<SelList>();
            using (OracleConnection con = new OracleConnection(_oraclePirr2n))
            {
                using (OracleCommand cmd = con.CreateCommand())
                {
                    con.Open();
                    cmd.CommandText = "select m_linom,fio,login from mail where m_linom!=777777 order by fio";
                    OracleDataReader reader = cmd.ExecuteReader();
                    using (reader)
                    {
                        while (reader.Read())
                        {
                            myList.Add(new SelList() { Id = reader["m_linom"].ToString(), Text = "" + reader["fio"].ToString() + " - " + reader["login"].ToString() + "@brestenergo.by" });
                        }
                    }
                }
            }
            /*using (SqlConnection con = new SqlConnection(_mssqlPirr2n))
            {
                using (SqlCommand cmd = con.CreateCommand())
                {
                    con.Open();
                    cmd.CommandText = "select m_linom,fio,login from dbo.mail where m_linom!=777777 order by fio";
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        myList.Add(new SelList() { Id = reader["m_linom"].ToString(), Text = "" + reader["fio"].ToString() + " - " + reader["login"].ToString() + "@brestenergo.by" });
                    }
                    reader.Dispose();
                }
            }*/
            return myList;
        }
        public async Task<IActionResult>Index()
        {
            List<IndexUser> indexUsers = new List<IndexUser>();
            string str_roles = string.Empty;
            var users = _userManager.Users.OrderBy(p=>p.UserName).ToList();
            foreach (User user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                str_roles = "";
                foreach (string role in roles.OrderBy(p=>p))
                {
                    str_roles += role + ";<br />";
                }
                indexUsers.Add(new IndexUser()
                {
                    IndUser=user,
                    Roles = str_roles
                });

            }
            return View(indexUsers);
        }
        public IActionResult Create()
        {
            ViewData["Person"] = new SelectList(ListPerson(), "Id", "Text");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                User user = new User { Email = model.Email, UserName = model.UserName, Linom = model.Linom, EmailConfirmed=true };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            User user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            EditUserViewModel model = new EditUserViewModel { Id = user.Id, Email = user.Email, Linom = user.Linom };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                User user = await _userManager.FindByIdAsync(model.Id);
                if (user != null)
                {
                    user.Email = model.Email;
                    user.UserName = model.Email;
                    user.Linom = model.Linom;

                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Delete(string id)
        {
            User user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                IdentityResult result = await _userManager.DeleteAsync(user);
            }
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> ChangePassword(string id)
        {
            User user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            ChangePasswordViewModel model = new ChangePasswordViewModel { Id = user.Id, Email = user.Email };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                User user = await _userManager.FindByIdAsync(model.Id);
                if (user != null)
                {
                    var _passwordValidator =
                        HttpContext.RequestServices.GetService(typeof(IPasswordValidator<User>)) as IPasswordValidator<User>;
                    var _passwordHasher =
                        HttpContext.RequestServices.GetService(typeof(IPasswordHasher<User>)) as IPasswordHasher<User>;

                    IdentityResult result =
                        await _passwordValidator.ValidateAsync(_userManager, user, model.NewPassword);
                    if (result.Succeeded)
                    {
                        user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
                        await _userManager.UpdateAsync(user);
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Пользователь не найден");
                }
            }
            return View(model);
        }
    }
}