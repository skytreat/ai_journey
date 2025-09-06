
using System;
using System.Threading.Tasks;
using Ipam.Client;
using Ipam.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Ipam.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IpamClient _ipamClient;

        public HomeController(IpamClient ipamClient)
        {
            _ipamClient = ipamClient;
        }

        public async Task<IActionResult> Index()
        {
            var addressSpaces = await _ipamClient.GetAddressSpacesAsync();
            return View(addressSpaces);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] AddressSpaceDto addressSpaceDto)
        {
            if (ModelState.IsValid)
            {
                await _ipamClient.CreateAddressSpaceAsync(addressSpaceDto);
                return RedirectToAction(nameof(Index));
            }
            return View(addressSpaceDto);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var addressSpace = await _ipamClient.GetAddressSpaceAsync(id.Value);
            if (addressSpace == null)
            {
                return NotFound();
            }
            return View(addressSpace);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Description")] AddressSpaceDto addressSpaceDto)
        {
            if (id != addressSpaceDto.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _ipamClient.UpdateAddressSpaceAsync(id, addressSpaceDto);
                return RedirectToAction(nameof(Index));
            }
            return View(addressSpaceDto);
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var addressSpace = await _ipamClient.GetAddressSpaceAsync(id.Value);
            if (addressSpace == null)
            {
                return NotFound();
            }
            return View(addressSpace);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var addressSpace = await _ipamClient.GetAddressSpaceAsync(id.Value);
            if (addressSpace == null)
            {
                return NotFound();
            }
            return View(addressSpace);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _ipamClient.DeleteAddressSpaceAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

