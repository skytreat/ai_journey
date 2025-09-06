
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ipam.Client;
using Ipam.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ipam.Web.Controllers
{
    public class IpAddressesController : Controller
    {
        private readonly IpamClient _ipamClient;

        public IpAddressesController(IpamClient ipamClient)
        {
            _ipamClient = ipamClient;
        }

        public async Task<IActionResult> Index(Guid addressSpaceId)
        {
            ViewBag.AddressSpaceId = addressSpaceId;
            var ipAddresses = await _ipamClient.GetIpAddressesAsync(addressSpaceId);
            return View(ipAddresses);
        }

        public async Task<IActionResult> Create(Guid addressSpaceId)
        {
            ViewBag.AddressSpaceId = addressSpaceId;
            // You might want to fetch existing IPs to populate a dropdown for ParentId
            var existingIps = await _ipamClient.GetIpAddressesAsync(addressSpaceId);
            ViewBag.ParentId = new SelectList(existingIps, "Id", "Prefix");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid addressSpaceId, [Bind("Prefix,Tags,ParentId")] IpAddressDto ipAddressDto)
        {
            if (ModelState.IsValid)
            {
                await _ipamClient.CreateIpAddressAsync(addressSpaceId, ipAddressDto);
                return RedirectToAction(nameof(Index), new { addressSpaceId = addressSpaceId });
            }
            ViewBag.AddressSpaceId = addressSpaceId;
            return View(ipAddressDto);
        }

        public async Task<IActionResult> Edit(Guid addressSpaceId, Guid id)
        {
            ViewBag.AddressSpaceId = addressSpaceId;
            var ipAddress = await _ipamClient.GetIpAddressAsync(addressSpaceId, id);
            if (ipAddress == null)
            {
                return NotFound();
            }
            // You might want to fetch existing IPs to populate a dropdown for ParentId
            var existingIps = await _ipamClient.GetIpAddressesAsync(addressSpaceId);
            ViewBag.ParentId = new SelectList(existingIps, "Id", "Prefix", ipAddress.ParentId);
            return View(ipAddress);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid addressSpaceId, Guid id, [Bind("Id,Prefix,Tags,ParentId")] IpAddressDto ipAddressDto)
        {
            if (id != ipAddressDto.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _ipamClient.UpdateIpAddressAsync(addressSpaceId, id, ipAddressDto);
                return RedirectToAction(nameof(Index), new { addressSpaceId = addressSpaceId });
            }
            ViewBag.AddressSpaceId = addressSpaceId;
            return View(ipAddressDto);
        }

        public async Task<IActionResult> Details(Guid addressSpaceId, Guid id)
        {
            ViewBag.AddressSpaceId = addressSpaceId;
            var ipAddress = await _ipamClient.GetIpAddressAsync(addressSpaceId, id);
            if (ipAddress == null)
            {
                return NotFound();
            }
            return View(ipAddress);
        }

        public async Task<IActionResult> Delete(Guid addressSpaceId, Guid id)
        {
            ViewBag.AddressSpaceId = addressSpaceId;
            var ipAddress = await _ipamClient.GetIpAddressAsync(addressSpaceId, id);
            if (ipAddress == null)
            {
                return NotFound();
            }
            return View(ipAddress);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid addressSpaceId, Guid id)
        {
            await _ipamClient.DeleteIpAddressAsync(addressSpaceId, id);
            return RedirectToAction(nameof(Index), new { addressSpaceId = addressSpaceId });
        }
    }
}
