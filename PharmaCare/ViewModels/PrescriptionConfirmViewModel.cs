namespace PharmaCare.ViewModels
{
    /* ViewModel for prescription medication confirmation page containing all reservation details */
    public class PrescriptionConfirmViewModel
    {
        /* Product entity containing medication details and prescription requirements */
        public Product Product { get; set; }

        /* Quantity of medication being reserved (typically 1 for prescription items) */
        public int Quantity { get; set; }

        /* Calculated total price for the prescription reservation */
        public decimal TotalPrice { get; set; }

        /* User entity containing customer information for prescription verification */
        public User User { get; set; }

        /* Expiration date for the prescription reservation before pickup required */
        public DateTime ExpiryDate { get; set; }
    }
}