namespace PharmaCare.ViewModels
{
    /* ViewModel for error page display with request tracking */
    public class ErrorViewModel
    {
        /* Nullable request identifier for error correlation */
        public string? RequestId { get; set; }

        /* Computed property determining if request ID should be displayed to user */
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}