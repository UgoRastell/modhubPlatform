using System.Text.Json.Serialization;

namespace Frontend.Models.Moderation
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ContentType
    {
        ForumPost,
        WikiPage,
        Comment,
        ModListing,
        UserProfile,
        Message,
        Review,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReportReason
    {
        Spam,
        Harassment,
        Violence,
        Pornography,
        IllegalContent,
        ChildAbuse,
        HateSpeech,
        Misinformation,
        Copyright,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReportStatus
    {
        Pending,
        InReview,
        Resolved,
        Rejected,
        Duplicate
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ModeratorAction
    {
        NoAction,
        ContentRemoved,
        ContentEdited,
        UserWarned,
        UserSuspended,
        UserBanned,
        ReportRejected,
        EscalatedToAdmin
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReportPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
