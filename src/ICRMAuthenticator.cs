using System;

namespace CrmWebApiProxy
{
    public interface ICRMAuthenticator
    {
        string GetAccessToken();

        string UserName { get; }
        Guid UserId { get; }
        Guid BusinessUnitId { get; }

        void EnsureConnection();
    }
}