namespace PharmaCare.Services
{
    /* Background service that automatically processes expired prescription reservations */
    public class ExpiredReservationsService : BackgroundService, IExpiredReservationsService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ExpiredReservationsService> _logger;
        /* Time interval between expired reservation checks - set to 1 hour */
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

        /* Constructor with dependency injection for service provider and logging */
        public ExpiredReservationsService(
            IServiceProvider services,
            ILogger<ExpiredReservationsService> logger)
        {
            _services = services;
            _logger = logger;
        }

        /* Main background task execution method that runs continuously */
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Expired Reservations Service is starting.");

            /* Continue running until application shutdown is requested */
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Checking for expired reservations");
                try
                {
                    /* Execute the expired reservations check */
                    await CheckExpiredReservationsNow();
                }
                catch (Exception ex)
                {
                    /* Log any errors but continue running the service */
                    _logger.LogError(ex, "Error in background expired reservations check");
                }

                /* Wait for the specified interval before next check */
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        /* Public method to manually trigger expired reservations check */
        public async Task CheckExpiredReservationsNow()
        {
            /* Create service scope to access scoped dependencies like DbContext */
            using (var scope = _services.CreateScope())
            {
                /* Get required services from the scope */
                var dbContext = scope.ServiceProvider.GetRequiredService<DataDbContext>();
                var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

                try
                {
                    var now = DateTime.Now;
                    /* Query for reservations that are still "Reserved" but past their expiry date */
                    var expiredReservations = await dbContext.PrescriptionReservations
                        .Include(r => r.Product)
                        .Where(r => r.Status == "Reserved" && r.ExpiryDate < now)
                        .ToListAsync();

                    _logger.LogInformation($"Found {expiredReservations.Count} expired reservations to process");

                    /* Process each expired reservation */
                    foreach (var reservation in expiredReservations)
                    {
                        /* Update reservation status to expired */
                        reservation.Status = "Expired";

                        /* Return reserved stock back to product inventory */
                        var product = reservation.Product;
                        if (product != null)
                        {
                            /* Add reserved quantity back to available stock */
                            product.Stock += reservation.Quantity;
                            productRepository.Update(product.ProductId, product);
                            _logger.LogInformation($"Returned {reservation.Quantity} units to product {product.ProductId} ({product.ProductName})");
                        }
                    }

                    /* Save all changes to database */
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Expired reservations processed successfully");
                }
                catch (Exception ex)
                {
                    /* Log any database or processing errors */
                    _logger.LogError(ex, "Error processing expired reservations");
                }
            }
        }
    }
}