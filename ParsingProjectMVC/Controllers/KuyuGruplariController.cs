﻿using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using ParsingProjectMVC.Models;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ParsingProjectMVC.Controllers
{
    public class KuyuGruplariController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _filePath;
        private List<KuyuGrubuModel> kuyuGruplari = new List<KuyuGrubuModel>();

        public KuyuGruplariController(IWebHostEnvironment env)
        {
            _env = env;
            _filePath = Path.Combine(_env.WebRootPath, "data", "Kuyu Grupları.csv");
            LoadKuyuGruplariFromCsv();
        }

        private void LoadKuyuGruplariFromCsv()
        {
            try
            {
                using (var reader = new StreamReader(_filePath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<dynamic>().ToList();
                    int idCounter = 1;
                    foreach (var record in records)
                    {
                        kuyuGruplari.Add(new KuyuGrubuModel
                        {
                            Id = idCounter++,
                            KuyuGrubuAdi = record.KuyuGrubuAdi,
                            SahaAdi = null // burayı kontrol et
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata yönetimi
                Console.WriteLine($"CSV dosyası okunurken bir hata oluştu: {ex.Message}");
            }
        }

        private void SaveKuyuGruplariToCsv()
        {
            try
            {
                using (var writer = new StreamWriter(_filePath))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(kuyuGruplari.Select(k => new { KuyuGrubuAdi = k.KuyuGrubuAdi }));
                }
            }
            catch (Exception ex)
            {
                // Hata yönetimi
                Console.WriteLine($"CSV dosyasına yazılırken bir hata oluştu: {ex.Message}");
            }
        }

        [HttpPost]
        public IActionResult Create(string kuyuGrubuAdi, int pageNumber = 1, int pageSize = 50)
        {
            var regex = new Regex(@"^([A-Z]+(\s[A-Z]+)*)-\d+$");

            bool isExisting = kuyuGruplari.Any(k => k.KuyuGrubuAdi.Equals(kuyuGrubuAdi, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(kuyuGrubuAdi) && regex.IsMatch(kuyuGrubuAdi) && !isExisting)
            {
                var newKuyuGrubu = new KuyuGrubuModel
                {
                    Id = kuyuGruplari.Count > 0 ? kuyuGruplari.Max(k => k.Id) + 1 : 1,
                    KuyuGrubuAdi = kuyuGrubuAdi
                };
                kuyuGruplari.Add(newKuyuGrubu);
                SaveKuyuGruplariToCsv();
                TempData["SuccessMessage"] = "Kuyu Grubu başarıyla eklendi!";
            }
            else
            {
                if (isExisting)
                {
                    TempData["ErrorMessage"] = "Kuyu Grubu adı zaten mevcut.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Kuyu Grubu adı formatınız doğru değil.";
                }
            }
            return RedirectToAction("Index", new { pageNumber, pageSize });
        }

        [HttpPost]
        public IActionResult Delete(int id, int pageNumber = 1, int pageSize = 50)
        {
            var kuyuGrubu = kuyuGruplari.FirstOrDefault(k => k.Id == id);
            if (kuyuGrubu != null)
            {
                kuyuGruplari.Remove(kuyuGrubu);
                SaveKuyuGruplariToCsv();
                TempData["SuccessMessage"] = "Kuyu Grubu başarıyla silindi!";
            }
            else
            {
                TempData["ErrorMessage"] = "Kuyu Grubu bulunamadı!";
            }
            return RedirectToAction("Index", new { pageNumber, pageSize });
        }

        [HttpGet]
        public IActionResult Index(int pageNumber = 1, int pageSize = 50)
        {
            var pagedKuyuGruplari = kuyuGruplari
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var totalItems = kuyuGruplari.Count;

            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            return View(pagedKuyuGruplari);
        }

        [HttpGet]
        public IActionResult Update(int id)
        {
            var kuyuGrubu = kuyuGruplari.FirstOrDefault(k => k.Id == id);
            if (kuyuGrubu == null)
            {
                return NotFound();
            }
            return View(kuyuGrubu);
        }

        [HttpPost]
        public IActionResult Update(int id, KuyuGrubuModel updatedKuyuGrubu, int pageNumber = 1, int pageSize = 50)
        {
            var regex = new Regex(@"^([A-Z]+(\s[A-Z]+)*)-\d+$");

            var kuyuGrubu = kuyuGruplari.FirstOrDefault(k => k.Id == id);

            if (kuyuGrubu == null)
            {
                TempData["ErrorMessage"] = "Güncellenecek Kuyu Grubu bulunamadı.";
                return RedirectToAction("Index", new { pageNumber, pageSize });
            }

            bool isExisting = kuyuGruplari.Any(kg => kg.KuyuGrubuAdi.Equals(updatedKuyuGrubu.KuyuGrubuAdi, StringComparison.OrdinalIgnoreCase) && kg.Id != id);

            if (!regex.IsMatch(updatedKuyuGrubu.KuyuGrubuAdi))
            {
                TempData["ErrorMessage"] = "Kuyu Grubu adı formatınız doğru değil.";
                return RedirectToAction("Index", new { pageNumber, pageSize });
            }

            if (isExisting)
            {
                TempData["ErrorMessage"] = "Bu Kuyu Grubu adı kullanılmaktadır.";
                return RedirectToAction("Index", new { pageNumber, pageSize });
            }

            kuyuGrubu.KuyuGrubuAdi = updatedKuyuGrubu.KuyuGrubuAdi;
            SaveKuyuGruplariToCsv();
            TempData["SuccessMessage"] = "Kuyu Grubu başarıyla güncellendi!";
            return RedirectToAction("Index", new { pageNumber, pageSize });
        }
    }
}