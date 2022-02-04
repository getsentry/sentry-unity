namespace Sentry.Unity
{
    /// <summary>
    /// Observer for the sync. of Scopes across SDKs.
    /// </summary>
    public interface IScopeObserver
    {
        /// <summary>
        /// Adds a breadcrumb.
        /// </summary>
        void AddBreadcrumb(Breadcrumb breadcrumb);

        /// <summary>
        /// Sets an extra.
        /// </summary>
        void SetExtra(string key, string value);

        /// <summary>
        /// Remove an extra.
        /// </summary>
        void UnsetExtra(string key);

        /// <summary>
        /// Sets a tag.
        /// </summary>
        void SetTag(string key, string value);

        /// <summary>
        /// Removes a tag.
        /// </summary>
        void UnsetTag(string key);

        /// <summary>
        /// Sets the user information.
        /// </summary>
        void SetUser(User user);

        /// <summary>
        /// Sets the user information.
        /// </summary>
        void UnsetUser();
    }
}
