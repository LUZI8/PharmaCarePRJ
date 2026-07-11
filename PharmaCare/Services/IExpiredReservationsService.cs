namespace PharmaCare.Services
{
    /* Interface defining contract for expired prescription reservations management service */
    public interface IExpiredReservationsService
    {
        /* Method to manually trigger immediate check and processing of expired reservations */
        Task CheckExpiredReservationsNow();
    }
}