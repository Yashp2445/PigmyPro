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
            ViewBag.IsSuperAdmin = CurrentUserRole == AppRoles.SuperAdmin;
            var data = await _repo.GetAllAsync();

            var vm = new List<BankListVM>();
            foreach (var x in data)
            {
                var branchCount = await _repo.GetDependentBranchCountAsync(x.BankID);
                vm.Add(new BankListVM
                {
                    BankID = x.BankID,
                    Name = x.Name,
                    ActiveYN = x.ActiveYN,
                    HasCBS = x.hasCBS == 'Y',
                    RecieptPrinting = x.RecieptPrinting == 'Y',
                    HasLogo = !string.IsNullOrEmpty(x.LogoFileName), // Keep LogoFileName as an indicator of existence
                    DependentBranchCount = branchCount
                });
            }

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

        private async Task<(byte[]? LogoData, string? LogoFileName)> HandleLogoUploadAsync(BankCreateEditVM vm, byte[]? currentLogo, string? currentFileName)
        {
            if (vm.RemoveLogo)
                return (null, null);

            if (vm.LogoFile != null && vm.LogoFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(vm.LogoFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                    throw new Exception("Only JPG, JPEG, and PNG images are allowed.");

                if (vm.LogoFile.Length > 2 * 1024 * 1024)
                    throw new Exception("File size must not exceed 2MB.");

                using var memoryStream = new MemoryStream();
                await vm.LogoFile.CopyToAsync(memoryStream);
                return (memoryStream.ToArray(), vm.LogoFile.FileName);
            }

            return (currentLogo, currentFileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BankCreateEditVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            long code;
            byte[]? logoData = null;
            string? logoFileName = null;
            try
            {
                code = GetCollectionGLCode(vm);
                var uploadResult = await HandleLogoUploadAsync(vm, null, null);
                logoData = uploadResult.LogoData;
                logoFileName = uploadResult.LogoFileName;
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
                AppLoginPrefix = vm.AppLoginPrefix,
                ActiveYN = vm.ActiveYN,
                CollectionGLCode = code,
                hasCBS = vm.HasCBS ? 'Y' : 'N',
                RecieptPrinting = vm.RecieptPrinting ? 'Y' : 'N',
                No_of_Holidays = vm.No_of_Holidays,
                Logo = logoData,
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
                AppLoginPrefix = data.AppLoginPrefix,
                ActiveYN = data.ActiveYN,
                HasCBS = data.hasCBS == 'Y',
                RecieptPrinting = data.RecieptPrinting == 'Y',
                No_of_Holidays = data.No_of_Holidays,
                HasExistingLogo = !string.IsNullOrEmpty(data.LogoFileName),

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
            byte[]? logoData = null;
            string? logoFileName = null;
            try
            {
                var existingBank = await _repo.GetByIdAsync(vm.BankID);
                if (existingBank == null) return NotFound();

                code = GetCollectionGLCode(vm);
                var uploadResult = await HandleLogoUploadAsync(vm, existingBank.Logo, existingBank.LogoFileName);
                logoData = uploadResult.LogoData;
                logoFileName = uploadResult.LogoFileName;
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
                AppLoginPrefix = vm.AppLoginPrefix,
                ActiveYN = vm.ActiveYN,
                CollectionGLCode = code,
                hasCBS = vm.HasCBS ? 'Y' : 'N',
                RecieptPrinting = vm.RecieptPrinting ? 'Y' : 'N',
                No_of_Holidays = vm.No_of_Holidays,
                Logo = logoData,
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
            var branchCount = await _repo.GetDependentBranchCountAsync(id);
            if (branchCount > 0)
            {
                TempData["Error"] = $"Cannot delete this bank because it has {branchCount} dependent branch(es).";
                return RedirectToAction(nameof(Index));
            }

            await _repo.DeleteAsync(id);

            TempData["Success"] = "Bank deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CheckDependencies(int id)
        {
            var branchCount = await _repo.GetDependentBranchCountAsync(id);
            if (branchCount > 0)
            {
                return Json(new { canDelete = false, branchCount, message = $"This bank cannot be deleted because it still has {branchCount} active branch(es). Please remove all branches first." });
            }
            return Json(new { canDelete = true, branchCount = 0, message = "" });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetLogo(int id)
        {
            var bank = await _repo.GetByIdAsync(id);
            if (bank == null || bank.Logo == null)
            {
                // Return a placeholder or 404
                return NotFound();
            }

            var extension = Path.GetExtension(bank.LogoFileName)?.ToLowerInvariant();
            var mimeType = "image/jpeg"; // default
            if (extension == ".png") mimeType = "image/png";

            return File(bank.Logo, mimeType);
        }
    }
}