namespace PharmaCare.Models
{
    /* Entity model for prescription medication reservations with expiry tracking */
    public class PrescriptionReservation
    {
        /* Primary key with explicit Key attribute */
        [Key]
        public int ReservationId { get; set; }

        /* Foreign key linking reservation to customer */
        public int UserId { get; set; }

        /* Foreign key linking to prescription medication product */
        public int ProductId { get; set; }

        /* Quantity of medication reserved (typically 1 for prescriptions) */
        public int Quantity { get; set; }

        /* Human-readable reservation number for customer tracking */
        [StringLength(50)]
        public string ReservationNumber { get; set; }

        /* Current reservation status (Reserved, Expired, Completed, Cancelled) */
        [StringLength(50)]
        public string Status { get; set; }

        /* Date and time when reservation was made */
        public DateTime ReservationDate { get; set; }

        /* Expiration date after which reservation becomes invalid */
        public DateTime ExpiryDate { get; set; }

        /* Optional notes for prescription details or special instructions */
        [StringLength(500)]
        public string Notes { get; set; }

        /* Nullable timestamp for when prescription was picked up */
        public DateTime? CompletedDate { get; set; }

        /* Navigation property to customer with explicit foreign key attribute */
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        /* Navigation property to medication product with explicit foreign key attribute */
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}