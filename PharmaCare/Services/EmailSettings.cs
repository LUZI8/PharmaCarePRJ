namespace PharmaCare.Services
{
    /* Strongly-typed SMTP configuration bound from the "EmailSettings" section of appsettings.json.
       Leave Host blank to run in development fallback mode, where codes are written to the app log
       instead of being sent over SMTP. */
    public class EmailSettings
    {
        /* SMTP server host, e.g. "smtp.gmail.com". Blank = dev fallback (log only). */
        public string Host { get; set; } = "";

        /* SMTP port, typically 587 for STARTTLS. */
        public int Port { get; set; } = 587;

        /* Whether to use SSL/TLS for the SMTP connection. */
        public bool EnableSsl { get; set; } = true;

        /* SMTP account username (usually the sending email address). */
        public string Username { get; set; } = "";

        /* SMTP password or app-password. Fill this in yourself; never commit a real value. */
        public string Password { get; set; } = "";

        /* Address the email is sent "from". Defaults to Username when blank. */
        public string FromAddress { get; set; } = "";

        /* Friendly sender name shown in the recipient's inbox. */
        public string FromName { get; set; } = "PharmaCare";
    }
}
