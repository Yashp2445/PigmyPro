using Microsoft.AspNetCore.Mvc;
using PigmyPro.Data.Interfaces;
using PigmyPro.Domain.Entities;
using PigmyPro.Web.ViewModels.Bank;
using Microsoft.AspNetCore.Authorization;
using PigmyPro.Domain;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace PigmyPro.Web.Controllers
{
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public class BankController : BaseController
    {
        private readonly IBankRepository _repo;
        private readonly IWebHostEnvironment _env;

        public BankController(IBankRepository repo, IWebHostEnvironment env)
        {
            _repo = repo;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _repo.GetAllAsync();

            var vm = data.Select(x => new BankListVM
            {
                BankID = x.BankID,
                Name = x.Name,
                ActiveYN = x.ActiveYN,
                HasCBS = x.hasCBS == 'Y',
                LogoFileName = x.LogoFileName
            }).ToList();

            return View(vm);
        }

        public IActionResult Create()
        {
            return View(new BankCreateEditVM
            {
                ActiveYN = true,
                IsPigmy = true
            });
        }

        private long GetCollectionGLCode(BankCreateEditVM vm)
        {
            if (!vm.IsPigmy)
                throw new Exception("Pigmy must always be selected.");

            if (!vm.IsLoan && !vm.IsRecurring)
                return 1;

            if (vm.IsLoan && !vm.IsRecurring)
                return 2;

            if (vm.IsLoan && vm.IsRecurring)
                return 3;

            throw new Exception("Invalid selection.");
        }

        private async Task<string?> HandleLogoUploadAsync(BankCreateEditVM vm, string? currentLogoFileName)
        {
            if (vm.RemoveLogo)
            {
                if (!string.IsNullOrEmpty(currentLogoFileName))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, "uploads", "bank-logos", currentLogoFileName);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }
                return null;
            }

            if (vm.LogoFile != null && vm.LogoFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(vm.LogoFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    throw new Exception("Only JPG, JPEG, and PNG images are allowed.");
                }

                if (vm.LogoFile.Length > 2 * 1024 * 1024)
                {
                    throw new Exception("File size must not exceed 2MB.");
                }

                if (!string.IsNullOrEmpty(currentLogoFileName))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, "uploads", "bank-logos", currentLogoFileName);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "bank-logos");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(vm.LogoFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await vm.LogoFile.CopyToAsync(stream);
                }

                return uniqueFileName;
            }

            return currentLogoFileName;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BankCreateEditVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            long code;
            string? logoFileName = null;
            try
            {
                code = GetCollectionGLCode(vm);
                logoFileName = await HandleLogoUploadAsync(vm, null);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(vm);
            }

            var entity = new Bank
            {
                Name = vm.Name,
                Address = vm.Address,
                ContactNo = vm.ContactNo,
                ContactPerson = vm.ContactPerson,
                EmailID = vm.EmailID,
                ActiveYN = vm.ActiveYN,
                CollectionGLCode = code,
                hasCBS = vm.HasCBS ? 'Y' : 'N',
                No_of_Holidays = vm.No_of_Holidays,
                LogoFileName = logoFileName,
                EntryDateTime = DateTime.Now
            };

            await _repo.AddAsync(entity);

            TempData["Success"] = "New bank registered successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data == null)
                return NotFound();

            long code = data.CollectionGLCode;

            var vm = new BankCreateEditVM
            {
                BankID = data.BankID,
                Name = data.Name,
                Address = data.Address,
                ContactNo = data.ContactNo,
                ContactPerson = data.ContactPerson,
                EmailID = data.EmailID,
                ActiveYN = data.ActiveYN,
                HasCBS = data.hasCBS == 'Y',
                No_of_Holidays = data.No_of_Holidays,
                ExistingLogoFileName = data.LogoFileName,

                IsPigmy = true,
                IsLoan = code >= 2,
                IsRecurring = code == 3,
                SelectedCollectionGLCode = code
            };

            return View("Create", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BankCreateEditVM vm)
        {
            if (!ModelState.IsValid)
                return View("Create", vm);

            long code;
            string? logoFileName = null;
            try
            {
                var existingBank = await _repo.GetByIdAsync(vm.BankID);
                if (existingBank == null) return NotFound();

                code = GetCollectionGLCode(vm);
                logoFileName = await HandleLogoUploadAsync(vm, existingBank.LogoFileName);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View("Create", vm);
            }

            var entity = new Bank
            {
                BankID = vm.BankID,
                Name = vm.Name,
                Address = vm.Address,
                ContactNo = vm.ContactNo,
                ContactPerson = vm.ContactPerson,
                EmailID = vm.EmailID,
                ActiveYN = vm.ActiveYN,
                CollectionGLCode = code,
                hasCBS = vm.HasCBS ? 'Y' : 'N',
                No_of_Holidays = vm.No_of_Holidays,
                LogoFileName = logoFileName
            };

            await _repo.UpdateAsync(entity);

            TempData["Success"] = "Bank updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var data = await _repo.GetByIdAsync(id);
            if (data != null && !string.IsNullOrEmpty(data.LogoFileName))
            {
                var oldPath = Path.Combine(_env.WebRootPath, "uploads", "bank-logos", data.LogoFileName);
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            await _repo.DeleteAsync(id);

            TempData["Success"] = "Bank deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}