namespace KrishivVideoUploader.Modal
{
    public class UserSubscription
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public bool IsSubscribed { get; set; }
        public DateTime? SubscribedOn { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
