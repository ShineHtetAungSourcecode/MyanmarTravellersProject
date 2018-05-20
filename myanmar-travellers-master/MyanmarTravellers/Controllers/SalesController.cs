using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyanmarTravellers.Models;
using QRCoder;

namespace MyanmarTravellers.Controllers
{
    public class SalesController : Controller
    {
        private MMTravellersEntities db = new MMTravellersEntities();
        private readonly string CART = "cart";

        // GET: Sales
        [Authorize]
        public ActionResult Index()
        {
            return View(db.Sales.ToList());
        }

        // GET: Sales/Details/5
        [Authorize]
        public ActionResult Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Sale sale = db.Sales.Find(id);
            if (sale == null)
            {
                return HttpNotFound();
            }
            return View(sale);
        }

        

        // GET: Sales/Delete/5
        [Authorize]        
        public ActionResult Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Sale sale = db.Sales.Find(id);
            if (sale == null)
            {
                return HttpNotFound();
            }
            return View(sale);
        }

        // POST: Sales/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]        
        public ActionResult DeleteConfirmed(long id)
        {
            Sale sale = db.Sales.Find(id);
            db.Sales.Remove(sale);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [Authorize]
        public ActionResult CheckOut(long[] ticket_ids, long course_id)
        {
            if(null == Session[CART])
            {
                Session[CART] = new List<Ticket>();
            }

            if(null == ticket_ids)
            {
                ModelState.AddModelError("", "Please Choose At least one seat to checkout");
                return Redirect(Request.UrlReferrer.ToString());
            }
            var course = db.Courses.Find(course_id);
            var cart = (List<Ticket>)Session[CART];

            var tickets = db.Tickets.Where(t => ticket_ids.Contains(t.id)).Include(t => t.Cours).ToList();
            cart.AddRange(tickets);

            ViewBag.Tickets = tickets;
            ViewBag.Course = course;
            ViewBag.Amount = tickets.Count * course.fee_per_seat;

            return View();
        }

        // POST: Sales/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost, ActionName("CheckOut")]
        [ValidateAntiForgeryToken]
        [Authorize]        
        public ActionResult CheckOutConfirm([Bind(Include = "customer_name,nrc,phone,total")] Sale sale)
        {
            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {

                        sale.created_at = DateTime.Now;
                        sale.updated_at = DateTime.Now;
                        db.Sales.Add(sale);
                        db.SaveChanges();

                        var tickets = (List<Ticket>)Session[CART];
                        foreach(var ticket in tickets)
                        {
                            ticket.sale_id = sale.id;
                            db.Entry(ticket).State = EntityState.Modified;
                        }
                        db.SaveChanges();

                        transaction.Commit();
                        return RedirectToAction("Index");
                    } catch (Exception e)
                    {
                        transaction.Rollback();
                    }
                }
            }

            return View(sale);
        }

        //Generates the sale_id 
        [Authorize]
        public ActionResult GenerateQR(long sale_id)
        {
            FileContentResult result = null;

            var sale = db.Sales.Find(sale_id);

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(sale.ToString(), QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);
            var img_Converter = new ImageConverter();
            var img_byte_array = (byte[])img_Converter.ConvertTo(qrCodeImage, typeof(byte[]));
            result = this.File(img_byte_array, "image/jpeg");

            return result;
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
