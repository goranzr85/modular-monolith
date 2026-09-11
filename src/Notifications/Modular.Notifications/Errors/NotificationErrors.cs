using ErrorOr;

namespace Modular.Notifications.Errors;

public static class NotificationErrors
{
    public static Error InvalidNotificationType() =>
        Error.Failure("Notification.InvalidType", "Invalid notification type.");

    public static Error UnsupportedPrimaryContactType() =>
        Error.Failure("Notification.UnsupportedPrimaryContactType", "Unsupported primary contact type.");
}
