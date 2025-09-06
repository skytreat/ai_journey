
using System;
using System.Threading.Tasks;
using Ipam.Client;
using Ipam.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ipam.Web.Controllers
{
    public class TagsController : Controller
    {
        private readonly IpamClient _ipamClient;

        public TagsController(IpamClient ipamClient)
        {
            _ipamClient = ipamClient;
        }

        public async Task<IActionResult> Index(Guid addressSpaceId)
        {
            ViewBag.AddressSpaceId = addressSpaceId;
            var tags = await _ipamClient.GetTagsAsync(addressSpaceId);
            return View(tags);
        }

        public IActionResult Create(Guid addressSpaceId)
        {
            ViewBag.AddressSpaceId = addressSpaceId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid addressSpaceId, [Bind("Name,Description,Type,KnownValues,Attributes")] TagDto tagDto)
        {
            if (ModelState.IsValid)
            {
                await _ipamClient.CreateTagAsync(addressSpaceId, tagDto);
                return RedirectToAction(nameof(Index), new { addressSpaceId = addressSpaceId });
            }
            ViewBag.AddressSpaceId = addressSpaceId;
            return View(tagDto);
        }

        public async Task<IActionResult> Edit(Guid addressSpaceId, string name)
        {
            ViewBag.AddressSpaceId = addressSpaceId;
            var tag = await _ipamClient.GetTagAsync(addressSpaceId, name);
            if (tag == null)
            {
                return NotFound();
            }
            return View(tag);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid addressSpaceId, string name, [Bind("Name,Description,Type,KnownValues,Attributes")] TagDto tagDto)
        {
            if (ModelState.IsValid)
            {
                await _ipamClient.UpdateTagAsync(addressSpaceId, name, tagDto);
                return RedirectToAction(nameof(Index), new { addressSpaceId = addressSpaceId });
            }
            ViewBag.AddressSpaceId = addressSpaceId;
            return View(tagDto);
        }

        public async Task<IActionResult> Details(Guid addressSpaceId, string name)
        {
            ViewBag.AddressSpaceId = addressSpaceId;
            var tag = await _ipamClient.GetTagAsync(addressSpaceId, name);
            if (tag == null)
            {
                return NotFound();
            }
            return View(tag);
        }

        public async Task<IActionResult> Delete(Guid addressSpaceId, string name)
        {
            ViewBag.AddressSpaceId = addressSpaceId;
            var tag = await _ipamClient.GetTagAsync(addressSpaceId, name);
            if (tag == null)
            {
                return NotFound();
            }
            return View(tag);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid addressSpaceId, string name)
        {
            await _ipamClient.DeleteTagAsync(addressSpaceId, name);
            return RedirectToAction(nameof(Index), new { addressSpaceId = addressSpaceId });
        }
    }
}
