/// <summary>
/// Defines the roles a user can have within an organization, determining their level of access and permissions.
/// </summary>
public enum OrganizationRole
{
    /// <summary>
    /// Grants read-only access to the organization's resources.
    /// Permissions:
    /// - View users, groups, and clients within the organization.
    /// - Cannot create, edit, or delete any resources.
    /// </summary>
    Viewer,

    /// <summary>
    /// Allows management of the organization's day-to-day operations and resources.
    /// Permissions:
    /// - All permissions of a Viewer.
    /// - Invite, edit, and remove users from the organization.
    /// - Create, edit, and delete groups.
    /// - Create, edit, and delete OpenID clients.
    /// - Cannot delete the organization itself.
    /// </summary>
    Admin,

    /// <summary>
    /// Provides full control over the organization, including destructive and administrative actions.
    /// Typically assigned to the user who created the organization.
    /// Permissions:
    /// - All permissions of an Admin.
    /// - Delete the organization.
    /// - Manage billing and subscription details.
    /// - Transfer ownership of the organization to another user.
    /// </summary>
    Owner
}
