using Nyntra.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;

namespace Nyntra.Controllers
{
    public class AdminController : Controller
    {
        private readonly MyntraEntities _context;

     
        // GET: Admin
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult MyProfile()
        {
            return View();
        }

       
        //display
        [HttpGet]
        public ActionResult ProductList()
        {
            try
            {
                using (var context = new MyntraEntities())
                {
                    var products = context.Products.ToList();
                    return View(products);
                }
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it)
                Response.Write(ex.Message);
                return View(new List<Product>()); // Return an empty list in case of error
            }
        }

        private MyntraEntities db = new MyntraEntities();

        // GET: Product/Create
        [HttpGet]
        public ActionResult AddProducts()
        {
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        public ActionResult AddProducts(Product productData, HttpPostedFileBase ImageFile)
        {
            try
            {
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    // Ensure the directory exists
                    string directoryPath = Server.MapPath("~/Images/");
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    // Save the file to the server
                    string path = Path.Combine(directoryPath, Path.GetFileName(ImageFile.FileName));
                    ImageFile.SaveAs(path);

                    // Set the ImagePath for storing in the database
                    productData.ProductImage = "~/Images/" + ImageFile.FileName;
                }

                // Save the product data to the database
                db.Products.Add(productData);
                int resultValue = db.SaveChanges();

                if (resultValue > 0)
                {
                    ViewBag.Message = "Product added successfully";
                }
                else
                {
                    ViewBag.Message = "Product not inserted";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error: " + ex.Message;
            }
            return View(productData);
        }


        //edit
        [HttpGet]
        public ActionResult EditProducts(int id)
        {
            using (var context = new MyntraEntities())
            {
                var product = context.Products.Find(id);
                if (product == null)
                {
                    return HttpNotFound(); // Return 404 if the product is not found
                }
                return View(product);
            }
        }

        // POST: Products/Edit/5
        [HttpPost]
        public ActionResult EditProducts(Product model, HttpPostedFileBase ProductImage)
        {
            try
            {
                using (var context = new MyntraEntities())
                {
                    var existingProduct = context.Products.Find(model.ProductID);
                    if (existingProduct != null)
                    {
                        existingProduct.ProductName = model.ProductName;
                        existingProduct.ProductPrice = model.ProductPrice;
                        existingProduct.ProductDescription = model.ProductDescription;
                        existingProduct.Category = model.Category;
                        existingProduct.Color = model.Color;
                        existingProduct.Brand = model.Brand;

                        // Check if a new image is uploaded
                        if (ProductImage != null && ProductImage.ContentLength > 0)
                        {
                            // Save the new image to a folder and get the path
                            var fileName = Path.GetFileName(ProductImage.FileName);
                            var path = Path.Combine(Server.MapPath("~/Images/"), fileName);
                            ProductImage.SaveAs(path);
                            existingProduct.ProductImage = "~/Images/" + fileName; // Save the relative path to the database
                        }

                        context.SaveChanges(); // Save changes to the database
                        return RedirectToAction("ProductList"); // Redirect after successful edit
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message); // Add the error message to the model state
            }

            return View(model); // Return the view with the model in case of error
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            using (var context = new MyntraEntities())
            {
                var product = context.Products.Find(id);
                if (product == null)
                {
                    return HttpNotFound(); // Return 404 if the product is not found
                }
                return View(product);
            }
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            using (var context = new MyntraEntities())
            {
                var product = context.Products.Find(id);
                if (product != null)
                {
                    context.Products.Remove(product);
                    context.SaveChanges();
                }
                return RedirectToAction("ProductList"); // Redirect to the product list after deletion
            }
        }

        // Action to download the product list as PDF
        
        public ActionResult DownloadPdf()
        {
            var products = _context.Products.ToList(); // Fetch products

            using (var memoryStream = new MemoryStream())
            {
                Document document = new Document();
                PdfWriter.GetInstance(document, memoryStream);
                document.Open();

                // Add content to the PDF
                document.Add(new Paragraph("Product List"));
                document.Add(new Paragraph("\n"));
                PdfPTable table = new PdfPTable(8); // Adjust column count as needed

                // Add table headers
                table.AddCell("Product ID");
                table.AddCell("Product Name");
                table.AddCell("Product Price");
                table.AddCell("Product Description");
                table.AddCell("Image");
                table.AddCell("Category");
                table.AddCell("Status");
                table.AddCell("Color");
                table.AddCell("Brand");

                // Add table rows
                foreach (var product in products)
                {
                    table.AddCell(product.ProductID.ToString());
                    table.AddCell(product.ProductName);
                    table.AddCell(product.ProductPrice?.ToString("F2")); // Format price to two decimal places
                    table.AddCell(product.ProductDescription);
                    table.AddCell(product.ProductImage);
                    table.AddCell(product.Category);
                    table.AddCell(product.Status);
                    table.AddCell(product.Color);
                    table.AddCell(product.Brand);
                }

                document.Add(table);
                document.Close();

                return File(memoryStream.ToArray(), "application/pdf", "ProductList.pdf");
            }
        }

        // Action to download the product list as Excel
        public ActionResult DownloadExcel()
        {
            var products = _context.Products.ToList(); // Fetch products

            using (var memoryStream = new MemoryStream())
            {
                using (var package = new ExcelPackage(memoryStream))
                {
                    var worksheet = package.Workbook.Worksheets.Add("Products");
                    worksheet.Cells[1, 1].Value = "Product ID";
                    worksheet.Cells[1, 2].Value = "Product Name";
                    worksheet.Cells[1, 3].Value = "Product Price";
                    worksheet.Cells[1, 4].Value = "Product Description";
                    worksheet.Cells[1, 5].Value = "Image";
                    worksheet.Cells[1, 6].Value = "Category";
                    worksheet.Cells[1, 7].Value = "Status";
                    worksheet.Cells[1, 8].Value = "Color";
                    worksheet.Cells[1, 9].Value = "Brand";

                    // Add data to the Excel sheet
                    int row = 2; // Start from the second row
                    foreach (var product in products)
                    {
                        worksheet.Cells[row, 1].Value = product.ProductID;
                        worksheet.Cells[row, 2].Value = product.ProductName;
                        worksheet.Cells[row, 3].Value = product.ProductPrice;
                        worksheet.Cells[row, 4].Value = product.ProductDescription;
                        worksheet.Cells[row, 5].Value = product.ProductImage;
                        worksheet.Cells[row, 6].Value = product.Category;
                        worksheet.Cells[row, 7].Value = product.Status;
                        worksheet.Cells[row, 8].Value = product.Color;
                        worksheet.Cells[row, 9].Value = product.Brand;
                        row++;
                    }

                    package.Save();
                }

                return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ProductList.xlsx");
            }
        }
    }
}

    
