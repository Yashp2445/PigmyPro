using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PigmyPro.Data.Interfaces;
using PigmyPro.Web.ViewModels.Map;

namespace PigmyPro.Web.Controllers
{
    [Authorize]
    public class MapController : BaseController
    {
        private readonly IMapRepository _repo;
        private readonly string[] _palette = {
            "#2563eb", "#16a34a", "#dc2626", "#d97706", "#7c3aed",
            "#0891b2", "#be185d", "#65a30d", "#ea580c", "#0f766e"
        };

        public MapController(IMapRepository repo)
        {
            _repo = repo;
        }

        public async Task<IActionResult> Index()
        {
            if (CurrentUserRole == "SuperAdmin")
                return RedirectToAction("Index", "Dashboard");

            ViewData["Breadcrumb"] = "<li class='breadcrumb-item active'>Collection Map</li>";

            var vm = new MapIndexVM
            {
                UserRole = CurrentUserRole,
                Date = DateTime.Today
            };

            if (CurrentUserRole == "BankAdmin")
            {
                var branches = await _repo.GetBranchDropdownAsync(CurrentBankID);
                vm.Branches = branches.Select(b => new SelectListItem(b.Name, b.BranchID.ToString())).ToList();
            }

            int? searchBranchId = CurrentUserRole == "BranchAdmin" ? CurrentBranchID : null;
            var agents = await _repo.GetAgentDropdownAsync(CurrentBankID, searchBranchId);
            vm.Agents = agents.Select(a => new SelectListItem(a.Name, a.Code.ToString())).ToList();

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetPins(DateTime date, int? branchCode, long? agentCode)
        {
            if (CurrentUserRole == "SuperAdmin") return Unauthorized();

            int bankId = CurrentBankID;
            int? searchBranchId = CurrentUserRole == "BranchAdmin" ? CurrentBranchID : branchCode;

            var rows = await _repo.GetMapTransactionsAsync(bankId, date, searchBranchId, agentCode);

            var pins = rows
                .Select(r => new {
                    Row = r,
                    LatParsed = double.TryParse(r.Latitude, out var lat) ? lat : (double?)null,
                    LngParsed = double.TryParse(r.Longitude, out var lng) ? lng : (double?)null
                })
                .Where(x => x.LatParsed.HasValue && x.LngParsed.HasValue)
                .GroupBy(x => new { x.Row.AgentCode, x.LatParsed, x.LngParsed })
                .Select(g => new MapPinVM
                {
                    AgentCode = g.Key.AgentCode,
                    AgentName = g.First().Row.AgentName,
                    BranchName = g.First().Row.BranchName,
                    Latitude = g.Key.LatParsed.Value,
                    Longitude = g.Key.LngParsed.Value,
                    TotalAmount = g.Sum(x => x.Row.Amount),
                    TransactionCount = g.Count(),
                    SequenceNo = g.First().Row.SequenceNo,
                    Entries = g.Select(x => new MapEntryVM
                    {
                        CustomerName = x.Row.CustomerName,
                        Amount = x.Row.Amount,
                        EntryTime = x.Row.EntryTime?.ToString("hh:mm tt") ?? "--",
                        Date = x.Row.Date.ToString("dd-MMM-yyyy"),
                        SequenceNo = x.Row.SequenceNo
                    }).ToList()
                })
                .ToList();

            // Assign colors by agent
            var agentCodes = pins.Select(p => p.AgentCode).Distinct().ToList();
            foreach (var pin in pins)
            {
                int idx = agentCodes.IndexOf(pin.AgentCode);
                pin.ColorHex = _palette[idx % _palette.Length];
            }

            return Json(pins);
        }

        [HttpGet]
        public async Task<IActionResult> GetAgents(int? branchCode)
        {
            int? searchBranchId = CurrentUserRole == "BranchAdmin" ? CurrentBranchID : branchCode;
            var agents = await _repo.GetAgentDropdownAsync(CurrentBankID, searchBranchId);
            var result = agents.Select(a => new { value = a.Code.ToString(), text = a.Name }).ToList();
            return Json(result);
        }
    }
}
