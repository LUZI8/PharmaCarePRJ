namespace PharmaCare.Models
{
    /* Entity model for storing customer contact form submissions */
    public class ContactMessage
    {
        /* Primary key for message identification */
        public int Id { get; set; }

        /* Contact person's first name */
        public string FirstName { get; set; }

        /* Contact person's last name */
        public string LastName { get; set; }

        /* Email address for response communication */
        public string Email { get; set; }

        /* Message subject line */
        public string Subject { get; set; }

        /* Main message content from contact form */
        public string Message { get; set; }

        /* Timestamp when message was submitted */
        public DateTime DateSubmitted { get; set; }

        /* Nullable foreign key - links to user if message from registered user */
        public int? UserId { get; set; }

        /* Type identifier for user status (registered/guest) */
        public string UserType { get; set; }

        /* Navigation property to user entity - virtual for lazy loading */
        public virtual User User { get; set; }
    }
}