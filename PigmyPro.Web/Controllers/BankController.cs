using Microsoft.AspNetCore.Mvc;
using PigmyPro.Data.Interfaces;
using PigmyPro.Domain.Entities;
using PigmyPro.Web.ViewModels.Bank;
using Microsoft.AspNetCore.Authorization;
using PigmyPro.Domain;

namespace PigmyPro.Web.Controllers
{
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public class BankController : BaseController
    {
        private readonly IBankRepository _repo;

        public BankController(IBankRepository repo)
        {
            _repo = repo;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _repo.GetAllAsync();

            var vm = data.Select(x => new BankListVM
            {
                BankID = x.BankID,
                Name = x.Name,
                ActiveYN = x.ActiveYN,
                HasCBS = x.hasCBS == 'Y'
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BankCreateEditVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            long code;
            try
            {
                code = GetCollectionGLCode(vm);
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
            try
            {
                code = GetCollectionGLCode(vm);
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
                No_of_Holidays = vm.No_of_Holidays
            };

            await _repo.UpdateAsync(entity);

            TempData["Success"] = "Bank updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);

            TempData["Success"] = "Bank deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}